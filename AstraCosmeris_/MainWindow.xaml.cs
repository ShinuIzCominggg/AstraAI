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
    public enum PetState { Idle, Thinking, Happy, Dragging }

    public partial class MainWindow : Window
    {
        private BitmapImage? _dragImageCache = null;

        public bool isFocusMode = false;
        public bool isBigChatOpen = false;
        public bool forceClose = false;
        public bool isSmol = false;

        private DispatcherTimer? randomThoughtTimer;
        private DispatcherTimer? taskRadarTimer;
        private DispatcherTimer? animTimer;

        // --- ĐỘNG CƠ VẬT LÝ CHO ASTRA SMOL & LẮC LƯ ---
        private DispatcherTimer? physicsTimer;
        private double velX = 0, velY = 0;
        private double friction = 0.94; // Ma sát trượt 
        private double bounce = -0.7;   // Độ nảy tường
        private double originalWidth, originalHeight;

        // Biến ngầm để tính toán gia tốc mượt mà
        private double _lastWinX = 0;
        private double _lastWinY = 0;
        private double currentRotation = 0;

        private System.Windows.Forms.NotifyIcon? trayIcon;

        // --- QUẢN LÝ CỬA SỔ ĐỘC QUYỀN (FLYOUT) ---
        public Window? currentExclusiveWindow = null;
        public DashboardWindow? dashboard;
        public ChatInputWindow? chatWindow;
        public ChatHistoryWindow? bigChatWindow;

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

        // --- CÔNG CỤ THEO DÕI CHUỘT XUYÊN MÀN HÌNH CỦA WINDOWS ---
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct Win32Point { public int X; public int Y; }

        // --- WIN API CHO GLOBAL HOTKEY ---
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const uint MOD_NONE = 0x0000;
        private const uint MOD_CONTROL = 0x0002; // Phím Ctrl
        private const uint VK_SPACE = 0x20;      // Phím Space
        private IntPtr _windowHandle;
        private System.Windows.Interop.HwndSource? _source;

        // Các biến phục vụ độ trễ kéo thả và ném vật lý
        private bool _isMouseDown = false;
        private bool _isDragging = false;
        private DateTime _mouseDownTime;
        private System.Windows.Point _mouseDownMousePos;
        private System.Windows.Point _mouseDownWindowPos;
        private System.Windows.Point _lastMouseScreenPos;
        private double _throwVelX = 0, _throwVelY = 0;


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
            physicsTimer.Start();

            PreloadDragImage();
        }

        // 👉 THÊM HÀM NÀY VÀO TRONG CLASS ĐỂ THỰC THI PRELOAD:
        private void PreloadDragImage()
        {
            try
            {
                string dragImgPng = Path.Combine(AppContext.BaseDirectory, "assets", "dragging", "dragging.png");
                string dragImgJpg = Path.Combine(AppContext.BaseDirectory, "assets", "dragging", "dragging.jpg");
                string targetImg = File.Exists(dragImgPng) ? dragImgPng : (File.Exists(dragImgJpg) ? dragImgJpg : "");

                if (!string.IsNullOrEmpty(targetImg))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(targetImg, UriKind.Absolute);
                    bmp.DecodePixelWidth = 350; // Kích thước này là nét căng rồi
                    bmp.CacheOption = BitmapCacheOption.OnLoad; // Bắt buộc nạp lên RAM
                    bmp.EndInit();
                    bmp.Freeze(); // Khóa cứng vào VGA cho max mượt
                    _dragImageCache = bmp;
                }
            }
            catch { /* Bỏ qua nếu m lỡ tay xóa mất file */ }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateFrame();

            string dbPath = Path.Combine(AppContext.BaseDirectory, "astra_database.json");
            bool isFirstRun = !File.Exists(dbPath);

            // KỊCH BẢN 1: LẦN ĐẦU MỞ APP -> HIỆN ONBOARDING TRƯỚC
            if (isFirstRun || string.IsNullOrEmpty(DataManager.Data.ApiKey))
            {
                this.Visibility = Visibility.Hidden; // Giấu Astra đi

                var onboard = new OnboardingWindow();
                onboard.ShowDialog(); // Màn hình sẽ dừng chờ user điền Form xong

                // Điền xong form, load lại data và hiện Astra lên
                DataManager.LoadData();
                this.Visibility = Visibility.Visible;

                // Khởi động Tutorial
                if (TutorialManager.BoardWindow == null)
                {
                    TutorialManager.BoardWindow = new TutorialBoardWindow(this);
                    TutorialManager.BoardWindow.Show();
                }
            }
            // KỊCH BẢN 2: USER CŨ NHƯNG CHƯA HOÀN THÀNH TUTORIAL
            else
            {
                if (!DataManager.Data.Facts.ContainsKey("__TutorialDone") || DataManager.Data.Facts["__TutorialDone"] != "true")
                {
                    if (TutorialManager.BoardWindow == null)
                    {
                        TutorialManager.BoardWindow = new TutorialBoardWindow(this);
                        TutorialManager.BoardWindow.Show();
                    }
                }
            }

            // Thiết lập tâm xoay ban đầu ở đỉnh đầu cho Astra lớn khi vừa mở app
            var container = PetImage.Parent as FrameworkElement;
            if (container != null) container.RenderTransformOrigin = new System.Windows.Point(0.5, 0.1);
        }

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
            animTimer.Tick += AnimTimer_Tick;
            animTimer.Start();
        }

        private void AnimTimer_Tick(object? sender, EventArgs e)
        {
            // 👉 KÍCH HOẠT BẢO KÊ: Quét liên tục, hở ra mép là kéo vào
            KeepInScreenBounds();

            // BỌC THÉP: Bị kéo thì cấm được cựa quậy
            if (currentState == PetState.Dragging) return;

            if (currentFrames.Length > 0 && !isSmol)
            {
                currentFrameIndex = (currentFrameIndex + 1) % currentFrames.Length;
                UpdateFrame();
            }
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (!DataManager.Data.Stats.ContainsKey(today)) DataManager.Data.Stats[today] = new DailyStat();
            DataManager.Data.Stats[today].ScreenTimeMinutes += (166.0 / 60000.0);
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

            // 👉 BẢO KÊ TUYỆT ĐỐI: Nếu đang trong quá trình kéo thả, cấm mọi nguồn khác đổi trạng thái bậy bạ
            if (_isDragging && newState != PetState.Dragging) return;

            currentState = newState;

            if (newState == PetState.Dragging) return;

            string folderName = newState.ToString().ToLower();
            string folderPath = Path.Combine(AppContext.BaseDirectory, "assets", folderName);
            if (Directory.Exists(folderPath)) currentFrames = Directory.GetFiles(folderPath, "*.*").Where(s => s.EndsWith(".png") || s.EndsWith(".jpg")).ToArray();
            else currentFrames = Array.Empty<string>();
            currentFrameIndex = 0;
        }

        private void UpdateFrame()
        {
            if (currentFrames.Length > 0 && !isSmol)
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(currentFrames[currentFrameIndex]);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze(); // Tối ưu siêu tốc cho hoạt ảnh!

                PetImage.Source = bmp;
            }
        }

        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            // NẾU ĐANG KÉO MÀ BẤM CHUỘT PHẢI -> HỦY KÉO AN TOÀN NGAY LẬP TỨC
            if (_isMouseDown)
            {
                _isMouseDown = false;
                this.ReleaseMouseCapture();

                if (_isDragging)
                {
                    _isDragging = false;
                    if (isSmol)
                    {
                        ChangeState(PetState.Idle);
                    }
                    else
                    {
                        HandleDragDropNormal(); // Gắn lại hiệu ứng nảy lò xo
                    }
                }
            }
            base.OnPreviewMouseRightButtonDown(e);
        }

        private void KeepInScreenBounds()
        {
            // Không can thiệp nếu user đang chủ động kéo, hoặc đang ở form Smol (vì Smol đã có vật lý đập tường riêng)
            if (_isDragging || isSmol) return;

            var wa = SystemParameters.WorkArea;
            bool isOutOfBound = false;
            double newLeft = this.Left;
            double newTop = this.Top;

            // Kẹt trái / phải
            if (newLeft < wa.Left) { newLeft = wa.Left; isOutOfBound = true; }
            else if (newLeft + this.Width > wa.Right) { newLeft = wa.Right - this.Width; isOutOfBound = true; }

            // Kẹt trên / dưới
            if (newTop < wa.Top) { newTop = wa.Top; isOutOfBound = true; }
            else if (newTop + this.Height > wa.Bottom) { newTop = wa.Bottom - this.Height; isOutOfBound = true; }

            if (isOutOfBound)
            {
                this.Left = newLeft;
                this.Top = newTop;
            }
        }
        // ==========================================
        // VẬT LÝ KÉO THẢ MƯỢT 100% CỦA WINDOWS
        // ==========================================
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
            {
                _isMouseDown = true;
                _isDragging = false;
                _mouseDownTime = DateTime.Now;

                Win32Point pt = new Win32Point();
                GetCursorPos(ref pt);
                _mouseDownMousePos = new System.Windows.Point(pt.X, pt.Y);
                _mouseDownWindowPos = new System.Windows.Point(this.Left, this.Top);

                _throwVelX = 0; _throwVelY = 0; // Reset lực ném
                _lastMouseScreenPos = _mouseDownMousePos;

                // 👉 ĐĂNG KÝ CHIẾM GIỮ CHUỘT XUYÊN SUỐT: Dùng cho cả lớn lẫn nhỏ
                this.CaptureMouse();
            }
        }

        // 2 Hàm này để làm vì trong file XAML của m đang có reference, nếu xóa đi là lỗi App
        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isMouseDown) return;

            Win32Point pt = new Win32Point();
            GetCursorPos(ref pt);
            System.Windows.Point currentMousePos = new System.Windows.Point(pt.X, pt.Y);

            double distance = Math.Sqrt(Math.Pow(currentMousePos.X - _mouseDownMousePos.X, 2) + Math.Pow(currentMousePos.Y - _mouseDownMousePos.Y, 2));
            double timeHeld = (DateTime.Now - _mouseDownTime).TotalMilliseconds;

            // ĐIỀU KIỆN KÍCH HOẠT DRAG
            if (!_isDragging && (timeHeld > 150 || distance > 10))
            {
                _isDragging = true;
                TutorialManager.CompleteQuest("drag", this);
                ChangeState(PetState.Dragging);

                if (_dragImageCache != null && !isSmol)
                    PetImage.Source = _dragImageCache;

                DragRotation.BeginAnimation(RotateTransform.AngleProperty, null);
            }

            // 👉 THỰC THI DI CHUYỂN
            if (_isDragging)
            {
                if (!isSmol)
                {
                    // Astra lớn: Ép cửa sổ dịch chuyển để con trỏ chuột luôn chỉ đúng vào góc phải phía trên
                    // Trừ đi Width để dịch lùi cửa sổ sang trái, cộng thêm chút offset (35, 15) để chuột nằm đúng nếp áo
                    this.Left = currentMousePos.X - this.Width + 35;
                    this.Top = currentMousePos.Y - 15;
                }
                else
                {
                    // Smol Astra: Giữ nguyên cơ chế lăn bóng theo tâm trỏ chuột ban đầu
                    this.Left = _mouseDownWindowPos.X + (currentMousePos.X - _mouseDownMousePos.X);
                    this.Top = _mouseDownWindowPos.Y + (currentMousePos.Y - _mouseDownMousePos.Y);
                }
            }
        }
        private void HandleDragDropNormal()
        {
            _isDragging = false; // <-- THÊM DÒNG NÀY ĐỂ FIX LỖI RÒ RỈ TRẠNG THÁI

            ChangeState(PetState.Idle);
            UpdateFrame();

            DoubleAnimation springAnim = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(1000),
                EasingFunction = new ElasticEase { Oscillations = 4, Springiness = 4, EasingMode = EasingMode.EaseOut }
            };
            DragRotation.BeginAnimation(RotateTransform.AngleProperty, springAnim);
        }
        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isMouseDown)
            {
                _isMouseDown = false;
                this.ReleaseMouseCapture(); // Giải phóng chuột trả về cho hệ thống

                if (_isDragging)
                {
                    _isDragging = false;
                    if (isSmol)
                    {
                        // TRUYỀN VẬN TỐC NÉM CHO ENGINE VẬT LÝ VÀ ĐẬP!
                        string mode = DataManager.Data.Facts.ContainsKey("__PhysicsMode") ? DataManager.Data.Facts["__PhysicsMode"] : "0";

                        if (mode == "1") { friction = 0.99; bounce = -0.95; velX = _throwVelX * 1.5; velY = _throwVelY * 1.5; }
                        else if (mode == "2") { friction = 0.85; bounce = -0.4; velX = _throwVelX * 0.8; velY = _throwVelY * 0.8; }
                        else { friction = 0.94; bounce = -0.7; velX = _throwVelX * 1.2; velY = _throwVelY * 1.2; }

                        ChangeState(PetState.Idle);
                    }
                    else
                    {
                        // Astra lớn: Thả chuột ra là kích hoạt hiệu ứng nảy lò xo đàn hồi ngay lập tức
                        HandleDragDropNormal();
                    }
                }
            }
        }

        // ==========================================
        // TINH TOÁN ĐỘ NGHIÊNG, LĂN BÓNG BẰNG TIMER BÚ BACKGROUND
        // ==========================================
        private void PhysicsTimer_Tick(object? sender, EventArgs e)
        {
            if (_isDragging)
            {
                double instVelX = this.Left - _lastWinX;
                double instVelY = this.Top - _lastWinY;

                if (isSmol)
                {
                    // TÍNH LỰC NÉM SMOL: Dùng LERP nhẹ (cộng dồn 50% cũ) để chống việc 
                    // chuột bị khựng 1 ms trước khi thả làm vận tốc về 0.
                    _throwVelX = (_throwVelX * 0.5) + (instVelX * 0.5);
                    _throwVelY = (_throwVelY * 0.5) + (instVelY * 0.5);

                    currentRotation += _throwVelX * 0.5;
                    DragRotation.Angle = currentRotation;
                }
                else
                {
                    // FIX LỖI GIẬT FPS ASTRA THƯỜNG: Dùng nội suy tuyến tính (LERP)
                    // Thay vì gán giật cục, ta cho góc hiện tại từ từ chạy về targetAngle
                    double targetAngle = Math.Clamp(instVelX * 2.5, -45, 45);
                    currentRotation += (targetAngle - currentRotation) * 0.2;
                    DragRotation.Angle = currentRotation;
                }
            }
            else if (isSmol)
            {
                // Logic bóng lăn tường cho Smol
                velX *= friction;
                velY *= friction;
                this.Left += velX;
                this.Top += velY;

                if (Math.Abs(velX) > 0.1 || Math.Abs(velY) > 0.1)
                {
                    currentRotation += velX * 3;
                    DragRotation.Angle = currentRotation;
                }

                var wa = SystemParameters.WorkArea;
                bool hit = false;

                if (this.Top < wa.Top) { this.Top = wa.Top; velY *= bounce; hit = true; }
                else if (this.Top + this.Height > wa.Bottom) { this.Top = wa.Bottom - this.Height; velY *= bounce; hit = true; }
                if (this.Left < wa.Left) { this.Left = wa.Left; velX *= bounce; hit = true; }
                else if (this.Left + this.Width > wa.Right) { this.Left = wa.Right - this.Width; velX *= bounce; hit = true; }

                if (hit && (Math.Abs(velX) + Math.Abs(velY) > 5) && rand.Next(10) < 3)
                {
                    string[] cries = { "Á!", "Ui da!", "Đau tớ!", "Cứu!" };
                    new SpeechBubble(cries[rand.Next(cries.Length)], this, 1000).Show();
                }
            }

            _lastWinX = this.Left;
            _lastWinY = this.Top;
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (isSmol) { ToggleSmolMode(); return; }
            if (isFocusMode) return;
            if (e.ChangedButton == MouseButton.Left)
            {
                TutorialManager.CompleteQuest("smartra", this);
                ChangeState(PetState.Happy);

                if (isBigChatOpen)
                {
                    if (bigChatWindow != null && bigChatWindow.IsLoaded) { bigChatWindow.Activate(); return; }
                    else { isBigChatOpen = false; bigChatWindow = null; }
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
                Window oldWin = currentExclusiveWindow;

                DoubleAnimation slideDown = new DoubleAnimation
                {
                    To = wa.Bottom + 50,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                slideDown.Completed += (s, e) => {
                    oldWin.Hide();
                    if (oldWin is SettingsWindow || oldWin is PomodoroSetupWindow || oldWin is ChatHistoryWindow)
                        oldWin.Close();
                    ShowNewWindow(newWin, wa);
                };
                oldWin.BeginAnimation(Window.TopProperty, slideDown);
            }
            else ShowNewWindow(newWin, wa);
        }

        private void ShowNewWindow(Window newWin, Rect wa)
        {
            currentExclusiveWindow = newWin;
            newWin.Left = wa.Left + (wa.Width - newWin.Width) / 2;
            newWin.Top = -newWin.Height - 50;
            newWin.Show();

            double targetTop = wa.Top + (wa.Height - newWin.Height) / 2;
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
                Window oldWin = currentExclusiveWindow;

                DoubleAnimation slideDown = new DoubleAnimation
                {
                    To = wa.Bottom + 50,
                    Duration = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                slideDown.Completed += (s, e) => {
                    oldWin.Hide();
                    if (oldWin is SettingsWindow || oldWin is PomodoroSetupWindow || oldWin is ChatHistoryWindow)
                        oldWin.Close();
                    if (currentExclusiveWindow == oldWin) currentExclusiveWindow = null;
                };
                oldWin.BeginAnimation(Window.TopProperty, slideDown);
            }
        }

        public void ToggleSmolMode()
        {
            // Tự động tìm khung bọc (Grid/Border) bên ngoài đang chứa DragRotation
            var container = PetImage.Parent as FrameworkElement;

            if (!isSmol)
            {
                isSmol = true;
                originalWidth = this.Width; originalHeight = this.Height;

                this.Width = 120;
                this.Height = 120;

                // 👉 ĐỔI TÂM XOAY VỀ CHÍNH GIỮA (Lăn bóng tròn cho Smol)
                if (container != null)
                    container.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

                PetImage.Width = 90;
                PetImage.Height = 90;

                string path = Path.Combine(AppContext.BaseDirectory, "assets", "rolling", "rolling.png");
                if (File.Exists(path)) PetImage.Source = new BitmapImage(new Uri(path));

                string mode = DataManager.Data.Facts.ContainsKey("__PhysicsMode") ? DataManager.Data.Facts["__PhysicsMode"] : "0";
                if (mode == "1") { friction = 0.99; bounce = -0.95; }
                else if (mode == "2") { friction = 0.85; bounce = -0.4; }
                else { friction = 0.94; bounce = -0.7; }

                velY = 0; velX = (rand.NextDouble() - 0.5) * 15;
            }
            else
            {
                isSmol = false;
                this.Width = originalWidth; this.Height = originalHeight;

                // 👉 ĐỔI TÂM XOAY LÊN GẦN ĐỈNH ĐẦU (Tạo hiệu ứng móc khóa đu đưa cho Astra lớn)
                if (container != null)
                    container.RenderTransformOrigin = new System.Windows.Point(0.5, 0.1);

                PetImage.Width = double.NaN;
                PetImage.Height = double.NaN;

                ChangeState(PetState.Idle);
                DragRotation.Angle = 0;
            }
        }

        public void OpenDashboard() { if (isFocusMode) return; TutorialManager.CompleteQuest("dashboard", this); DataManager.TrackDashboardOpen(); if (dashboard == null || !dashboard.IsLoaded) { dashboard = new DashboardWindow(); } OpenExclusiveWindow(dashboard); }
        public void CloseAllChats() { chatWindow?.Close(); chatWindow = null; bigChatWindow?.Close(); bigChatWindow = null; isBigChatOpen = false; }
        private void MenuSmol_Click(object sender, RoutedEventArgs e) => ToggleSmolMode();
        private void MenuBigChat_Click(object sender, RoutedEventArgs e)
        {
            if (!isFocusMode && !isBigChatOpen)
            {
                isBigChatOpen = true;
                bigChatWindow = new ChatHistoryWindow(this);
                bigChatWindow.Closed += (s, args) => { isBigChatOpen = false; bigChatWindow = null; ChangeState(PetState.Idle); };
                OpenExclusiveWindow(bigChatWindow);
            }
        }
        private void MenuDashboard_Click(object sender, RoutedEventArgs e) => OpenDashboard();
        private void MenuPomodoro_Click(object sender, RoutedEventArgs e) { if (!isFocusMode) { TutorialManager.CompleteQuest("pomodoro", this); OpenExclusiveWindow(new PomodoroSetupWindow(this)); } }
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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            _source = System.Windows.Interop.HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            // Đăng ký Ctrl + Space
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL, VK_SPACE);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                // Khi bấm Ctrl+Space -> Gọi lại logic y hệt như Double Click vào Astra
                if (!isFocusMode)
                {
                    if (chatWindow == null || !chatWindow.IsLoaded)
                    {
                        chatWindow = new ChatInputWindow(this);
                        chatWindow.Closed += (s, args) => chatWindow = null;
                        chatWindow.Show();
                    }
                    else
                    {
                        chatWindow.Activate(); // Nếu đang mở mà bị khuất thì lôi lên đầu
                        if (chatWindow.WindowState == WindowState.Minimized) chatWindow.WindowState = WindowState.Normal;
                    }
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }
            UnregisterHotKey(_windowHandle, HOTKEY_ID); // Hủy đăng ký phím tắt
            base.OnClosed(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            randomThoughtTimer?.Stop();
            taskRadarTimer?.Stop();
            animTimer?.Stop();
            physicsTimer?.Stop();

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