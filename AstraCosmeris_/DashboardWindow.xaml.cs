using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public partial class DashboardWindow : Window
    {
        private DispatcherTimer clockTimer;
        private UcCalendar viewCalendar = new UcCalendar();
        private UcNotes viewNotes = new UcNotes();
        private UcTasks viewTasks = new UcTasks();
        private UcCanvas viewCanvas;
        private bool isAnimating = false;

        public DashboardWindow()
        {
            InitializeComponent();

            string state = "sitting";
            string outfit = DataManager.Data.CurrentOutfit;

            // 👉 SỬA BIỆN PHÁP: Fix lỗi biến mất ảnh ngồi Default ở nền Dashboard
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
                    AstraImage.Source = new BitmapImage(new Uri($"/assets/{state}/{state}.png", UriKind.RelativeOrAbsolute));
                }
            }

            clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            clockTimer.Tick += (s, e) => ClockText.Text = DateTime.Now.ToString("HH:mm");
            clockTimer.Start();

            MainContent.Content = viewCalendar;
            viewCanvas = new UcCanvas(this);
        }

        public void ToggleSidebar() { SidebarBorder.Visibility = SidebarBorder.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; }
        public void ToggleFullScreen()
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void SwitchTab(System.Windows.Controls.UserControl newView)
        {
            if (isAnimating || MainContent.Content == newView) return;
            isAnimating = true;

            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (s, e) =>
            {
                MainContent.Content = newView;
                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
                fadeIn.Completed += (s2, e2) => isAnimating = false;
                MainContent.BeginAnimation(OpacityProperty, fadeIn);
            };
            MainContent.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void BtnCalendar_Click(object sender, RoutedEventArgs e) => SwitchTab(viewCalendar);
        private void BtnNotes_Click(object sender, RoutedEventArgs e) => SwitchTab(viewNotes);
        private void BtnTasks_Click(object sender, RoutedEventArgs e) => SwitchTab(viewTasks);
        private void BtnCanvas_Click(object sender, RoutedEventArgs e) => SwitchTab(viewCanvas);

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (mainWindow != null) mainWindow.CloseExclusiveWindow();
            else this.Hide();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }

        private void BtnPomodoroSetup_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

            if (mainWindow != null && mainWindow.isFocusMode) return;
            if (System.Windows.Application.Current.Windows.OfType<PomodoroSetupWindow>().Any()) return;

            if (mainWindow != null)
            {
                mainWindow.OpenExclusiveWindow(new PomodoroSetupWindow(mainWindow));
            }
        }
    }
}