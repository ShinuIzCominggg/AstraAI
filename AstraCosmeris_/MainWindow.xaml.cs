using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public enum PetState
    {
        Idle,
        Thinking,
        Happy
    }

    public partial class MainWindow : Window
    {
        // --- QUẢN LÝ BIẾN TOÀN CỤC ---
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
        private List<ReminderPopup> activePopups = new List<ReminderPopup>();

        private string[] currentFrames = Array.Empty<string>();
        private int currentFrameIndex = 0;
        private PetState currentState = PetState.Idle;

        private readonly List<string> randomThoughts = new List<string>
        {
            "cậu đang làm gì thế?", "cậu đã uống nước chưa?", "tớ đang chờ cậu đây!",
            "cậu xem mình còn công việc gì không nhé.", "ngày hôm nay của cậu thế nào?",
            "hãy cùng nhau làm việc nhé!", "hmmm..", "chán thật đó!",
            "cậu có đang rảnh rỗi không?", "hãy làm việc đi nhé!",
            "hãy nghỉ ngơi và ngủ đủ nhé! đừng để bản thân mệt mỏi.",
            "đang làm gì đó?", "tớ vẫn đang ở đây nhé!", "tớ vẫn đang chờ cậu đây!!",
            "hôm nay cậu cần tớ giúp gì nhỉ?", "tớ luôn ở đây hỗ trợ cậu!!"
        };

        public MainWindow()
        {
            InitializeComponent();
            MemoryManager.LoadMemory();

            SetupTimers();
            SetupTrayIcon();
            ChangeState(PetState.Idle);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) => UpdateFrame();

        // ==========================================
        // 1. SETUP & KHỞI TẠO
        // ==========================================
        private void SetupTimers()
        {
            // Timer Lảm Nhảm
            randomThoughtTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            randomThoughtTimer.Tick += (s, e) =>
            {
                if (isFocusMode || chatWindow != null || isBigChatOpen) return;
                string thought = randomThoughts[rand.Next(randomThoughts.Count)];
                new SpeechBubble(thought, this, 4000).Show();
            };
            randomThoughtTimer.Start();

            // Radar quét Task
            taskRadarTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            taskRadarTimer.Tick += TaskRadarTimer_Tick;
            taskRadarTimer.Start();

            // Timer Animation
            animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(166) }; // ~6 FPS
            animTimer.Tick += (s, e) =>
            {
                if (currentFrames.Length > 0)
                {
                    currentFrameIndex = (currentFrameIndex + 1) % currentFrames.Length;
                    UpdateFrame();
                }
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

            contextMenu.Items.Add("📝 Tạo Note Nhanh", null, (s, e) => {
                if (NoteManager.AllNotes.Count == 0) NoteManager.LoadNotes();
                var newNote = new AstraNote { Content = "Ghi chú nhanh...", IsFloating = true, Left = 100, Top = 100 };
                NoteManager.AllNotes.Add(newNote);
                NoteManager.SaveNotes();
                new FloatingNoteWindow(newNote).Show();
            });

            contextMenu.Items.Add("🗂️ Open Dashboard", null, (s, e) => OpenDashboard());
            contextMenu.Items.Add("⚙️ Cài đặt (Settings)", null, (s, e) => {
                if (isFocusMode) { System.Windows.MessageBox.Show("Đang focus, không được phân tâm nhé!", "Astra"); return; }
                new SettingsWindow().Show();
            });

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add("Quit", null, (s, e) => this.Close());

            trayIcon.ContextMenuStrip = contextMenu;
        }

        // ==========================================
        // 2. HỆ THỐNG ANIMATION
        // ==========================================
        public void ChangeState(PetState newState)
        {
            currentState = newState;
            string folderName = newState.ToString().ToLower();
            string folderPath = Path.Combine(AppContext.BaseDirectory, "assets", folderName);

            if (Directory.Exists(folderPath))
            {
                currentFrames = Directory.GetFiles(folderPath, "*.*")
                    .Where(s => s.EndsWith(".png") || s.EndsWith(".jpg")).ToArray();
            }
            else currentFrames = Array.Empty<string>();

            currentFrameIndex = 0;
        }

        private void UpdateFrame()
        {
            if (currentFrames.Length > 0)
                PetImage.Source = new BitmapImage(new Uri(currentFrames[currentFrameIndex]));
        }

        // ==========================================
        // 3. TƯƠNG TÁC CHUỘT & WINDOW
        // ==========================================
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount != 2 && e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (isFocusMode) return;
            if (e.ChangedButton == MouseButton.Left)
            {
                ChangeState(PetState.Happy);

                if (isBigChatOpen)
                {
                    bigChatWindow?.Activate();
                    return;
                }

                if (chatWindow == null)
                {
                    chatWindow = new ChatInputWindow(this);
                    chatWindow.Closed += (s, args) => chatWindow = null;
                    chatWindow.Show();
                }
                else
                {
                    chatWindow.Close();
                    chatWindow = null;
                }
            }
        }

        public void OpenDashboard()
        {
            if (isFocusMode)
            {
                System.Windows.MessageBox.Show("Đang focus, không được lướt Dashboard!", "Astra");
                return;
            }

            if (dashboard == null || !dashboard.IsLoaded)
            {
                dashboard = new DashboardWindow();
                dashboard.Show();
            }
            else
            {
                dashboard.Activate();
                if (dashboard.WindowState == WindowState.Minimized) dashboard.WindowState = WindowState.Normal;
            }
        }

        public void CloseAllChats()
        {
            chatWindow?.Close(); chatWindow = null;
            bigChatWindow?.Close(); bigChatWindow = null;
            isBigChatOpen = false;
        }

        // ==========================================
        // 4. HỆ THỐNG MENU (CONTEXT MENU)
        // ==========================================
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
        private void MenuPomodoro_Click(object sender, RoutedEventArgs e)
        {
            if (isFocusMode) return;
            new PomodoroSetupWindow(this).Show();
            dashboard?.Hide();
        }
        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            if (isFocusMode) return;
            new SettingsWindow().Show();
        }

        // ==========================================
        // 5. RADAR & NHẮC NHỞ (REMINDER)
        // ==========================================
        private void TaskRadarTimer_Tick(object? sender, EventArgs e)
        {
            string currentMinute = DateTime.Now.ToString("HH:mm");
            if (currentMinute == lastTriggeredMinute) return;

            string tasksFilePath = Path.Combine(AppContext.BaseDirectory, "tasks.json");
            if (!File.Exists(tasksFilePath)) return;

            try
            {
                var allTasks = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(tasksFilePath));
                if (allTasks == null) return;

                string todayKey = DateTime.Today.ToString("yyyy-MM-dd");
                if (allTasks.TryGetValue(todayKey, out string? todayTasks))
                {
                    string[] lines = todayTasks.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (line.Trim().StartsWith("[") && line.Length >= 7 && line[6] == ']')
                        {
                            string taskTime = line.Substring(1, 5);
                            if (taskTime == currentMinute)
                            {
                                string taskContent = line.Substring(7).Trim();
                                TriggerReminder(taskContent);
                                lastTriggeredMinute = currentMinute;
                                break;
                            }
                        }
                    }
                }
            }
            catch { /* Im lặng cho qua */ }
        }

        public async void TriggerReminder(string task)
        {
            await StartReminderStormAsync($"⚠️ {task}\nLÀM NGAY VÀ LUÔN!!!");
        }

        private async Task StartReminderStormAsync(string text)
        {
            int maxCycles = 5, spamDurationMs = 3000, clearDurationMs = 5000;

            for (int i = 0; i < maxCycles; i++)
            {
                long endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + spamDurationMs;
                while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < endTime)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        var popup = new ReminderPopup(text, spamDurationMs);
                        popup.Show();
                        activePopups.Add(popup);
                    }
                    await Task.Delay(20);
                }

                foreach (var popup in activePopups) popup?.Close();
                activePopups.Clear();

                if (i < maxCycles - 1) await Task.Delay(clearDurationMs);
            }
        }

        // ==========================================
        // 6. XỬ LÝ SỰ KIỆN ĐÓNG APP
        // ==========================================
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!forceClose)
            {
                e.Cancel = true;
                var existingExitWin = System.Windows.Application.Current.Windows.OfType<ExitConfirmWindow>().FirstOrDefault();
                if (existingExitWin != null)
                {
                    existingExitWin.Activate();
                    return;
                }
                new ExitConfirmWindow(this).Show();
            }
            else
            {
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
            }
        }
    }
}