using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public partial class SpeechBubble : Window
    {
        private DispatcherTimer? closeTimer;
        private DispatcherTimer? followTimer;
        private MainWindow parentPet;

        private bool isPinned = false;
        private bool isDetached = false;

        public SpeechBubble(string text, MainWindow parent, int durationMs = 3000)
        {
            InitializeComponent();
            MessageText.Text = text;
            parentPet = parent;

            followTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            followTimer.Tick += FollowTimer_Tick;
            followTimer.Start();

            closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
            closeTimer.Tick += CloseTimer_Tick;
            closeTimer.Start();

            this.UpdateLayout();
            UpdatePosition();
        }

        private void FollowTimer_Tick(object? sender, EventArgs e) => UpdatePosition();

        private void UpdatePosition()
        {
            if (isDetached) return;

            double petX = parentPet.Left;
            double petY = parentPet.Top;
            double petWidth = parentPet.Width;
            double bubbleWidth = this.ActualWidth;

            double offsetX = 40;
            double offsetY = 10;

            double targetX = petX - bubbleWidth + offsetX;
            double targetY = petY - offsetY;

            if (targetX < 0) targetX = petX + petWidth - offsetX;

            this.Left = targetX;
            this.Top = targetY;
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            if (!isPinned) CloseBubble();
        }

        public void CloseBubble(bool keepState = false)
        {
            followTimer?.Stop();
            closeTimer?.Stop();
            if (!keepState) parentPet.ChangeState(PetState.Idle);
            this.Close();
        }

        private void Grid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) => MenuPanel.Visibility = Visibility.Visible;
        private void Grid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => MenuPanel.Visibility = Visibility.Collapsed;

        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (isDetached && e.ChangedButton == System.Windows.Input.MouseButton.Left) this.DragMove();
        }

        private void BtnPin_Click(object sender, RoutedEventArgs e)
        {
            isPinned = !isPinned;
            BtnPin.Content = isPinned ? "📌 Unpin" : "📌 Pin";
            BtnPin.Foreground = isPinned ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);

            if (!isPinned)
            {
                closeTimer?.Stop();
                closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
                closeTimer.Tick += CloseTimer_Tick;
                closeTimer.Start();
            }
        }

        private void BtnDetach_Click(object sender, RoutedEventArgs e)
        {
            isDetached = !isDetached;
            BtnDetach.Content = isDetached ? "🔗 Attach" : "🔗 Detach";
            BtnDetach.Foreground = isDetached ? new SolidColorBrush(Colors.Blue) : new SolidColorBrush(Colors.Black);
        }

        public void ShowMessage(string text)
        {
            MessageText.Text = text;
            this.Show();
        }
    }
}