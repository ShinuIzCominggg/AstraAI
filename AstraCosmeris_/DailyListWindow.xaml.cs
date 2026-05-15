using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public partial class DailyListWindow : Window
    {
        private DateTime currentDate;
        public ObservableCollection<AstraEvent> DailyEvents { get; set; } = new ObservableCollection<AstraEvent>();

        public DailyListWindow(DateTime date)
        {
            InitializeComponent();
            currentDate = date;
            TxtTitle.Text = $"📅 Ngày {date:dd/MM/yyyy}";
            LoadData();
        }

        private void LoadData()
        {
            string dateKey = currentDate.ToString("yyyy-MM-dd");

            // 1. TẢI CÔNG VIỆC (TASKS)
            if (DataManager.Data.Tasks.TryGetValue(dateKey, out string? taskText) && !string.IsNullOrWhiteSpace(taskText))
            {
                TxtTasks.Text = taskText;
                TxtTasks.FontStyle = FontStyles.Normal;
                TxtTasks.Foreground = System.Windows.Media.Brushes.Black;
            }
            else
            {
                TxtTasks.Text = "Cậu chưa có công việc nào trong ngày này cả~";
                TxtTasks.FontStyle = FontStyles.Italic;
                TxtTasks.Foreground = System.Windows.Media.Brushes.Gray;
            }

            // 2. TẢI SỰ KIỆN (EVENTS)
            DailyEvents.Clear();
            var events = DataManager.Data.Events.Where(e => {
                if (e.Date.Date == currentDate.Date) return true;
                if (e.Date.Date > currentDate.Date) return false;

                return e.Repeat switch
                {
                    "Hàng tuần" => e.Date.DayOfWeek == currentDate.DayOfWeek,
                    "Hàng tháng" => e.Date.Day == currentDate.Day,
                    "Hàng năm" => e.Date.Day == currentDate.Day && e.Date.Month == currentDate.Month,
                    _ => false
                };
            }).ToList();

            foreach (var ev in events) DailyEvents.Add(ev);
            EventList.ItemsSource = DailyEvents;

            // Ẩn hiện chữ "Không có sự kiện nào"
            if (DailyEvents.Count == 0)
            {
                TxtNoEvents.Visibility = Visibility.Visible;
                EventList.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtNoEvents.Visibility = Visibility.Collapsed;
                EventList.Visibility = Visibility.Visible;
            }
        }

        // 3. LOGIC NÚT XÓA SỰ KIỆN
        private void BtnDeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is AstraEvent ev)
            {
                var result = System.Windows.MessageBox.Show($"Cậu có chắc chắn muốn xóa sự kiện '{ev.Title}' không? Tính năng lặp lại cũng sẽ bị hủy bỏ vĩnh viễn đó!",
                                             "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    // Trảm!
                    DataManager.Data.Events.Remove(ev);
                    DataManager.SaveData();
                    LoadData(); // Load lại list ngay lập tức
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }
    }
}