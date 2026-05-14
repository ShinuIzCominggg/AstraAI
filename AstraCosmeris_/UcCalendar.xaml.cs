using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AstraCosmeris_
{
    // Đã chỉ định rõ đây là UserControl của WPF
    public partial class UcCalendar : System.Windows.Controls.UserControl
    {
        private DateTime displayDate;

        public UcCalendar()
        {
            InitializeComponent();
            displayDate = DateTime.Now;
            RenderCalendar();
        }

        private void RenderCalendar()
        {
            TxtYear.Text = displayDate.Year.ToString();
            TxtMonth.Text = displayDate.ToString("MMMM");

            DaysGrid.Children.Clear();

            DateTime firstDayOfMonth = new DateTime(displayDate.Year, displayDate.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(displayDate.Year, displayDate.Month);

            int startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            int offset = startDayOfWeek - 1;
            if (offset < 0) offset = 6;

            for (int i = 0; i < offset; i++) DaysGrid.Children.Add(CreateDayCell(""));

            for (int i = 1; i <= daysInMonth; i++)
            {
                bool isToday = (displayDate.Year == DateTime.Now.Year && displayDate.Month == DateTime.Now.Month && i == DateTime.Now.Day);
                DaysGrid.Children.Add(CreateDayCell(i.ToString(), isToday));
            }

            int remainingCells = 42 - (offset + daysInMonth);
            for (int i = 0; i < remainingCells; i++) DaysGrid.Children.Add(CreateDayCell(""));
        }

        private Border CreateDayCell(string dayText, bool isToday = false)
        {
            Border border = new Border
            {
                // Chỉ định rõ Color và ColorConverter của System.Windows.Media
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333")),
                BorderThickness = new Thickness(0.5),
                Background = isToday ? new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FDF8E2"))
                                     : new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9FB2D4")),

                // Chỉ định rõ Cursors của System.Windows.Input
                Cursor = string.IsNullOrEmpty(dayText) ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.Hand
            };

            TextBlock txt = new TextBlock
            {
                Text = dayText,
                Margin = new Thickness(5),
                // Chỉ định rõ HorizontalAlignment của System.Windows
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = new SolidColorBrush(Colors.Black),
                FontSize = 26,
                FontWeight = FontWeights.Bold
            };

            border.Child = txt;

            if (!string.IsNullOrEmpty(dayText))
            {
                border.MouseLeftButtonDown += (s, e) => {
                    e.Handled = true;

                    // Tạo ngày được chọn
                    int day = int.Parse(dayText);
                    DateTime clickedDate = new DateTime(displayDate.Year, displayDate.Month, day);

                    // Mở cái bảng ghi Task lên
                    TaskEntryWindow taskWindow = new TaskEntryWindow(clickedDate);
                    taskWindow.ShowDialog(); // Dùng ShowDialog để bắt m gõ xong mới được xài tiếp lịch
                };
            }

            return border;
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e) { displayDate = displayDate.AddMonths(-1); RenderCalendar(); }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { displayDate = displayDate.AddMonths(1); RenderCalendar(); }
    }
}