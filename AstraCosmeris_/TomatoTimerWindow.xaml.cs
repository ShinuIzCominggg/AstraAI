using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public partial class TomatoTimerWindow : Window
    {
        public bool forceClose = false;
        private MediaPlayer flashbangPlayer = new MediaPlayer();

        private int workSeconds, breakSeconds, currentSeconds;
        private bool isWorking = true;
        private bool isPaused = false;

        private DispatcherTimer timer;
        private DispatcherTimer shakeTimer;
        private Random rand = new Random();

        private bool _isDragging = false;
        private System.Windows.Point _clickPosition;

        public TomatoTimerWindow(int w, int b, FocusAstraWindow focusWin, MainWindow main)
        {
            InitializeComponent();

            workSeconds = (w == -1) ? 1 : (w * 60);
            breakSeconds = b * 60;
            currentSeconds = workSeconds;

            this.Left = main.Left - 240;
            this.Top = main.Top + 20;

            timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick!;
            timer.Start();

            shakeTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(50) };
            shakeTimer.Tick += ShakeTimer_Tick!;
            shakeTimer.Start();

            UpdateText();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!forceClose) e.Cancel = true;
            else
            {
                timer?.Stop();
                shakeTimer?.Stop();
                flashbangPlayer?.Close();
            }
            base.OnClosing(e);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isPaused) return;
            currentSeconds--;
            UpdateText();

            if (currentSeconds <= 0)
            {
                TriggerFlashbang();
                isWorking = !isWorking;
                currentSeconds = isWorking ? workSeconds : breakSeconds;
                TxtStatus.Text = isWorking ? "LÀM VIỆC" : "NGHỈ NGƠI";
                TxtStatus.Foreground = isWorking ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.LightGreen;
            }
        }

        private void ShakeTimer_Tick(object sender, EventArgs e)
        {
            if (currentSeconds > 30 || currentSeconds <= 0 || isPaused)
            {
                TomatoRotation.Angle = 0;
                return;
            }
            double intensity = (30 - currentSeconds) * 1.5;
            TomatoRotation.Angle = (rand.NextDouble() * 2 - 1) * intensity;
        }

        private void TriggerFlashbang()
        {
            try
            {
                string soundPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "assets", "pomodoro", "explosion.mp3"));
                if (File.Exists(soundPath))
                {
                    flashbangPlayer.Open(new Uri(soundPath, UriKind.Absolute));
                    flashbangPlayer.Volume = 1.0;
                    flashbangPlayer.Play();
                }
            }
            catch { }

            Window flash = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.White,
                Topmost = true,
                ShowInTaskbar = false,
                WindowState = System.Windows.WindowState.Maximized
            };

            DoubleAnimation fade = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(5))
            };

            fade.Completed += (s, ev) => flash.Close();
            flash.Show();
            flash.BeginAnimation(OpacityProperty, fade);
        }

        private void UpdateText() => TxtTimer.Text = TimeSpan.FromSeconds(currentSeconds).ToString(@"mm\:ss");

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2 && isWorking)
                {
                    isPaused = !isPaused;
                    TxtStatus.Text = isPaused ? "TẠM DỪNG" : "LÀM VIỆC";
                }
                else if (e.ClickCount != 2)
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