using AstraCosmeris_;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AstraCosmeris_
{
    // Tạo danh sách trạng thái cho Astra
    public enum PetState
    {
        Idle,
        Thinking,
        Happy
    }

    public partial class MainWindow : Window
    {
        // --- TRÙM CUỐI: RANDOM THOUGHTS ---
        private System.Windows.Threading.DispatcherTimer randomThoughtTimer;
        private System.Windows.Threading.DispatcherTimer taskRadarTimer;
        private string lastTriggeredMinute = "";
        private Random rand = new Random();
        public bool isFocusMode = false;
        private List<string> randomThoughts = new List<string>
        {
            "cậu đang làm gì thế?",
            "cậu đã uống nước chưa?",
            "tớ đang chờ cậu đây!",
            "cậu xem mình còn công việc gì không nhé.",
            "ngày hôm nay của cậu thế nào?",
            "hãy cùng nhau làm việc nhé!",
            "hmmm..",
            "chán thật đó!",
            "cậu có đang rảnh rỗi không?",
            "hãy làm việc đi nhé!",
            "hãy nghỉ ngơi và ngủ đủ nhé! đừng để bản thân mệt mỏi.",
            "đang làm gì đó?",
            "tớ vẫn đang ở đây nhé!",
            "tớ vẫn đang chờ cậu đây!!",
            "hôm nay cậu cần tớ giúp gì nhỉ?",
            "tớ luôn ở đây hỗ trợ cậu!!"
        };
        // Fix cảnh báo Non-nullable bằng dấu "?"
        private System.Windows.Forms.NotifyIcon? trayIcon;
        private DispatcherTimer? animTimer;

        // Quản lý animation
        private string[] currentFrames = Array.Empty<string>();
        private int currentFrameIndex = 0;
        private PetState currentState = PetState.Idle;

        private DashboardWindow? dashboard;

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
                dashboard.Activate(); // Có rồi thì lôi lên trên cùng
                if (dashboard.WindowState == WindowState.Minimized) dashboard.WindowState = WindowState.Normal;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            // Cài đặt Timer lảm nhảm (30 giây nổ 1 lần)
            randomThoughtTimer = new System.Windows.Threading.DispatcherTimer();
            randomThoughtTimer.Interval = TimeSpan.FromSeconds(30);
            randomThoughtTimer.Tick += (s, e) =>
            {
                // FIX LỖI 2: Đang focus cấm nói chuyện!
                if (isFocusMode) return;

                if (chatWindow != null || isBigChatOpen) return;

                // Bốc đại 1 câu khịa
                string thought = randomThoughts[rand.Next(randomThoughts.Count)];

                // ĐẺ RA MỘT BONG BÓNG MỚI (Hiện trong 4000ms = 4 giây)
                SpeechBubble randomBubble = new SpeechBubble(thought, this, 4000);
                randomBubble.Show();
            };
            randomThoughtTimer.Start();
            // --- Cài đặt Radar quét Task (Quét mỗi 10 giây) ---
            taskRadarTimer = new System.Windows.Threading.DispatcherTimer();
            taskRadarTimer.Interval = TimeSpan.FromSeconds(10);
            taskRadarTimer.Tick += TaskRadarTimer_Tick;
            taskRadarTimer.Start();
            SetupTrayIcon();
            SetupAnimationTimer();

            // Nạp não từ file JSON
            MemoryManager.LoadMemory();

            ChangeState(PetState.Idle);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateFrame(); // Vẽ frame đầu tiên khi load xong Window
        }

        // --- HỆ THỐNG ANIMATION THEO TRẠNG THÁI ---
        public void ChangeState(PetState newState)
        {
            currentState = newState;
            // Tên folder sẽ trùng tên trạng thái viết thường (idle, thinking, happy)
            string folderName = newState.ToString().ToLower();
            // ĐỔI THÀNH AppContext.BaseDirectory
            string folderPath = Path.Combine(AppContext.BaseDirectory, "assets", folderName);

            // Quét thư mục lấy ảnh
            if (Directory.Exists(folderPath))
            {
                currentFrames = Directory.GetFiles(folderPath, "*.*")
                    .Where(s => s.EndsWith(".png") || s.EndsWith(".jpg"))
                    .ToArray();
            }
            else
            {
                currentFrames = Array.Empty<string>(); // Không có thư mục thì nhịn
            }

            currentFrameIndex = 0; // Reset lại từ frame 0
        }

        private void UpdateFrame()
        {
            if (currentFrames.Length > 0)
            {
                // Thay source ảnh bằng frame hiện tại
                PetImage.Source = new BitmapImage(new Uri(currentFrames[currentFrameIndex]));
            }
        }

        private void SetupAnimationTimer()
        {
            animTimer = new DispatcherTimer();
            animTimer.Interval = TimeSpan.FromMilliseconds(166); // Tầm 6 FPS
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

        // --- 1. KÉO THẢ PET ---
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Bỏ qua lệnh kéo thả nếu t đang click đúp (ClickCount == 2)
                if (e.ClickCount == 2)
                    return;

                // Chắc cú thêm lần nữa là chuột đang thực sự được nhấn
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            }
        }

        // --- 2. NHÁY ĐÚP MỞ/TẮT CHAT ---
        private ChatInputWindow? chatWindow;
        private ChatHistoryWindow? bigChatWindow;
        public bool isBigChatOpen = false;

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (isFocusMode) return;
            if (e.ChangedButton == MouseButton.Left)
            {
                ChangeState(PetState.Happy);

                // Nếu Chat bự đang mở rồi thì kéo nó lên trên cùng
                if (isBigChatOpen)
                {
                    if (bigChatWindow != null) bigChatWindow.Activate();
                    return;
                }

                // --- NẾU M TẠM THỜI CHƯA MUỐN XÓA CHAT NHỎ, THÌ CỨ ĐỂ ĐOẠN NÀY LÀ ĐƯỢC ---
                // (Sau này chán chat nhỏ thì vứt đi, thay bằng code mở bigChatWindow)
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

        // --- 3. SYSTEM TRAY & MENU ---
        private void SetupTrayIcon()
        {
            trayIcon = new System.Windows.Forms.NotifyIcon();

            // ĐỔI THÀNH AppContext.BaseDirectory
            string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "assets", "icon.ico");

            try
            {
                if (System.IO.File.Exists(iconPath))
                {
                    trayIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    System.Windows.MessageBox.Show("Astra không tìm thấy file Icon tại:\n" + iconPath, "Lỗi đường dẫn", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi load Icon: " + ex.Message);
            }

            trayIcon.Text = "AstraCosmeris";
            trayIcon.Visible = true;

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            // --- NÚT TẠO NOTE BAY LƠ LỬNG TỪ TRAY ---
            contextMenu.Items.Add("📝 Tạo Note Nhanh", null, (s, e) => {
                // Đảm bảo dữ liệu đã được nạp
                if (NoteManager.AllNotes.Count == 0) NoteManager.LoadNotes();

                // Đẻ ra một cái Note mới, mặc định cho nó bay lơ lửng
                var newNote = new AstraNote
                {
                    Content = "Ghi chú nhanh...",
                    IsFloating = true,
                    Left = 100, // Tọa độ xuất hiện trên Desktop
                    Top = 100
                };
                NoteManager.AllNotes.Add(newNote);
                NoteManager.SaveNotes();

                // Bật cái cửa sổ lơ lửng lên luôn
                FloatingNoteWindow fw = new FloatingNoteWindow(newNote);
                fw.Show();
            });

            // Nhớ thêm dòng này vào menu của TrayIcon:
            contextMenu.Items.Add("🗂️ Open Dashboard", null, (s, e) => {
                OpenDashboard();
            });

            contextMenu.Items.Add("⚙️ Cài đặt (Settings)", null, (s, e) => {
                if (isFocusMode) { System.Windows.MessageBox.Show("Đang focus, không được phân tâm nhé!", "Astra"); return; }
                SettingsWindow sw = new SettingsWindow();
                sw.Show();
            });

            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add("Quit", null, (s, e) => {
                // SỬA Ở ĐÂY: Gọi lệnh tắt MainWindow để nó tự nhảy vào OnClosing
                this.Close();
            });

            trayIcon.ContextMenuStrip = contextMenu;
        }

        // --- HỆ THỐNG NHẮC NHỞ TỪ AI ---
        // Lưu trữ danh sách popup để lúc lặn t dọn sạch màn hình
        private System.Collections.Generic.List<ReminderPopup> activePopups = new System.Collections.Generic.List<ReminderPopup>();

        public async void TriggerReminder(string task)
        {
            // Đã tháo phong ấn: Task nào tới giờ cũng nổ bão luôn không cần hỏi nhiều =))
            await StartReminderStormAsync($"⚠️ {task}\nLÀM NGAY VÀ LUÔN!!!");

            // --- LƯU Ý KHI NÀO TRẦM CẢM QUÁ MÀ MUỐN QUAY LẠI KIỂU HIỀN LÀNH THÌ DÙNG ĐOẠN NÀY ---
            // (Bôi đen 3 dòng dưới rồi ấn Ctrl + K, Ctrl + U để mở khóa, và comment cái hàm nổ bão ở trên lại)

            // ChangeState(PetState.Happy);
            // SpeechBubble bubble = new SpeechBubble($"⏰ Tới giờ làm việc rồi:\n{task}", this, 10000);
            // bubble.Show();
        }

        private async Task StartReminderStormAsync(string text)
        {
            int maxCycles = 5; // 5 chu kì
            int spamDurationMs = 3000; // 3s nổ
            int clearDurationMs = 5000; // 5s lặn

            for (int i = 0; i < maxCycles; i++)
            {
                // PHA 1: NỔ BÃO SPAM (3 giây)
                long endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() + spamDurationMs;

                while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < endTime)
                {
                    // Đẻ 8 cái popup cùng một lúc cho nó dày cộp màn hình
                    for (int j = 0; j < 8; j++)
                    {
                        ReminderPopup popup = new ReminderPopup(text, spamDurationMs);
                        popup.Show();
                        activePopups.Add(popup);
                    }
                    // Nghỉ 20ms để máy m không bị đứng hình (Crash UI)
                    await Task.Delay(20);
                }

                // PHA 2: LẶN BIẾN MẤT
                // Đóng và xóa sạch đống popup trên màn hình
                foreach (var popup in activePopups)
                {
                    if (popup != null) popup.Close();
                }
                activePopups.Clear();

                // Chờ 5 giây tĩnh lặng trước khi nổ chu kì tiếp theo (trừ chu kì cuối cùng)
                if (i < maxCycles - 1)
                {
                    await Task.Delay(clearDurationMs);
                }
            }
        }
        // --- THUẬT TOÁN ĐỌC TRỘM LỊCH TRÌNH ---
        private void TaskRadarTimer_Tick(object? sender, EventArgs e)
        {
            string currentMinute = DateTime.Now.ToString("HH:mm");

            // Tránh việc trong 1 phút nó nổ 6 lần (do timer quét 10s/lần)
            if (currentMinute == lastTriggeredMinute) return;

            string tasksFilePath = System.IO.Path.Combine(AppContext.BaseDirectory, "tasks.json");
            if (!System.IO.File.Exists(tasksFilePath)) return;

            try
            {
                // Đọc file JSON
                var allTasks = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(System.IO.File.ReadAllText(tasksFilePath));
                if (allTasks == null) return;

                // Lấy ngày hôm nay
                string todayKey = DateTime.Today.ToString("yyyy-MM-dd");
                if (allTasks.ContainsKey(todayKey))
                {
                    string todayTasks = allTasks[todayKey];

                    // Cắt các dòng ra để quét (vì m gõ tự do bằng dấu Enter)
                    string[] lines = todayTasks.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        // Kiểm tra xem dòng có đúng format [HH:mm] không
                        if (line.Trim().StartsWith("[") && line.Length >= 7 && line[6] == ']')
                        {
                            string taskTime = line.Substring(1, 5); // Cắt lấy chữ HH:mm

                            // NẾU KHỚP GIỜ -> BÁO ĐỘNG!
                            if (taskTime == currentMinute)
                            {
                                string taskContent = line.Substring(7).Trim(); // Cắt lấy nội dung công việc đằng sau
                                TriggerReminder(taskContent); // Gọi cái hàm nổ bão của m
                                lastTriggeredMinute = currentMinute; // Đánh dấu là phút này nổ rồi, đừng nổ nữa
                                break;
                            }
                        }
                    }
                }
            }
            catch { /* Lỗi đọc file thì giả vờ mù bỏ qua =)) */ }
        }
        private void MenuBigChat_Click(object sender, System.Windows.RoutedEventArgs e)
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
        private void MenuDashboard_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenDashboard();
        }
        private void MenuPomodoro_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isFocusMode) return;
            PomodoroSetupWindow setup = new PomodoroSetupWindow(this);
            setup.Show();
            if (dashboard != null) dashboard.Hide();
        }
        private void MenuSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (isFocusMode) return;
            SettingsWindow sw = new SettingsWindow();
            sw.Show();
        }

        public void CloseAllChats()
        {
            if (chatWindow != null)
            {
                chatWindow.Close();
                chatWindow = null;
            }
            if (bigChatWindow != null)
            {
                bigChatWindow.Close();
                bigChatWindow = null;
            }
            isBigChatOpen = false;
        }

        // BẢO KÊ ASTRA: Cấm Alt+F4
        public bool forceClose = false;

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!forceClose)
            {
                e.Cancel = true; // Chặn đứng lệnh tắt ngay lập tức

                // Nếu bảng chửi đang mở rồi thì lôi cổ nó lên trên cùng, cấm đẻ thêm!
                var existingExitWin = System.Windows.Application.Current.Windows.OfType<ExitConfirmWindow>().FirstOrDefault();
                if (existingExitWin != null)
                {
                    existingExitWin.Activate();
                    return;
                }

                // Chưa mở thì đẻ ra
                ExitConfirmWindow exitWin = new ExitConfirmWindow(this);
                exitWin.Show();
            }
            else
            {
                // Nếu ĐƯỢC CẤP PHÉP TẮT THẬT (forceClose = true) thì dọn dẹp khay hệ thống rồi mới nhắm mắt
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
            }
            // Không cần base.OnClosing(e) ở dưới này nữa để tránh lỗi
        }
    }
}