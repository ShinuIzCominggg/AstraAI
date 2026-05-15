using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public partial class AstraNotificationWindow : Window
    {
        private Action? _action1;
        private Action? _action2;
        private DispatcherTimer _closeTimer;

        // Constructor đa năng
        public AstraNotificationWindow(string title, string message, string btn1Text = "", Action? action1 = null, string btn2Text = "", Action? action2 = null)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage.Text = message;

            _action1 = action1;
            _action2 = action2;

            if (!string.IsNullOrEmpty(btn1Text) || !string.IsNullOrEmpty(btn2Text))
            {
                ActionPanel.Visibility = Visibility.Visible;
                if (!string.IsNullOrEmpty(btn1Text)) BtnAction1.Content = btn1Text; else BtnAction1.Visibility = Visibility.Collapsed;
                if (!string.IsNullOrEmpty(btn2Text)) BtnAction2.Content = btn2Text; else BtnAction2.Visibility = Visibility.Collapsed;
            }

            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right; // Đặt sẵn ở ngoài rìa màn hình
            this.Top = workArea.Bottom - this.Height - 20;

            if (DataManager.Data.NotiConfig.EnableSound)
                System.Media.SystemSounds.Exclamation.Play(); // Tiếng ting ting nhẹ nhàng

            _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(DataManager.Data.NotiConfig.DurationSeconds) };
            _closeTimer.Tick += (s, e) => CloseOut();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Animation trượt vào
            DoubleAnimation slideIn = new DoubleAnimation
            {
                From = SystemParameters.WorkArea.Right,
                To = SystemParameters.WorkArea.Right - this.Width - 20,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            this.BeginAnimation(Window.LeftProperty, slideIn);

            // Nếu không có nút bấm thì tự hẹn giờ tắt
            if (_action1 == null && _action2 == null) _closeTimer.Start();
        }

        private void CloseOut()
        {
            _closeTimer.Stop();
            // Animation trượt ra
            DoubleAnimation slideOut = new DoubleAnimation
            {
                To = SystemParameters.WorkArea.Right,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            slideOut.Completed += (s, e) => this.Close();
            this.BeginAnimation(Window.LeftProperty, slideOut);
        }

        private void BtnAction1_Click(object sender, RoutedEventArgs e) { _action1?.Invoke(); CloseOut(); }
        private void BtnAction2_Click(object sender, RoutedEventArgs e) { _action2?.Invoke(); CloseOut(); }
    }
}