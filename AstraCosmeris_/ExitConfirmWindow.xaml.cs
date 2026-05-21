using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AstraCosmeris_
{
    public partial class ExitConfirmWindow : Window
    {
        private MainWindow? parentMain;

        public ExitConfirmWindow()
        {
            InitializeComponent();
            LoadDynamicImage();
        }

        public ExitConfirmWindow(MainWindow main)
        {
            InitializeComponent();
            parentMain = main;
            LoadDynamicImage();
        }

        private void LoadDynamicImage()
        {
            string state = "mad";
            string outfit = DataManager.Data.CurrentOutfit;

            // 👉 SỬA BIỆN PHÁP: Nếu là Default thì gọi trực tiếp Pack URI nhúng ngầm
            if (string.IsNullOrEmpty(outfit) || outfit == "Default")
            {
                AstraImage.Source = new BitmapImage(new Uri($"/assets/{state}/{state}.png", UriKind.RelativeOrAbsolute));
            }
            else
            {
                string basePath = AppContext.BaseDirectory;
                string imagePath = Path.Combine(basePath, "assets", state, "wardrobe", outfit, $"{state}_{outfit}.png");

                if (File.Exists(imagePath))
                {
                    AstraImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                }
                else
                {
                    // Lùi về ảnh nhúng nếu folder bị lỗi file
                    AstraImage.Source = new BitmapImage(new Uri($"/assets/{state}/{state}.png", UriKind.RelativeOrAbsolute));
                }
            }
        }

        private void BtnStay_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            if (parentMain != null)
            {
                parentMain.forceClose = true;
                parentMain.Close();
            }
            else
            {
                System.Windows.Application.Current.Shutdown();
            }
            System.Windows.Application.Current.Shutdown();
        }
    }
}