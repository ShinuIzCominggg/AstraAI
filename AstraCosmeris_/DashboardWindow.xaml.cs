using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;

namespace AstraCosmeris_
{
    public partial class DashboardWindow : Window
    {
        private DispatcherTimer clockTimer;

        // Khởi tạo sẵn 3 cái view
        private UcCalendar viewCalendar = new UcCalendar();
        private UcNotes viewNotes = new UcNotes();
        private UcTasks viewTasks = new UcTasks();

        public DashboardWindow()
        {
            InitializeComponent();

            // --- FIX LỖI TÀNG HÌNH ASTRA ---
            string imagePath = Path.Combine(AppContext.BaseDirectory, "assets", "sitting", "sitting.png");
            if (File.Exists(imagePath))
            {
                AstraImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
            }

            // Setup đồng hồ
            clockTimer = new DispatcherTimer();
            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += (s, e) => {
                ClockText.Text = DateTime.Now.ToString("HH:mm");
            };
            clockTimer.Start();

            // Mặc định nạp tab Calendar
            MainContent.Content = viewCalendar;
        }

        // --- CÁC HÀM CHUYỂN TAB (M BỊ THIẾU CÁI NÀY NÈ) ---
        private void BtnCalendar_Click(object sender, RoutedEventArgs e) => MainContent.Content = viewCalendar;
        private void BtnNotes_Click(object sender, RoutedEventArgs e) => MainContent.Content = viewNotes;
        private void BtnTasks_Click(object sender, RoutedEventArgs e) => MainContent.Content = viewTasks;

        // --- ĐIỀU KHIỂN WINDOW ---
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnPomodoroSetup_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

            // FIX LỖI 6: Nếu đang focus hoặc Bảng setup đã mở rồi thì nghỉ, không đẻ thêm!
            if (mainWindow != null && mainWindow.isFocusMode) return;
            if (System.Windows.Application.Current.Windows.OfType<PomodoroSetupWindow>().Any()) return;

            this.Hide();
            if (mainWindow != null)
            {
                PomodoroSetupWindow setup = new PomodoroSetupWindow(mainWindow);
                setup.Show();
            }
        }
    }
}