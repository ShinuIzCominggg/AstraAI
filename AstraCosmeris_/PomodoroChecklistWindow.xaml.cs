using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace AstraCosmeris_
{
    public class FocusTask : INotifyPropertyChanged
    {
        private bool _isChecked;
        private bool _isCrossed;
        private string _name = "";

        public int Id { get; set; }
        public string OrderText { get; set; } = "";

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public int Level { get; set; }
        public Thickness MarginLevel => new Thickness(System.Math.Min(Level, 3) * 20, 0, 0, 0);

        // CỜ HIỆU ĐỂ BÁO CHO CODE BIẾT ĐÂY LÀ DÒNG MỚI ĐẺ RA, CẦN FOCUS CHUỘT VÀO!
        public bool IsNew { get; set; } = false;

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; if (value) IsCrossed = false; OnPropertyChanged(nameof(IsChecked)); OnPropertyChanged(nameof(IsStrikethrough)); OnPropertyChanged(nameof(TextColor)); }
        }
        public bool IsCrossed
        {
            get => _isCrossed;
            set { _isCrossed = value; if (value) IsChecked = false; OnPropertyChanged(nameof(IsCrossed)); OnPropertyChanged(nameof(IsStrikethrough)); OnPropertyChanged(nameof(TextColor)); }
        }

        public bool IsStrikethrough => IsChecked || IsCrossed;
        public System.Windows.Media.Brush TextColor => IsCrossed ? System.Windows.Media.Brushes.Red : (IsChecked ? System.Windows.Media.Brushes.Blue : System.Windows.Media.Brushes.Black);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class PomodoroChecklistWindow : Window
    {
        public ObservableCollection<FocusTask> Tasks { get; set; } = new ObservableCollection<FocusTask>();
        private int mainTaskCounter = 1;

        public PomodoroChecklistWindow()
        {
            InitializeComponent();
            LvTasks.ItemsSource = Tasks;
        }

        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            // Đẻ thẳng 1 dòng trống tinh tươm, cắm cờ IsNew = true
            Tasks.Add(new FocusTask { Id = Tasks.Count, OrderText = $"{mainTaskCounter}.", Name = "", Level = 0, IsNew = true });
            mainTaskCounter++;
        }

        // BÍ THUẬT AUTO-FOCUS CHUỘT
        private void TaskTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox txt && txt.DataContext is FocusTask task)
            {
                if (task.IsNew)
                {
                    txt.Focus(); // Bắt con trỏ chuột nhảy vào đây
                    task.IsNew = false; // Xóa cờ để lần sau cuộn danh sách không bị focus lại
                }
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e) { /* Tự động update nhờ TwoWay Binding */ }

        private void MenuCrossOut_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.MenuItem)?.CommandParameter is FocusTask task) task.IsCrossed = true;
        }

        private void MenuAddSubTask_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.MenuItem)?.CommandParameter is FocusTask parentTask)
            {
                // Dẹp luôn cái InputBox phèn đi! Giờ đẻ thẳng 1 dòng task con trống để tự gõ.
                int parentIndex = Tasks.IndexOf(parentTask);
                int subCount = 1;

                for (int i = parentIndex + 1; i < Tasks.Count; i++)
                {
                    if (Tasks[i].Level > parentTask.Level) subCount++;
                    else break;
                }

                Tasks.Insert(parentIndex + subCount, new FocusTask
                {
                    Id = Tasks.Count,
                    OrderText = $"{parentTask.OrderText.TrimEnd('.')}.{subCount}",
                    Name = "", // Trống bốc để gõ
                    Level = parentTask.Level + 1,
                    IsNew = true // Cắm cờ để auto-focus
                });
            }
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (e.ChangedButton == System.Windows.Input.MouseButton.Left) this.DragMove(); }
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Hide();
    }
}