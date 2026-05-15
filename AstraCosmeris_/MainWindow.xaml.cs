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

            // XỬ LÝ CHUẨN BỊ CHO BÁO CÁO (Daily Report)
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (DataManager.Data.LastOpenedDate != today)
            {
                // Gọi hàm đẻ Report ở đây (Phase sau sẽ làm)
                DataManager.Data.LastOpenedDate = today;
                DataManager.SaveData();
            }

            SetupTimers();
            SetupTrayIcon();
            ChangeState(PetState.Idle);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) => UpdateFrame();

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

                // Thu thập thời gian sử dụng app (Tracking Screen Time)
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
            DataManager.TrackDashboardOpen(); // Track Report
            if (dashboard == null || !dashboard.IsLoaded) { dashboard = new DashboardWindow(); dashboard.Show(); }
            else { dashboard.Show();  dashboard.Activate(); if (dashboard.WindowState == WindowState.Minimized) dashboard.WindowState = WindowState.Normal; }
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
        // HOT FIX: SỬA LỖI RADAR ĐỂ NÓ HOẠT ĐỘNG
        // ==========================================
        private void TaskRadarTimer_Tick(object? sender, EventArgs e)
        {
            string currentMinute = DateTime.Now.ToString("HH:mm");
            if (currentMinute == lastTriggeredMinute) return;

            string todayKey = DateTime.Today.ToString("yyyy-MM-dd");
            if (DataManager.Data.Tasks.TryGetValue(todayKey, out string? todayTasks))
            {
                string[] lines = todayTasks.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("[") && trimmed.Length >= 7 && trimmed[6] == ']')
                    {
                        string taskTime = trimmed.Substring(1, 5);
                        if (taskTime == currentMinute)
                        {
                            string taskContent = trimmed.Substring(7).Trim();
                            TriggerReminder(taskContent);
                            lastTriggeredMinute = currentMinute;
                            break;
                        }
                    }
                }
            }
        }

        public void TriggerReminder(string task) => StartReminderStorm($"⚠️ {task}\nLÀM NGAY VÀ LUÔN!!!");

        // ==========================================
        // TÍNH NĂNG ĐIÊN RỒ: POPUPS VÔ HẠN & NÚT OK NHẢY
        // ==========================================
        private void StartReminderStorm(string text)
        {
            if (spamTimer != null && spamTimer.IsEnabled) return;

            // 1. Tạo Nút Stop Thần Thánh
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
                FontWeight = System.Windows.FontWeights.Bold,
                FontSize = 16,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnStop.Resources.Add(typeof(Border), new Style(typeof(Border)) { Setters = { new Setter(Border.CornerRadiusProperty, new CornerRadius(15)) } });
            btnStop.Click += (s, e) => StopReminderStorm();
            stopButtonWindow.Content = btnStop;

            // 2. Timer Nảy nút (Cứ 3.5s nhảy 1 phát)
            DispatcherTimer jumpTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            jumpTimer.Tick += (s, e) => MoveWindowToRandom(stopButtonWindow);
            jumpTimer.Start();

            stopButtonWindow.Closed += (s, e) => jumpTimer.Stop();
            stopButtonWindow.Show();
            MoveWindowToRandom(stopButtonWindow);

            // 3. Timer Xả rác (0.4s đẻ 1 cái)
            spamTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            spamTimer.Tick += (s, e) =>
            {
                // Giới hạn max 50 cái trên màn hình để khỏi treo RAM
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