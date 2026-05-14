using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
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

        public DashboardWindow()
        {
            InitializeComponent();

            string imagePath = Path.Combine(AppContext.BaseDirectory, "assets", "sitting", "sitting.png");
            if (File.Exists(imagePath)) AstraImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));

            clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            clockTimer.Tick += (s, e) => ClockText.Text = DateTime.Now.ToString("HH:mm");
            clockTimer.Start();

            MainContent.Content = viewCalendar;
        }

        private void BtnCalendar_Click(object sender, RoutedEventArgs e) => MainContent.Content = viewCalendar;
        private void BtnNotes_Click(object sender, RoutedEventArgs e) => MainContent.Content = viewNotes;
        private void BtnTasks_Click(object sender, RoutedEventArgs e) => MainContent.Content = viewTasks;

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Hide();

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }

        private void BtnPomodoroSetup_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

            if (mainWindow != null && mainWindow.isFocusMode) return;
            if (System.Windows.Application.Current.Windows.OfType<PomodoroSetupWindow>().Any()) return;

            this.Hide();
            if (mainWindow != null) new PomodoroSetupWindow(mainWindow).Show();
        }
    }
}