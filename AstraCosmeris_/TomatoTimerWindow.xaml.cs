using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public partial class TomatoTimerWindow : Window
    {
        public bool forceClose = false;
        private System.Windows.Media.MediaPlayer flashbangPlayer = new System.Windows.Media.MediaPlayer();

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!forceClose)
            {
                e.Cancel = true;
            }
            else
            {
                // FIX: DỌN SẠCH ZOMBIE TRƯỚC KHI CỬA SỔ CHẾT THẬT
                if (timer != null) timer.Stop();
                if (shakeTimer != null) shakeTimer.Stop();
                if (flashbangPlayer != null) flashbangPlayer.Close();
            }
            base.OnClosing(e);
        }

        private int workSeconds, breakSeconds, currentSeconds;
        private bool isWorking = true;
        private bool isPaused = false;
        private DispatcherTimer timer;

        // BIẾN CHO HIỆU ỨNG LẮC LƯ
        private DispatcherTimer shakeTimer;
        private Random rand = new Random();

        private bool _isDragging = false;
        private System.Windows.Point _clickPosition;

        public TomatoTimerWindow(int w, int b, FocusAstraWindow focusWin, MainWindow main)
        {
            InitializeComponent();
            
            // XỬ LÝ EASTER EGG TỪ BẢNG SETUP GỬI QUA
            workSeconds = (w == -1) ? 1 : (w * 60); 
            breakSeconds = b * 60;
            currentSeconds = workSeconds;

            this.Left = main.Left - 240;
            this.Top = main.Top + 20;

            // Timer chính (1 giây nổ 1 lần)
            timer = new DispatcherTimer(DispatcherPriority.Render);
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick!;
            timer.Start();

            // Timer phụ để rung lắc mượt mà (50ms nổ 1 lần)
            shakeTimer = new DispatcherTimer(DispatcherPriority.Render);
            shakeTimer.Interval = TimeSpan.FromMilliseconds(50);
            shakeTimer.Tick += ShakeTimer_Tick!;
            shakeTimer.Start(); // Cứ cho chạy ngầm, lúc nào cần lắc nó tự lắc

            UpdateText();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isPaused) return;
            currentSeconds--;
            UpdateText();

            if (currentSeconds <= 0)
            {
                // HẾT GIỜ -> NÉM FLASHBANG !!!
                TriggerFlashbang();

                // Đảo trạng thái (Đang làm -> Nghỉ, Đang nghỉ -> Làm)
                isWorking = !isWorking;
                currentSeconds = isWorking ? workSeconds : breakSeconds;
                TxtStatus.Text = isWorking ? "LÀM VIỆC" : "NGHỈ NGƠI";
                TxtStatus.Foreground = isWorking ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.LightGreen;
            }
        }

        // --- THUẬT TOÁN RUNG LẮC QUẢ CÀ CHUA ---
        private void ShakeTimer_Tick(object sender, EventArgs e)
        {
            // Nếu thời gian > 30s hoặc đang bị Pause thì nằm im
            if (currentSeconds > 30 || currentSeconds <= 0 || isPaused)
            {
                TomatoRotation.Angle = 0;
                return;
            }

            // Nếu còn <= 30 giây: Tính biên độ rung (càng gần 0 rung càng bạo)
            double intensity = (30 - currentSeconds) * 1.5;

            // Xoay random trái/phải liên tục
            TomatoRotation.Angle = (rand.NextDouble() * 2 - 1) * intensity;
        }

        // --- BÍ THUẬT NÉM FLASHBANG ---
        private void TriggerFlashbang()
        {
            try
            {
                // Bọc thép đường dẫn tuyệt đối
                string soundPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "assets", "pomodoro", "explosion.mp3"));

                if (System.IO.File.Exists(soundPath))
                {
                    flashbangPlayer.Open(new Uri(soundPath, UriKind.Absolute));
                    flashbangPlayer.Volume = 1.0; // Ép âm lượng 100%
                    flashbangPlayer.Play(); // Bóp cò!
                }
            }
            catch { } // Lỗi thì câm nín chịu đựng

            // Tạo 1 cửa sổ chớp trắng đè lên mọi thứ trên màn hình
            Window flash = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.White, // Màu Flashbang
                Topmost = true,
                ShowInTaskbar = false,
                WindowState = WindowState.Maximized // Phóng to full màn
            };

            // Hiệu ứng mờ dần (từ mù mắt 100% xuống 0%) trong 5 giây
            System.Windows.Media.Animation.DoubleAnimation fade = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(5))
            };

            // Mờ xong thì cửa sổ tự sát luôn, dọn dẹp RAM
            fade.Completed += (s, ev) => flash.Close();

            flash.Show();
            flash.BeginAnimation(Window.OpacityProperty, fade);
        }

        private void UpdateText()
        {
            TimeSpan t = TimeSpan.FromSeconds(currentSeconds);
            TxtTimer.Text = t.ToString(@"mm\:ss");
        }

        // --- KÉO THẢ & NHÁY ĐÚP ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    if (isWorking)
                    {
                        isPaused = !isPaused;
                        TxtStatus.Text = isPaused ? "TẠM DỪNG" : "LÀM VIỆC";
                    }
                }
                else
                {
                    _isDragging = true;
                    _clickPosition = e.GetPosition(this);
                    RootGrid.CaptureMouse();
                }
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging)
            {
                System.Windows.Point currentMousePos = PointToScreen(e.GetPosition(this));
                this.Left = currentMousePos.X - _clickPosition.X;
                this.Top = currentMousePos.Y - _clickPosition.Y;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                RootGrid.ReleaseMouseCapture();
            }
        }
    }
}