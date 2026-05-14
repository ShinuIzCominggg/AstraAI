using System;
using System.Linq;
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
            int offset = ((int)firstDayOfMonth.DayOfWeek == 0 ? 7 : (int)firstDayOfMonth.DayOfWeek) - 1;

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
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333")),
                BorderThickness = new Thickness(0, 2, 2, 0),
                Background = isToday ? new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFCDD2")) : new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9FB2D4")),
                Cursor = string.IsNullOrEmpty(dayText) ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.Hand
            };

            if (string.IsNullOrEmpty(dayText)) return border;

            // Kiểm tra xem có Task hay Event trong ngày này không để vẽ chấm màu
            string dateKey = new DateTime(displayDate.Year, displayDate.Month, int.Parse(dayText)).ToString("yyyy-MM-dd");
            bool hasTask = DataManager.Data.Tasks.ContainsKey(dateKey) && !string.IsNullOrWhiteSpace(DataManager.Data.Tasks[dateKey]);
            bool hasEvent = DataManager.Data.Events.Any(e => e.Date.ToString("yyyy-MM-dd") == dateKey);

            StackPanel dotPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(5) };
            if (hasTask) dotPanel.Children.Add(new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF0000")), Margin = new Thickness(2) });
            if (hasEvent) dotPanel.Children.Add(new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFD700")), Margin = new Thickness(2) });

            Grid cellGrid = new Grid();
            cellGrid.Children.Add(new TextBlock { Text = dayText, Margin = new Thickness(5), HorizontalAlignment = System.Windows.HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#000000")), FontSize = 26, FontWeight = FontWeights.Bold });
            cellGrid.Children.Add(dotPanel);
            border.Child = cellGrid;

            // Xử lý Click đẻ ra Menu
            border.MouseLeftButtonDown += (s, e) => {
                e.Handled = true;
                DateTime clickedDate = new DateTime(displayDate.Year, displayDate.Month, int.Parse(dayText));

                System.Windows.Controls.ContextMenu menu = new System.Windows.Controls.ContextMenu();
                System.Windows.Controls.MenuItem taskItem = new System.Windows.Controls.MenuItem { Header = "📝 Thêm Lịch trình (Task)" };
                taskItem.Click += (sender, args) => { new TaskEntryWindow(clickedDate).ShowDialog(); RenderCalendar(); };

                System.Windows.Controls.MenuItem eventItem = new System.Windows.Controls.MenuItem { Header = "🎉 Thêm Sự kiện (Event)" };
                eventItem.Click += (sender, args) => { new EventEntryWindow(clickedDate).ShowDialog(); RenderCalendar(); };

                menu.Items.Add(taskItem);
                menu.Items.Add(eventItem);
                menu.IsOpen = true;
            };

            return border;
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e) { displayDate = displayDate.AddMonths(-1); RenderCalendar(); }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { displayDate = displayDate.AddMonths(1); RenderCalendar(); }
    }
}