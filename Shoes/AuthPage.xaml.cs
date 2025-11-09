using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Логика взаимодействия для AuthPage.xaml
    /// </summary>
    public partial class AuthPage : Page
    {
        private int failedAttempts = 0;

        public AuthPage()
        {
            InitializeComponent();
 
        }

        private async void LodinBtn_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTB.Text;
            string password = PassTB.Text;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Есть пустые поля");
                return;
            }

            User user = Shoes_GerasimovaEntities.GetContext().User.ToList()
               .Find(p => p.login == login && p.password == password);

            if (user != null)
            {
               

                Manager.MainFrame.Navigate(new ShoesPage(user));
                LoginTB.Text = "";
                PassTB.Text = "";
                failedAttempts = 0;
            
            }
            else
            {
                MessageBox.Show("Введены неверные данные");
                failedAttempts++;

                // Блокируем кнопку
                LodinBtn.IsEnabled = false;

            }
        }
      

        private void welcome_Click(object sender, RoutedEventArgs e)
        {
            LoginTB.Text = "";
            PassTB.Text = "";
            User guestUser = new User()
            {
                firstName = "Гость",
                lastName = "",
                patronymic = "",
                login = "guest",
                password = "password", // Или любая другая заглушка
                role_user = 0 // Или любая другая роль по умолчанию
            };

            Manager.MainFrame.Navigate(new ShoesPage(guestUser, true));
        }
    }
}
