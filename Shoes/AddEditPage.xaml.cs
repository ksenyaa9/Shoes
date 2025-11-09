using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    public partial class AddEditPage : Page
    {
        private Product _currentShoes = new Product();
        private string _originalImagePath = null;
        private bool _isNewProduct = false;
        private string _imagesFolder = @"C:\Users\kseni\Desktop\дэмэкзамен\Shoes\Shoes\image";

        public AddEditPage(Product SelectedService)
        {
            InitializeComponent();

            if (SelectedService != null)
            {
                _currentShoes = SelectedService;
                _originalImagePath = _currentShoes.image;
                _isNewProduct = false;

                // Для редактирования показываем артикул как только для чтения
                ArticleTextBox.IsEnabled = false;
                ArticleTextBox.Background = Brushes.LightGray;
            }
            else
            {
                _currentShoes = new Product();
                _currentShoes.discount = 0;
                _isNewProduct = true;

                // Для нового товара скрываем поле артикула
                ArticleTextBox.Visibility = Visibility.Collapsed;
                // Находим TextBlock для артикула и скрываем его
                var textBlock = LogicalTreeHelper.FindLogicalNode(this, "АртикулTextBlock") as TextBlock;
                if (textBlock != null)
                    textBlock.Visibility = Visibility.Collapsed;
            }

            LoadComboBoxData();
            DataContext = _currentShoes;
            SetSelectedValuesInComboBoxes();
        }

        private void LoadComboBoxData()
        {
            try
            {
                var context = Shoes_GerasimovaEntities.GetContext();

                var categories = context.Category.ToList();
                ComboCategory.ItemsSource = categories;
                ComboCategory.DisplayMemberPath = "name_category";
                ComboCategory.SelectedValuePath = "id_category";

                var manufacturers = context.manufacturer.ToList();
                ComboManufacturer.ItemsSource = manufacturers;
                ComboManufacturer.DisplayMemberPath = "manufacturer_name";
                ComboManufacturer.SelectedValuePath = "id_manufacturer";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных: " + ex.Message);
            }
        }

        private void SetSelectedValuesInComboBoxes()
        {
            if (_currentShoes.category > 0)
            {
                ComboCategory.SelectedValue = _currentShoes.category;
            }

            if (_currentShoes.manufacturer > 0)
            {
                ComboManufacturer.SelectedValue = _currentShoes.manufacturer;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(_currentShoes.product_name))
                errors.AppendLine("Укажите название товара");

            if (_currentShoes.cost <= 0)
                errors.AppendLine("Укажите корректную стоимость");

            if (_currentShoes.quantityInStock < 0)
                errors.AppendLine("Количество на складе не может быть отрицательным");

            if (_currentShoes.category <= 0)
                errors.AppendLine("Выберите категорию товара");

            if (_currentShoes.discount < 0 || _currentShoes.discount > 100)
                errors.AppendLine("Скидка должна быть в диапазоне от 0 до 100");

            if (_currentShoes.manufacturer <= 0)
                errors.AppendLine("Выберите производителя");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString());
                return;
            }

            try
            {
                var context = Shoes_GerasimovaEntities.GetContext();

                if (_isNewProduct)
                {
                    _currentShoes.article = GenerateArticle();
                    context.Product.Add(_currentShoes);
                }
                else
                {
                    // Для существующего товара обновляем запись
                    context.Product.Attach(_currentShoes);
                    context.Entry(_currentShoes).State = System.Data.Entity.EntityState.Modified;
                }

                context.SaveChanges();
                MessageBox.Show("Информация сохранена");
                Manager.MainFrame.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.ToString());
            }
        }

        private string GenerateArticle()
        {
            var context = Shoes_GerasimovaEntities.GetContext();
            var existingArticles = context.Product
                .Where(p => p.article != null)
                .Select(p => p.article)
                .ToHashSet();

            string newArticle;
            Random random = new Random();
            int attempts = 0;
            const int maxAttempts = 1000;

            do
            {
                attempts++;
                if (attempts > maxAttempts)
                {
                    throw new Exception("Не удалось сгенерировать уникальный артикул");
                }

                char firstChar = (char)('A' + random.Next(0, 26));
                string threeDigits = random.Next(100, 1000).ToString();
                char secondChar = (char)('A' + random.Next(0, 26));
                string lastDigit = random.Next(0, 10).ToString();

                newArticle = $"{firstChar}{threeDigits}{secondChar}{lastDigit}";
            }
            while (existingArticles.Contains(newArticle));

            return newArticle;
        }

        private void ComboCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboCategory.SelectedValue != null && int.TryParse(ComboCategory.SelectedValue.ToString(), out int categoryId))
            {
                _currentShoes.category = categoryId;
            }
        }

        private void ComboManufacturer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboManufacturer.SelectedValue != null && int.TryParse(ComboManufacturer.SelectedValue.ToString(), out int manufacturerId))
            {
                _currentShoes.manufacturer = manufacturerId;
            }
        }

        private void ChangePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFile = openFileDialog.FileName;

                try
                {
                    // Обрабатываем изображение
                    string processedImagePath = ProcessAndSaveImage(selectedFile);

                    // Удаляем старое изображение если оно было изменено и находится в папке image
                    if (!string.IsNullOrEmpty(_originalImagePath) &&
                        _originalImagePath != processedImagePath &&
                        System.IO.File.Exists(_originalImagePath) &&
                        _originalImagePath.StartsWith(_imagesFolder))
                    {
                        try
                        {
                            System.IO.File.Delete(_originalImagePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Не удалось удалить старое изображение: " + ex.Message);
                        }
                    }

                    _currentShoes.image = processedImagePath;
                    _originalImagePath = processedImagePath; // Обновляем оригинальный путь

                    // Обновляем изображение в интерфейсе
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(processedImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    LogoImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при обработке изображения: " + ex.Message);
                }
            }
        }

        private string ProcessAndSaveImage(string sourceImagePath)
        {
            // Используем существующую папку image
            if (!System.IO.Directory.Exists(_imagesFolder))
            {
                System.IO.Directory.CreateDirectory(_imagesFolder);
            }

            // Генерируем уникальное имя файла
            string fileExtension = System.IO.Path.GetExtension(sourceImagePath);
            string fileName = $"product_{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid().ToString().Substring(0, 8)}{fileExtension}";
            string outputPath = System.IO.Path.Combine(_imagesFolder, fileName);

            // Загружаем и обрабатываем изображение
            using (System.IO.FileStream sourceStream = new System.IO.FileStream(sourceImagePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                BitmapImage sourceImage = new BitmapImage();
                sourceImage.BeginInit();
                sourceImage.StreamSource = sourceStream;
                sourceImage.CacheOption = BitmapCacheOption.OnLoad;
                sourceImage.EndInit();
                sourceImage.Freeze();

                // Создаем TransformedBitmap для изменения размера
                TransformedBitmap resizedImage = new TransformedBitmap();
                resizedImage.BeginInit();
                resizedImage.Source = sourceImage;

                // Вычисляем коэффициенты масштабирования для размера 300x200
                double scaleX = 300.0 / sourceImage.PixelWidth;
                double scaleY = 200.0 / sourceImage.PixelHeight;
                double scale = Math.Min(scaleX, scaleY); // Сохраняем пропорции

                resizedImage.Transform = new ScaleTransform(scale, scale);
                resizedImage.EndInit();
                resizedImage.Freeze();

                // Сохраняем изображение
                BitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(resizedImage));

                using (System.IO.FileStream outputStream = new System.IO.FileStream(outputPath, System.IO.FileMode.Create))
                {
                    encoder.Save(outputStream);
                }
            }

            return outputPath;
        }

        // Добавляем обработчик для очистки временных файлов при навигации назад без сохранения
        private void CleanupTemporaryImage()
        {
            // Если пользователь отменил редактирование и загружал новое изображение, но не сохранил
            if (!string.IsNullOrEmpty(_currentShoes.image) &&
                _currentShoes.image != _originalImagePath &&
                System.IO.File.Exists(_currentShoes.image))
            {
                try
                {
                    System.IO.File.Delete(_currentShoes.image);
                }
                catch (Exception ex)
                {
                    // Игнорируем ошибки при удалении временных файлов
                }
            }
        }

        // Обработчик для кнопки "Назад" или отмены
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CleanupTemporaryImage();
                Manager.MainFrame.GoBack();
            }
            base.OnKeyDown(e);
        }
    }
}