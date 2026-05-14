using System;
using System.Windows;
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public partial class ReminderPopup : Window
    {
        private DispatcherTimer closeTimer;

        public ReminderPopup(string text, int ttlMs = 5000)
        {
            InitializeComponent();
            MessageText.Text = text;

            // Nhảy vị trí random trên màn hình cho người dùng lú luôn =))
            Random rnd = new Random();
            var workArea = SystemParameters.WorkArea;
            this.Left = rnd.Next((int)workArea.Left, (int)(workArea.Right - this.Width));
            this.Top = rnd.Next((int)workArea.Top, (int)(workArea.Bottom - this.Height));

            // Hẹn giờ tự tắt
            closeTimer = new DispatcherTimer();
            closeTimer.Interval = TimeSpan.FromMilliseconds(ttlMs);
            closeTimer.Tick += (s, e) => { closeTimer.Stop(); this.Close(); };
            closeTimer.Start();
        }
    }
}