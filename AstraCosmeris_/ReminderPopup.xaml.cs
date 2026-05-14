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

            Random rnd = new Random();
            var workArea = SystemParameters.WorkArea;

            // Xử lý chống Crash nếu Width/Height chưa kịp nạp
            double safeWidth = this.Width > 0 ? this.Width : 250;
            double safeHeight = this.Height > 0 ? this.Height : 90;

            this.Left = rnd.Next((int)workArea.Left, (int)(workArea.Right - safeWidth));
            this.Top = rnd.Next((int)workArea.Top, (int)(workArea.Bottom - safeHeight));

            closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ttlMs) };
            closeTimer.Tick += (s, e) =>
            {
                closeTimer.Stop();
                this.Close();
            };
            closeTimer.Start();
        }
    }
}