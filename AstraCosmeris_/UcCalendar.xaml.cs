using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AstraCosmeris_
{
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
            int offset = (startDayOfWeek == 0 ? 7 : startDayOfWeek) - 1;

            for (int i = 0; i < offset; i++) DaysGrid.Children.Add(CreateDayCell(""));

            for (int i = 1; i <= daysInMonth; i++)
            {
                bool isToday = (displayDate.Year == DateTime.Now.Year && displayDate.Month == DateTime.Now.Month && i == DateTime.Now.Day);
                DaysGrid.Children.Add(CreateDayCell(i.ToString(), isToday));
            }
        }

        private Border CreateDayCell(string dayText, bool isToday = false)
        {
            Border border = new Border
            {
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(0, 2, 2, 0),
                Background = isToday ? new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFCDD2")) : System.Windows.Media.Brushes.Transparent,
                Cursor = string.IsNullOrEmpty(dayText) ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.Hand
            };

            border.Child = new TextBlock
            {
                Text = dayText,
                Margin = new Thickness(5),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Foreground = System.Windows.Media.Brushes.Black,
                FontSize = 26,
                FontWeight = System.Windows.FontWeights.Bold
            };

            if (!string.IsNullOrEmpty(dayText))
            {
                border.MouseLeftButtonDown += (s, e) => {
                    e.Handled = true;
                    new TaskEntryWindow(new DateTime(displayDate.Year, displayDate.Month, int.Parse(dayText))).ShowDialog();
                };
            }

            return border;
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e) { displayDate = displayDate.AddMonths(-1); RenderCalendar(); }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { displayDate = displayDate.AddMonths(1); RenderCalendar(); }
    }
}