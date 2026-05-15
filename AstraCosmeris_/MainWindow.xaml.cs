using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        private DispatcherTimer? randomThoughtTimer;
        private DispatcherTimer? taskRadarTimer;
        private DispatcherTimer? animTimer;

        private System.Windows.Forms.NotifyIcon? trayIcon;
        private DashboardWindow? dashboard;
        private ChatInputWindow? chatWindow;
        private ChatHistoryWindow? bigChatWindow;

        private string lastTriggeredMinute = "";
        private Random rand = new Random();

        // BIẾN QUẢN LÝ TRÒ CHƠI "BẮT NÚT OK"
        private List<ReminderPopup> activePopups = new List<ReminderPopup>();
        private DispatcherTimer? spamTimer;
        private Window? stopButtonWindow;

        private string[] currentFrames = Array.Empty<string>();
        private int currentFrameIndex = 0;
        private PetState currentState = PetState.Idle;

        private readonly List<string> randomThoughts = new List<string>
        {
            "cậu đang làm gì thế?", "cậu đã uống nước chưa?", "tớ đang chờ cậu đây!",
            "hãy cùng nhau làm việc nhé!", "hãy làm việc đi nhé!"
        };

        public MainWindow()
        {
            InitializeComponent();
            DataManager.LoadData();

            // XỬ LÝ CHUẨN BỊ CHO BÁO CÁO (Daily Report) VÀ LỜI CHÀO SỰ KIỆN
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (DataManager.Data.LastOpenedDate != today)
            {
                DataManager.Data.LastOpenedDate = today;
                DataManager.SaveData();

                // 1. GỌI LỜI CHÀO SỰ KIỆN BUỔI SÁNG
                CheckMorningEvents();

                // 2. GỌI REPORT (Phase tới mình sẽ đẻ cửa sổ Report ở đây)
            }

            SetupTimers();
            SetupTrayIcon();
            ChangeState(PetState.Idle);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) => UpdateFrame();

        // ==========================================
        // MA THUẬT 1: LỜI CHÀO SỰ KIỆN ĐẦU NGÀY
        // ==========================================
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

                // Trì hoãn 2 giây cho app load xong UI rồi mới bắn Toast ra cho mượt
                DispatcherTimer delayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                delayTimer.Tick += (s, e) => {
                    delayTimer.Stop();
                    new AstraNotificationWindow(
                        "🎉 Chào buổi sáng!",
                        $"Cậu đừng quên hôm nay chúng ta có:\n{eventNames} nhé!"
                    ).Show();
                };
                delayTimer.Start();
            }
        }

        private void SetupTimers()
        {
            randomThoughtTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            randomThoughtTimer.Tick += (s, e) =>
            {
                if (isFocusMode || chatWindow != null || isBigChatOpen) return;
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
                if (currentFrames.Length > 0)
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
            contextMenu.Items.Add("🗂️ Open Dashboard", null, (s, e) => OpenDashboard());
            contextMenu.Items.Add("⚙️ Cài đặt (Settings)", null, (s, e) => {
                if (isFocusMode) { System.Windows.MessageBox.Show("Đang focus, không được phân tâm nhé!", "Astra"); return; }
                new SettingsWindow().Show();
            });
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add("Quit", null, (s, e) => this.Close());

            trayIcon.ContextMenuStrip = contextMenu;
        }

        public void ChangeState(PetState newState)
        {
            currentState = newState;
            string folderName = newState.ToString().ToLower();
            string folderPath = Path.Combine(AppContext.BaseDirectory, "assets", folderName);
            if (Directory.Exists(folderPath))
            {
                currentFrames = Directory.GetFiles(folderPath, "*.*").Where(s => s.EndsWith(".png") || s.EndsWith(".jpg")).ToArray();
            }
            else currentFrames = Array.Empty<string>();
            currentFrameIndex = 0;
        }

        private void UpdateFrame() { if (currentFrames.Length > 0) PetImage.Source = new BitmapImage(new Uri(currentFrames[currentFrameIndex])); }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left && e.ClickCount != 2 && e.ButtonState == MouseButtonState.Pressed) this.DragMove(); }
        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (isFocusMode) return;
            if (e.ChangedButton == MouseButton.Left)
            {
                ChangeState(PetState.Happy);
                if (isBigChatOpen) { bigChatWindow?.Activate(); return; }
                if (chatWindow == null)
                {
                    chatWindow = new ChatInputWindow(this);
                    chatWindow.Closed += (s, args) => chatWindow = null;
                    chatWindow.Show();
                }
                else { chatWindow.Close(); chatWindow = null; }
            }
        }

        public void OpenDashboard()
        {
            if (isFocusMode) { System.Windows.MessageBox.Show("Đang focus, không được lướt Dashboard!", "Astra"); return; }
            DataManager.TrackDashboardOpen();
            if (dashboard == null || !dashboard.IsLoaded) { dashboard = new DashboardWindow(); dashboard.Show(); }
            else { dashboard.Activate(); if (dashboard.WindowState == WindowState.Minimized) dashboard.WindowState = WindowState.Normal; }
        }

        public void CloseAllChats() { chatWindow?.Close(); chatWindow = null; bigChatWindow?.Close(); bigChatWindow = null; isBigChatOpen = false; }

        private void MenuBigChat_Click(object sender, RoutedEventArgs e)
        {
            if (isFocusMode) return;
            if (!isBigChatOpen)
            {
                isBigChatOpen = true;
                var bigChat = new ChatHistoryWindow(this);
                bigChat.Closed += (s, args) => { isBigChatOpen = false; ChangeState(PetState.Idle); };
                bigChat.Show();
            }
        }
        private void MenuDashboard_Click(object sender, RoutedEventArgs e) => OpenDashboard();
        private void MenuPomodoro_Click(object sender, RoutedEventArgs e) { if (!isFocusMode) new PomodoroSetupWindow(this).Show(); dashboard?.Hide(); }
        private void MenuSettings_Click(object sender, RoutedEventArgs e) { if (!isFocusMode) new SettingsWindow().Show(); }
        private void MenuExit_Click(object sender, RoutedEventArgs e) => this.Close();

        // ==========================================
        // MA THUẬT 2: RADAR & THAO TÚNG TÂM LÝ BẠO LỰC
        // ==========================================
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
                        string taskTime = trimmed.Substring(1, 5);
                        if (taskTime == currentMinute)
                        {
                            string taskContent = trimmed.Substring(7).Trim();
                            // Đẩy cả mảng dòng vào để lát nó còn biết sửa file nếu lùi 5 phút
                            TriggerReminder(taskContent, todayKey, i, lines);
                            lastTriggeredMinute = currentMinute;
                            return;
                        }
                    }
                }
            }
        }

        public void TriggerReminder(string task, string dateKey, int lineIndex, string[] allLines)
        {
            bool isHandled = false; // Cờ kiểm tra thái độ user

            var notif = new AstraNotificationWindow(
                title: "⏰ Tới giờ làm việc!",
                message: $"Nhiệm vụ: {task}\nCậu định làm ngay hay muốn lùi lại?",
                btn1Text: "✅ Làm ngay",
                action1: () => {
                    isHandled = true;
                    // Chấp hành ngoan ngoãn -> Tha cho
                },
                btn2Text: "⏳ Lùi 5 phút",
                action2: () => {
                    isHandled = true;
                    // M lười à? Ok sửa lại Database cộng thêm 5 phút!
                    DateTime newTime = DateTime.Now.AddMinutes(5);
                    allLines[lineIndex] = $"[{newTime:HH:mm}] {task}";
                    DataManager.Data.Tasks[dateKey] = string.Join(Environment.NewLine, allLines);
                    DataManager.SaveData();
                }
            );

            // Bắt sự kiện khi Toast đóng lại
            notif.Closed += (s, e) => {
                // Nếu Toast đóng mà cờ vẫn false (nghĩa là Toast tự tắt do hết thời gian, user lờ đi)
                if (!isHandled)
                {
                    StartReminderStorm($"⚠️ {task}\nĐÃ BẢO LÀ LÀM NGAY ĐI CƠ MÀ!!!");
                }
            };

            notif.Show();
        }

        // ==========================================
        // KHU VỰC GAME BẠO LỰC (GIỮ NGUYÊN)
        // ==========================================
        private void StartReminderStorm(string text)
        {
            if (spamTimer != null && spamTimer.IsEnabled) return;

            stopButtonWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                Width = 180,
                Height = 60
            };

            System.Windows.Controls.Button btnStop = new System.Windows.Controls.Button
            {
                Content = "🛑 TỚ BIẾT RỒI!",
                Background = System.Windows.Media.Brushes.Red,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnStop.Resources.Add(typeof(Border), new Style(typeof(Border)) { Setters = { new Setter(Border.CornerRadiusProperty, new CornerRadius(15)) } });
            btnStop.Click += (s, e) => StopReminderStorm();
            stopButtonWindow.Content = btnStop;

            DispatcherTimer jumpTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            jumpTimer.Tick += (s, e) => MoveWindowToRandom(stopButtonWindow);
            jumpTimer.Start();

            stopButtonWindow.Closed += (s, e) => jumpTimer.Stop();
            stopButtonWindow.Show();
            MoveWindowToRandom(stopButtonWindow);

            spamTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            spamTimer.Tick += (s, e) =>
            {
                if (activePopups.Count >= 50)
                {
                    activePopups[0].Close();
                    activePopups.RemoveAt(0);
                }
                var popup = new ReminderPopup(text);
                popup.Show();
                activePopups.Add(popup);
            };
            spamTimer.Start();
        }

        private void MoveWindowToRandom(Window w)
        {
            var workArea = SystemParameters.WorkArea;
            w.Left = rand.Next((int)workArea.Left, (int)(workArea.Right - w.Width));
            w.Top = rand.Next((int)workArea.Top, (int)(workArea.Bottom - w.Height));
        }

        private void StopReminderStorm()
        {
            spamTimer?.Stop();
            stopButtonWindow?.Close();
            stopButtonWindow = null;
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