using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public partial class SpeechBubble : Window
    {
        private DispatcherTimer? closeTimer;
        private DispatcherTimer? followTimer; // Timer để bám đuôi
        private MainWindow parentPet;

        private bool isPinned = false;
        private bool isDetached = false;

        public SpeechBubble(string text, MainWindow parent, int durationMs = 3000)
        {
            InitializeComponent();
            MessageText.Text = text;
            parentPet = parent;

            // Chạy timer bám đuôi (cập nhật liên tục 16ms ~ 60fps)
            followTimer = new DispatcherTimer();
            followTimer.Interval = TimeSpan.FromMilliseconds(16);
            followTimer.Tick += FollowTimer_Tick;
            followTimer.Start();

            // Chạy timer tự hủy
            closeTimer = new DispatcherTimer();
            closeTimer.Interval = TimeSpan.FromMilliseconds(durationMs);
            closeTimer.Tick += CloseTimer_Tick;
            closeTimer.Start();

            // Ép update layout ngay lập tức để tính được chiều rộng
            this.UpdateLayout();
            UpdatePosition();
        }

        // --- LOGIC BÁM ĐUÔI VÀ TỰ LẬT ---
        private void FollowTimer_Tick(object? sender, EventArgs e)
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (isDetached) return; // Nếu bị detach thì đứng im

            double petX = parentPet.Left;
            double petY = parentPet.Top;
            double petWidth = parentPet.Width;
            double bubbleWidth = this.ActualWidth;

            // --- CHỈNH KHOẢNG CÁCH Ở ĐÂY NÈ BRO ---
            // offsetX: Tăng số này lên để bong bóng xích lại GẦN pet hơn (theo chiều ngang)
            double offsetX = 40;
            // offsetY: Giảm số này xuống để bong bóng hạ THẤP xuống gần đầu pet hơn
            double offsetY = 10;

            // Vị trí mặc định: BÊN TRÁI Pet
            double targetX = petX - bubbleWidth + offsetX;
            double targetY = petY - offsetY;

            // Check nếu bên trái bị kịch viền màn hình (< 0)
            if (targetX < 0)
            {
                // Vứt bong bóng sang BÊN PHẢI Pet
                targetX = petX + petWidth - offsetX;
            }

            this.Left = targetX;
            this.Top = targetY;
        }

        // --- LOGIC TỰ HỦY ---
        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            if (isPinned) return; // Đang ghim thì không nổ

            CloseBubble();
        }

        public void CloseBubble(bool keepState = false)
        {
            followTimer?.Stop();
            closeTimer?.Stop();
            if (!keepState)
            {
                parentPet.ChangeState(PetState.Idle);
            }
            this.Close();
        }

        // --- SỰ KIỆN CHUỘT (HOVER & DRAG) ---
        private void Grid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MenuPanel.Visibility = Visibility.Visible; // Hiện menu khi đưa chuột vào
        }

        private void Grid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MenuPanel.Visibility = Visibility.Collapsed; // Ẩn menu khi đưa chuột ra
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Chỉ cho kéo thả bong bóng nếu đã ấn Detach
            if (isDetached && e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // --- NÚT BẤM PIN / DETACH ---
        private void BtnPin_Click(object sender, RoutedEventArgs e)
        {
            isPinned = !isPinned;
            BtnPin.Content = isPinned ? "📌 Unpin" : "📌 Pin";
            BtnPin.Foreground = isPinned ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);

            // Bỏ ghim thì cho sống thêm 1.5s rồi tắt
            if (!isPinned)
            {
                closeTimer?.Stop();
                closeTimer = new DispatcherTimer();
                closeTimer.Interval = TimeSpan.FromMilliseconds(1500);
                closeTimer.Tick += CloseTimer_Tick;
                closeTimer.Start();
            }
        }

        private void BtnDetach_Click(object sender, RoutedEventArgs e)
        {
            isDetached = !isDetached;
            BtnDetach.Content = isDetached ? "🔗 Attach" : "🔗 Detach";
            BtnDetach.Foreground = isDetached ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        }

        public void ShowMessage(string text)
        {
            MessageText.Text = text;
            this.Show();
        }
    }
}