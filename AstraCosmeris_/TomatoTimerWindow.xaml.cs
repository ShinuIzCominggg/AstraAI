using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public partial class TomatoTimerWindow : Window
    {
        public bool forceClose = false;
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
            else { timer?.Stop(); shakeTimer?.Stop(); }
            base.OnClosing(e);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isPaused) return;
            currentSeconds--;
            UpdateText();

            // KHI HẾT GIỜ
            if (currentSeconds <= 0)
            {
                isPaused = true; // Tạm dừng để chờ lệnh

                string title = isWorking ? "🍅 Hết Pomodoro!" : "☕ Hết giờ nghỉ!";
                string msg = isWorking ? "Cậu đã làm rất tốt! Muốn nghỉ ngơi hay làm thêm 5 phút?" : "Hết giờ xả hơi rồi! Vào làm việc tiếp thôi!";

                var notif = new AstraNotificationWindow(
                    title: title,
                    message: msg,
                    btn1Text: "Cố thêm 5p",
                    action1: () => {
                        currentSeconds += 300; // Cộng 5 phút
                        isPaused = false;
                    },
                    btn2Text: isWorking ? "Nghỉ ngơi" : "Làm việc",
                    action2: () => {
                        isWorking = !isWorking;
                        currentSeconds = isWorking ? workSeconds : breakSeconds;
                        TxtStatus.Text = isWorking ? "LÀM VIỆC" : "NGHỈ NGƠI";
                        TxtStatus.Foreground = isWorking ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.LightGreen;
                        isPaused = false;

                        // Track Report
                        if (!isWorking)
                        {
                            string today = DateTime.Now.ToString("yyyy-MM-dd");
                            if (!DataManager.Data.Stats.ContainsKey(today)) DataManager.Data.Stats[today] = new DailyStat();
                            DataManager.Data.Stats[today].PomodorosCompleted++;
                            DataManager.SaveData();
                        }
                    }
                );
                notif.Show();
            }
        }

        private void ShakeTimer_Tick(object sender, EventArgs e)
        {
            if (currentSeconds > 30 || currentSeconds <= 0 || isPaused) { TomatoRotation.Angle = 0; return; }
            double intensity = (30 - currentSeconds) * 1.5;
            TomatoRotation.Angle = (rand.NextDouble() * 2 - 1) * intensity;
        }

        private void UpdateText() => TxtTimer.Text = TimeSpan.FromSeconds(Math.Max(currentSeconds, 0)).ToString(@"mm\:ss");

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

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) { if (_isDragging) { System.Windows.Point p = PointToScreen(e.GetPosition(this)); this.Left = p.X - _clickPosition.X; this.Top = p.Y - _clickPosition.Y; } }
        private void Window_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (_isDragging) { _isDragging = false; RootGrid.ReleaseMouseCapture(); } }
    }
}