using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shoes
{
    /// <summary>
    /// Логика взаимодействия для ShoesPage.xaml
    /// </summary>
    public partial class ShoesPage : Page
    {

        private User _currentUser; // Добавлено поле для хранения информации о пользователе
        private bool _isGuest = false;


        // Используем числовые значения вместо констант
        private const int ROLE_CLIENT = 1;
        private const int ROLE_ADMIN = 2;
        private const int ROLE_MANAGER = 3;
        private const int ROLE_GUEST = 0;

        public ShoesPage(User user, bool isGuest = false)
        {
            InitializeComponent();

            _isGuest = isGuest;
            _currentUser = user; // Сохраняем информацию о пользователе

            if (isGuest)
            {
                FIOTB.Text = "Вы авторизованы как Гость";
                RoleTB.Text = "";

                _currentUser.role_user = ROLE_GUEST;
            }
            else
            {
                FIOTB.Text = $"Вы авторизованы как {user.lastName} {user.firstName} {user.patronymic}";

                // Получаем название роли из связанной таблицы
                var role = Shoes_GerasimovaEntities.GetContext().Role
                    .FirstOrDefault(r => r.id_role == user.role_user);

                string roleText = role?.nameRole ?? "Роль не определена";
                RoleTB.Text = $"Роль: {roleText}";
            }
            // Применяем ограничения в зависимости от роли
            ApplyRoleRestrictions();


            var currentShoes = Shoes_GerasimovaEntities.GetContext().Product.ToList();
            ShoesListView.ItemsSource = currentShoes;
            ComboType.SelectedIndex = 0;
            UpdateShoes();
        }


        private void ApplyRoleRestrictions()
        {
            int userRole = _currentUser.role_user;

            // По ТЗ: Поиск, сортировка и фильтрация доступны только Менеджеру и Администратору
            if (userRole != ROLE_MANAGER && userRole != ROLE_ADMIN)
            {
                // Скрываем панель с поиском, фильтрацией и сортировкой
                var wrapPanel = (WrapPanel)this.FindName("WrapPanel");
                if (wrapPanel != null)
                {
                    wrapPanel.Visibility = Visibility.Collapsed;
                }
            }

            // По ТЗ: Добавление, редактирование и удаление доступно только Администратору
            if (userRole != ROLE_ADMIN)
            {
                AddButton.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateShoes()
        {
            var currentShoes = Shoes_GerasimovaEntities.GetContext().Product.ToList();

            int userRole = _currentUser.role_user;

            if (userRole == ROLE_MANAGER || userRole == ROLE_ADMIN)
            {

                if (ComboType.SelectedIndex == 0)
                {
                    currentShoes = currentShoes.ToList();
                }
                if (ComboType.SelectedIndex == 1)
                {
                    currentShoes = currentShoes.Where(p => p.supplier == "Kari").ToList();
                }

                if (ComboType.SelectedIndex == 2)
                {
                    currentShoes = currentShoes.Where(p => p.supplier == "Обувь для вас").ToList();
                }


                string searchText = TBoxSearch.Text.ToLower();
                currentShoes = currentShoes.Where(p =>
                (p.product_name.ToLower().Contains(searchText)) ||
                (p.Category1.name_category.ToLower().Contains(searchText)) ||
                (p.description.ToLower().Contains(searchText)) ||
                (p.manufacturer1.manufacturer_name.ToLower().Contains(searchText)) ||
                (p.supplier.ToLower().Contains(searchText)) ||
                (p.unit.ToLower().Contains(searchText))
                ).ToList();



                if (RBUp.IsChecked.Value)
                {
                    currentShoes = currentShoes.OrderBy(p => p.quantityInStock).ToList();
                }
                if (RBDown.IsChecked.Value)
                {
                    currentShoes = currentShoes.OrderByDescending(p => p.quantityInStock).ToList();
                }

            }
            ShoesListView.ItemsSource = currentShoes.ToList();
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateShoes();
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateShoes();
        }


        private void RBUp_Checked(object sender, RoutedEventArgs e)
        {
            UpdateShoes();
        }

        private void RBDown_Checked(object sender, RoutedEventArgs e)
        {
            UpdateShoes();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser.role_user != ROLE_ADMIN)
            {
                MessageBox.Show("Добавлять товары может только администратор!", "Ошибка прав доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Manager.MainFrame.Navigate(new AddEditPage(null));
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем права на редактирование
            if (_currentUser.role_user != ROLE_ADMIN)
            {
                MessageBox.Show("Редактировать товары может только администратор!", "Ошибка прав доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Manager.MainFrame.Navigate(new AddEditPage((sender as Button).DataContext as Product));
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                Shoes_GerasimovaEntities.GetContext().ChangeTracker.Entries().ToList().ForEach(p => p.Reload());
                // Вместо прямого присвоения ItemsSource вызываем UpdateShoes()
                UpdateShoes();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем права на удаление
            if (_currentUser.role_user != ROLE_ADMIN)
            {
                MessageBox.Show("Удалять товары может только администратор!", "Ошибка прав доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var currentProduct = (sender as Button).DataContext as Product;

            // Проверяем, есть ли товар в каких-либо заказах
            bool existsInOrders = Shoes_GerasimovaEntities.GetContext().OrderProduct
                .Any(op => op.articleNumber == currentProduct.article);

            if (existsInOrders)
            {
                MessageBox.Show("Невозможно удалить товар, который присутствует в заказе!",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Вы точно хотите выполнить удаление?",
                "Внимание!",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                
                    Shoes_GerasimovaEntities.GetContext().Product.Remove(currentProduct);
                    Shoes_GerasimovaEntities.GetContext().SaveChanges();

                    // Обновляем список товаров
                    ShoesListView.ItemsSource = Shoes_GerasimovaEntities.GetContext().Product.ToList();
                    UpdateShoes();
                
                
            }
        }
    }
}
