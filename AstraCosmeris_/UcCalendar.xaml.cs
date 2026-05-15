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
        }

        private Border CreateDayCell(string dayText, bool isToday = false)
        {
            Border border = new Border
            {
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333")),
                BorderThickness = new Thickness(0, 2, 2, 0),
                Background = isToday ? new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFCDD2")) : new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FDF8E2")),
                Cursor = string.IsNullOrEmpty(dayText) ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.Hand
            };

            if (string.IsNullOrEmpty(dayText)) return border;

            string dateKey = new DateTime(displayDate.Year, displayDate.Month, int.Parse(dayText)).ToString("yyyy-MM-dd");

            // ĐẾM SỐ TASK & SỰ KIỆN CHÍNH XÁC
            int taskCount = 0;
            if (DataManager.Data.Tasks.ContainsKey(dateKey))
            {
                taskCount = DataManager.Data.Tasks[dateKey].Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            }
            int eventCount = DataManager.Data.Events.Count(e => e.Date.ToString("yyyy-MM-dd") == dateKey);

            StackPanel dotPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(5) };

            if (taskCount > 0)
            {
                Border tBorder = new Border { Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9FB2D4")), CornerRadius = new CornerRadius(10), MinWidth = 22, Height = 22, Margin = new Thickness(2), Padding = new Thickness(4, 0, 4, 0) };
                tBorder.Child = new TextBlock { Text = taskCount > 9 ? "9+" : taskCount.ToString(), Foreground = System.Windows.Media.Brushes.White, FontWeight = System.Windows.FontWeights.Bold, VerticalAlignment = System.Windows.VerticalAlignment.Center, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, FontSize = 12 };
                dotPanel.Children.Add(tBorder);
            }

            if (eventCount > 0)
            {
                Border eBorder = new Border { Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2F3273")), CornerRadius = new CornerRadius(10), MinWidth = 22, Height = 22, Margin = new Thickness(2), Padding = new Thickness(4, 0, 4, 0) };
                eBorder.Child = new TextBlock { Text = eventCount > 9 ? "9+" : eventCount.ToString(), Foreground = System.Windows.Media.Brushes.White, FontWeight = System.Windows.FontWeights.Bold, VerticalAlignment = System.Windows.VerticalAlignment.Center, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, FontSize = 12 };
                dotPanel.Children.Add(eBorder);
            }

            Grid cellGrid = new Grid();
            cellGrid.Children.Add(new TextBlock { Text = dayText, Margin = new Thickness(5), HorizontalAlignment = System.Windows.HorizontalAlignment.Left, VerticalAlignment = System.Windows.VerticalAlignment.Top, Foreground = System.Windows.Media.Brushes.Black, FontSize = 26, FontWeight = System.Windows.FontWeights.Bold });
            cellGrid.Children.Add(dotPanel);
            border.Child = cellGrid;

            // XỬ LÝ MENU (SANG CHẢNH VỚI 3 TÙY CHỌN)
            border.MouseLeftButtonDown += (s, e) => {
                e.Handled = true;
                DateTime clickedDate = new DateTime(displayDate.Year, displayDate.Month, int.Parse(dayText));

                System.Windows.Controls.ContextMenu menu = new System.Windows.Controls.ContextMenu();

                System.Windows.Controls.MenuItem listItem = new System.Windows.Controls.MenuItem { Header = "📋 Xem danh sách trong ngày" };
                listItem.Click += (sender, args) => { System.Windows.MessageBox.Show("Tính năng xem danh sách sẽ ra mắt ở Phase sau!", "Astra V2"); };

                System.Windows.Controls.MenuItem taskItem = new System.Windows.Controls.MenuItem { Header = "📝 Thêm Lịch trình (Task)" };
                taskItem.Click += (sender, args) => { new TaskEntryWindow(clickedDate).ShowDialog(); RenderCalendar(); };

                System.Windows.Controls.MenuItem eventItem = new System.Windows.Controls.MenuItem { Header = "🎉 Thêm Sự kiện (Event)" };
                eventItem.Click += (sender, args) => { new EventEntryWindow(clickedDate).ShowDialog(); RenderCalendar(); };

                menu.Items.Add(listItem);
                menu.Items.Add(new System.Windows.Controls.Separator()); // Thước kẻ ngang
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