using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public enum PetState { Idle, Thinking, Happy }

    public partial class MainWindow : Window
    {
        public bool isFocusMode = false;
        public bool isBigChatOpen = false;
        public bool forceClose = false;
        public bool isSmol = false;

        private DispatcherTimer? randomThoughtTimer;
        private DispatcherTimer? taskRadarTimer;
        private DispatcherTimer? animTimer;

        // --- ĐỘNG CƠ VẬT LÝ CHO ASTRA SMOL ---
        private DispatcherTimer? physicsTimer;
        private double velX = 0, velY = 0;
        private double friction = 0.94; // Ma sát trượt (Càng gần 1 trượt càng xa)
        private double bounce = -0.7;   // Độ nảy khi đập tường
        private double originalWidth, originalHeight;

        private System.Windows.Forms.NotifyIcon? trayIcon;

        // --- QUẢN LÝ CỬA SỔ ĐỘC QUYỀN (FLYOUT ANIMATION) ---
        private Window? currentExclusiveWindow = null;
        private DashboardWindow? dashboard;
        private ChatInputWindow? chatWindow;
        private ChatHistoryWindow? bigChatWindow;

        // --- QUÁN TÍNH CHUỘT ---
        private bool _isDragging = false;
        private System.Windows.Point _dragStartPos;
        private System.Windows.Point _lastMousePos; // Biến mới để đo lực ném

        private string lastTriggeredMinute = "";
        private Random rand = new Random();
        private List<ReminderPopup> activePopups = new List<ReminderPopup>();
        private DispatcherTimer? spamTimer;

        private string[] currentFrames = Array.Empty<string>();
        private int currentFrameIndex = 0;
        private PetState currentState = PetState.Idle;

        private readonly List<string> randomThoughts = new List<string>
        {
            "cậu đang làm gì thế?", "cậu đã uống nước chưa?", "tớ đang chờ cậu đây!"
        };

        public MainWindow()
        {
            InitializeComponent();
            DataManager.LoadData();

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (DataManager.Data.LastOpenedDate != today)
            {
                DataManager.Data.LastOpenedDate = today;
                DataManager.SaveData();
                CheckMorningEvents();
            }

            SetupTimers();
            SetupTrayIcon();
            ChangeState(PetState.Idle);

            physicsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            physicsTimer.Tick += PhysicsTimer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) => UpdateFrame();

        private void CheckMorningEvents()
        {
            DateTime today = DateTime.Today;
            var todayEvents = DataManager.Data.Events.Where(e => {
                if (e.Date.Date == today) return true;
                if (e.Date.Date > today) return false;
                return e.Repeat switch
                {
                    "Hàng tuần" => e.Date.DayOfWeek == today.DayOfWeek,
                    "Hàng tháng" => e.Date.Day == today.Day,
                    "Hàng năm" => e.Date.Day == today.Day && e.Date.Month == today.Month,
                    _ => false
                };
            }).ToList();

            if (todayEvents.Count > 0)
            {
                string eventNames = string.Join(", ", todayEvents.Select(ev => ev.Title));
                DispatcherTimer delayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                delayTimer.Tick += (s, e) => {
                    delayTimer.Stop();
                    new AstraNotificationWindow("🎉 Chào buổi sáng!", $"Hôm nay chúng ta có:\n{eventNames} nhé!").Show();
                };
                delayTimer.Start();
            }
        }

        private void SetupTimers()
        {
            randomThoughtTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            randomThoughtTimer.Tick += (s, e) =>
            {
                if (isFocusMode || chatWindow != null || isBigChatOpen || isSmol) return;
                string thought = randomThoughts[rand.Next(randomThoughts.Count)];
                new SpeechBubble(thought, this, 4000).Show();
            };
            randomThoughtTimer.Start();

            taskRadarTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            taskRadarTimer.Tick += TaskRadarTimer_Tick;
            taskRadarTimer.Start();

            animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(166) };
            animTimer.Tick += (s, e) =>
            {
                if (currentFrames.Length > 0 && !isSmol)
                {
                    currentFrameIndex = (currentFrameIndex + 1) % currentFrames.Length;
                    UpdateFrame();
                }
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (!DataManager.Data.Stats.ContainsKey(today)) DataManager.Data.Stats[today] = new DailyStat();
                DataManager.Data.Stats[today].ScreenTimeMinutes += (166.0 / 60000.0);
            };
            animTimer.Start();
        }

        private void SetupTrayIcon()
        {
            trayIcon = new System.Windows.Forms.NotifyIcon();
            string iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "icon.ico");
            if (File.Exists(iconPath)) trayIcon.Icon = new System.Drawing.Icon(iconPath);
            trayIcon.Text = "AstraCosmeris";
            trayIcon.Visible = true;

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("🤏 Thu nhỏ (Smol Astra)", null, (s, e) => ToggleSmolMode());
            contextMenu.Items.Add("🗂️ Open Dashboard", null, (s, e) => OpenDashboard());
            contextMenu.Items.Add("⚙️ Cài đặt (Settings)", null, (s, e) => {
                if (isFocusMode) return; OpenExclusiveWindow(new SettingsWindow());
            });
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add("Quit", null, (s, e) => this.Close());
            trayIcon.ContextMenuStrip = contextMenu;
        }

        public void ChangeState(PetState newState)
        {
            if (isSmol) return;
            currentState = newState;
            string folderName = newState.ToString().ToLower();
            string folderPath = Path.Combine(AppContext.BaseDirectory, "assets", folderName);
            if (Directory.Exists(folderPath)) currentFrames = Directory.GetFiles(folderPath, "*.*").Where(s => s.EndsWith(".png") || s.EndsWith(".jpg")).ToArray();
            else currentFrames = Array.Empty<string>();
            currentFrameIndex = 0;
        }

        private void UpdateFrame() { if (currentFrames.Length > 0 && !isSmol) PetImage.Source = new BitmapImage(new Uri(currentFrames[currentFrameIndex])); }

        // ==========================================
        // VẬT LÝ QUÁN TÍNH: XÁCH CỔ ASTRA LẮC LƯ
        // ==========================================
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
            {
                if (isSmol)
                {
                    physicsTimer?.Stop(); // Đang cầm thì rút điện vật lý cho nhẹ RAM
                    velX = 0; velY = 0;
                }
                _isDragging = true;
                _dragStartPos = PointToScreen(e.GetPosition(this));
                _lastMousePos = _dragStartPos;
                this.CaptureMouse();
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging)
            {
                System.Windows.Point currentPos = PointToScreen(e.GetPosition(this));
                double deltaX = currentPos.X - _dragStartPos.X;
                double deltaY = currentPos.Y - _dragStartPos.Y;

                this.Left += deltaX;
                this.Top += deltaY;

                if (isSmol)
                {
                    double instVelX = currentPos.X - _lastMousePos.X;
                    double instVelY = currentPos.Y - _lastMousePos.Y;

                    // Đo lực ném: Chỉ ghi nhận lực khi chuột đang di chuyển thật sự
                    // Nhân 2 để văng cho mượt, ném phát bay sang kia màn hình
                    if (Math.Abs(instVelX) > 0.5 || Math.Abs(instVelY) > 0.5)
                    {
                        velX = instVelX * 2.0;
                        velY = instVelY * 2.0;
                    }
                    _lastMousePos = currentPos;
                }
                else
                {
                    DragRotation.Angle = Math.Clamp(deltaX * 1.5, -30, 30);
                }

                _dragStartPos = currentPos;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.ReleaseMouseCapture();

                if (isSmol)
                {
                    // Thả chuột ra là cắm điện Timer Vật lý cho nó trượt ngay lập tức
                    physicsTimer?.Start();
                }
                else
                {
                    DoubleAnimation springAnim = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(800),
                        EasingFunction = new ElasticEase { Oscillations = 3, Springiness = 5, EasingMode = EasingMode.EaseOut }
                    };
                    DragRotation.BeginAnimation(RotateTransform.AngleProperty, springAnim);
                }
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (isSmol) { ToggleSmolMode(); return; } // Double click để thoát Smol Mode
            if (isFocusMode) return;
            if (e.ChangedButton == MouseButton.Left)
            {
                ChangeState(PetState.Happy);

                // HOT FIX: Vá lỗi kẹt cờ khiến không mở được ChatInput
                if (isBigChatOpen)
                {
                    if (bigChatWindow != null && bigChatWindow.IsLoaded) { bigChatWindow.Activate(); return; }
                    else { isBigChatOpen = false; bigChatWindow = null; } // Giải phóng cờ kẹt!
                }

                if (chatWindow == null || !chatWindow.IsLoaded)
                {
                    chatWindow = new ChatInputWindow(this);
                    chatWindow.Closed += (s, args) => chatWindow = null;
                    chatWindow.Show();
                }
                else { chatWindow.Close(); chatWindow = null; }
            }
        }

        // ==========================================
        // TRÌNH QUẢN LÝ CỬA SỔ ĐỘC QUYỀN (FLYOUT)
        // ==========================================
        public void OpenExclusiveWindow(Window newWin)
        {
            if (isFocusMode) return;

            var wa = SystemParameters.WorkArea;
            newWin.WindowStartupLocation = WindowStartupLocation.Manual;

            if (currentExclusiveWindow != null && currentExclusiveWindow.IsVisible)
            {
                // Cửa sổ cũ rớt xuống đáy màn hình
                DoubleAnimation slideDown = new DoubleAnimation
                {
                    To = wa.Bottom + 50,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                slideDown.Completed += (s, e) => {
                    currentExclusiveWindow.Hide();
                    if (currentExclusiveWindow is SettingsWindow || currentExclusiveWindow is PomodoroSetupWindow)
                        currentExclusiveWindow.Close();
                    ShowNewWindow(newWin, wa);
                };
                currentExclusiveWindow.BeginAnimation(Window.TopProperty, slideDown);
            }
            else
            {
                ShowNewWindow(newWin, wa);
            }
        }

        private void ShowNewWindow(Window newWin, Rect wa)
        {
            currentExclusiveWindow = newWin;
            newWin.Left = wa.Left + (wa.Width - newWin.Width) / 2;
            newWin.Top = -newWin.Height - 50; // Giấu tuốt trên đỉnh đầu
            newWin.Show();

            double targetTop = wa.Top + (wa.Height - newWin.Height) / 2; // Rơi xuống giữa màn hình
            DoubleAnimation slideIn = new DoubleAnimation
            {
                To = targetTop,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            newWin.BeginAnimation(Window.TopProperty, slideIn);
        }

        public void CloseExclusiveWindow()
        {
            if (currentExclusiveWindow != null && currentExclusiveWindow.IsVisible)
            {
                var wa = SystemParameters.WorkArea;
                DoubleAnimation slideDown = new DoubleAnimation
                {
                    To = wa.Bottom + 50,
                    Duration = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                slideDown.Completed += (s, e) => {
                    currentExclusiveWindow.Hide();
                    if (currentExclusiveWindow is SettingsWindow || currentExclusiveWindow is PomodoroSetupWindow)
                        currentExclusiveWindow.Close();
                    currentExclusiveWindow = null;
                };
                currentExclusiveWindow.BeginAnimation(Window.TopProperty, slideDown);
            }
        }

        public void ToggleSmolMode()
        {
            if (!isSmol)
            {
                isSmol = true;
                originalWidth = this.Width; originalHeight = this.Height;
                this.Width = 90; this.Height = 90;
                string path = Path.Combine(AppContext.BaseDirectory, "assets", "rolling", "rolling.png");
                if (File.Exists(path)) PetImage.Source = new BitmapImage(new Uri(path));
                velY = 0; velX = (rand.NextDouble() - 0.5) * 15; // Ném văng sang 2 bên
                physicsTimer?.Start();
            }
            else
            {
                isSmol = false;
                physicsTimer?.Stop();
                this.Width = originalWidth; this.Height = originalHeight;
                ChangeState(PetState.Idle);
            }
        }

        private void PhysicsTimer_Tick(object? sender, EventArgs e)
        {
            if (_isDragging) return;

            // Áp dụng ma sát làm giảm dần vận tốc (BumpTop Effect, lơ lửng mọi nơi)
            velX *= friction;
            velY *= friction;
            this.Left += velX;
            this.Top += velY;

            var wa = SystemParameters.WorkArea;
            bool hit = false;

            // Xử lý nảy đập 4 bức tường
            if (this.Top < wa.Top) { this.Top = wa.Top; velY *= bounce; hit = true; }
            else if (this.Top + this.Height > wa.Bottom) { this.Top = wa.Bottom - this.Height; velY *= bounce; hit = true; }
            if (this.Left < wa.Left) { this.Left = wa.Left; velX *= bounce; hit = true; }
            else if (this.Left + this.Width > wa.Right) { this.Left = wa.Right - this.Width; velX *= bounce; hit = true; }

            if (hit && (Math.Abs(velX) + Math.Abs(velY) > 5) && rand.Next(10) < 3)
            {
                string[] cries = { "Á!", "Ui da!", "Đau tớ!", "Cứu!" };
                new SpeechBubble(cries[rand.Next(cries.Length)], this, 1000).Show();
            }

            // MA THUẬT TIẾT KIỆM RAM: Khi nó trượt chậm lại và dừng hẳn, TẮT TIMER!
            if (Math.Abs(velX) < 0.5 && Math.Abs(velY) < 0.5) physicsTimer?.Stop();
        }

        public void OpenDashboard() { if (isFocusMode) return; DataManager.TrackDashboardOpen(); if (dashboard == null || !dashboard.IsLoaded) { dashboard = new DashboardWindow(); } OpenExclusiveWindow(dashboard); }
        public void CloseAllChats() { chatWindow?.Close(); chatWindow = null; bigChatWindow?.Close(); bigChatWindow = null; isBigChatOpen = false; }
        private void MenuSmol_Click(object sender, RoutedEventArgs e) => ToggleSmolMode();
        private void MenuBigChat_Click(object sender, RoutedEventArgs e)
        {
            if (!isFocusMode && !isBigChatOpen)
            {
                isBigChatOpen = true;
                bigChatWindow = new ChatHistoryWindow(this); // Gán vào biến hệ thống
                bigChatWindow.Closed += (s, args) => { isBigChatOpen = false; bigChatWindow = null; ChangeState(PetState.Idle); };
                OpenExclusiveWindow(bigChatWindow); // Mở bằng hiệu ứng bay từ trên xuống
            }
        }
        private void MenuDashboard_Click(object sender, RoutedEventArgs e) => OpenDashboard();
        private void MenuPomodoro_Click(object sender, RoutedEventArgs e) { if (!isFocusMode) OpenExclusiveWindow(new PomodoroSetupWindow(this)); }
        private void MenuSettings_Click(object sender, RoutedEventArgs e) { if (!isFocusMode) OpenExclusiveWindow(new SettingsWindow()); }
        private void MenuExit_Click(object sender, RoutedEventArgs e) => this.Close();

        private void TaskRadarTimer_Tick(object? sender, EventArgs e)
        {
            string currentMinute = DateTime.Now.ToString("HH:mm");
            if (currentMinute == lastTriggeredMinute) return;
            string todayKey = DateTime.Today.ToString("yyyy-MM-dd");
            if (DataManager.Data.Tasks.TryGetValue(todayKey, out string? todayTasks))
            {
                string[] lines = todayTasks.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].Trim();
                    if (trimmed.StartsWith("[") && trimmed.Length >= 7 && trimmed[6] == ']')
                    {
                        if (trimmed.Substring(1, 5) == currentMinute)
                        {
                            TriggerReminder(trimmed.Substring(7).Trim(), todayKey, i, lines);
                            lastTriggeredMinute = currentMinute;
                            return;
                        }
                    }
                }
            }
        }

        public void TriggerReminder(string task, string dateKey, int lineIndex, string[] allLines)
        {
            bool isHandled = false;

            StartReminderStorm($"⚠️ {task}\nĐÃ BẢO LÀ LÀM NGAY ĐI CƠ MÀ!!!");

            var notif = new AstraNotificationWindow(
                title: "⏰ Tới giờ làm việc!",
                message: $"Nhiệm vụ: {task}\nBấm vào đây để tắt báo động!",
                btn1Text: "✅ Làm ngay",
                action1: () => { isHandled = true; StopReminderStorm(); },
                btn2Text: "⏳ Lùi 5 phút",
                action2: () => {
                    isHandled = true; StopReminderStorm();
                    DateTime newTime = DateTime.Now.AddMinutes(5);
                    allLines[lineIndex] = $"[{newTime:HH:mm}] {task}";
                    DataManager.Data.Tasks[dateKey] = string.Join(Environment.NewLine, allLines);
                    DataManager.SaveData();
                }
            );

            notif.Closed += (s, e) => {
                if (!isHandled) TriggerReminder(task, dateKey, lineIndex, allLines);
            };
            notif.Show();
        }

        private void StartReminderStorm(string text)
        {
            if (spamTimer != null && spamTimer.IsEnabled) return;
            spamTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            spamTimer.Tick += (s, e) =>
            {
                if (activePopups.Count >= 50) { activePopups[0].Close(); activePopups.RemoveAt(0); }
                var popup = new ReminderPopup(text);
                popup.Show();
                activePopups.Add(popup);
            };
            spamTimer.Start();
        }

        private void StopReminderStorm()
        {
            spamTimer?.Stop();
            foreach (var popup in activePopups) popup.Close();
            activePopups.Clear();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!forceClose)
            {
                e.Cancel = true;
                var existingExitWin = System.Windows.Application.Current.Windows.OfType<ExitConfirmWindow>().FirstOrDefault();
                if (existingExitWin != null) { existingExitWin.Activate(); return; }
                new ExitConfirmWindow(this).Show();
            }
            else { if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); } }
        }
    }
}