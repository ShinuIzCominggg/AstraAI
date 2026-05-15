using System;
using System.Windows;

namespace AstraCosmeris_
{
    public partial class ReminderPopup : Window
    {
        public ReminderPopup(string text)
        {
            InitializeComponent();
            MessageText.Text = text;

            Random rnd = new Random();
            var workArea = SystemParameters.WorkArea;

            double safeWidth = this.Width > 0 ? this.Width : 280;
            double safeHeight = this.Height > 0 ? this.Height : 100;

            this.Left = rnd.Next((int)workArea.Left, (int)(workArea.Right - safeWidth));
            this.Top = rnd.Next((int)workArea.Top, (int)(workArea.Bottom - safeHeight));

            // XÓA TIMER TỰ HỦY: Nó sẽ sống mãi cho đến khi bị MainWindow đóng!
        }
    }
}