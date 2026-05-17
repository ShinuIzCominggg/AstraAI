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
        private bool isAnimating = false; // Cờ chống lag

        public DashboardWindow()
        {
            InitializeComponent();

            string imagePath = Path.Combine(AppContext.BaseDirectory, "assets", "sitting", "sitting.png");
            if (File.Exists(imagePath)) AstraImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));

            clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            clockTimer.Tick += (s, e) => ClockText.Text = DateTime.Now.ToString("HH:mm");
            clockTimer.Start();

            MainContent.Content = viewCalendar;
            viewCanvas = new UcCanvas(this);
        }

        // Hỗ trợ thu gọn UI cho Canvas
        public void ToggleSidebar() { SidebarBorder.Visibility = SidebarBorder.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; }
        public void ToggleFullScreen() { this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }

        private void SwitchTab(System.Windows.Controls.UserControl newView)
        {
            if (isAnimating || MainContent.Content == newView) return;
            isAnimating = true;

            DoubleAnimation fadeOut = new DoubleAnimation { To = 0.0, Duration = TimeSpan.FromMilliseconds(150), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            fadeOut.Completed += (s, e) =>
            {
                MainContent.Content = newView;
                DoubleAnimation fadeIn = new DoubleAnimation { To = 1.0, Duration = TimeSpan.FromMilliseconds(200), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
                fadeIn.Completed += (s2, e2) => isAnimating = false;
                MainContent.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };
            MainContent.BeginAnimation(UIElement.OpacityProperty, fadeOut);
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