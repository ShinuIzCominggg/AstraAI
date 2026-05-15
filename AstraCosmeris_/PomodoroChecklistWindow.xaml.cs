using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AstraCosmeris_
{
    public class FocusTask : INotifyPropertyChanged
    {
        private bool _isChecked;
        private bool _isCrossed;
        private string _name = "";

        public int Id { get; set; }
        public string OrderText { get; set; } = "";
        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
        public int Level { get; set; }
        public Thickness MarginLevel => new Thickness(Math.Min(Level, 3) * 20, 0, 0, 0);
        public bool IsNew { get; set; } = false;

        // CỜ CHỐNG FARM TICK 
        public bool IsCountedForReport { get; set; } = false;

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

        // AUTO-FOCUS KHI ĐẺ DÒNG MỚI
        private void TaskTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox txt && txt.DataContext is FocusTask task && task.IsNew)
            {
                txt.IsReadOnly = false; txt.Cursor = System.Windows.Input.Cursors.IBeam;
                txt.Focus(); task.IsNew = false;
            }
        }

        // BÍ THUẬT: DOUBLE CLICK ĐỂ SỬA CHỮ
        private void TaskTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox txt)
            {
                txt.IsReadOnly = false; txt.Cursor = System.Windows.Input.Cursors.IBeam;
                txt.Focus();
                txt.BorderThickness = new System.Windows.Thickness(0, 0, 0, 1);
                txt.BorderBrush = System.Windows.Media.Brushes.Gray;
            }
        }

        // CLICK RA NGOÀI LÀ KHÓA LẠI
        private void TaskTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox txt)
            {
                txt.IsReadOnly = true; txt.Cursor = System.Windows.Input.Cursors.Arrow;
                txt.BorderThickness = new System.Windows.Thickness(0);
            }
        }

        // TICK HOÀN THÀNH VÀ CHỐNG FARM
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox chk && chk.DataContext is FocusTask task)
            {
                if (task.IsChecked && !task.IsCountedForReport)
                {
                    task.IsCountedForReport = true; // Đánh dấu đã farm
                    string today = DateTime.Now.ToString("yyyy-MM-dd");
                    if (!DataManager.Data.Stats.ContainsKey(today)) DataManager.Data.Stats[today] = new DailyStat();

                    DataManager.Data.Stats[today].TasksCompleted++;
                    DataManager.SaveData();
                }
            }
        }

        // LOGIC MENU THÔNG MINH (CHỨA NÚT XÓA)
        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is System.Windows.Controls.Grid grid && grid.DataContext is FocusTask task && grid.ContextMenu != null)
            {
                grid.ContextMenu.Items.Clear();

                if (task.IsCrossed)
                {
                    System.Windows.Controls.MenuItem delItem = new System.Windows.Controls.MenuItem { Header = "🗑️ Xóa Task vĩnh viễn", Foreground = System.Windows.Media.Brushes.Red };
                    delItem.Click += (s, args) => Tasks.Remove(task);
                    grid.ContextMenu.Items.Add(delItem);
                }
                else
                {
                    System.Windows.Controls.MenuItem crossItem = new System.Windows.Controls.MenuItem { Header = "❌ Gạch bỏ" };
                    crossItem.Click += (s, args) => task.IsCrossed = true;

                    System.Windows.Controls.MenuItem subItem = new System.Windows.Controls.MenuItem { Header = "➕ Tạo Task con" };
                    subItem.Click += (s, args) => MenuAddSubTask_Execute(task);

                    grid.ContextMenu.Items.Add(crossItem);
                    grid.ContextMenu.Items.Add(subItem);
                }
            }
        }

        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            Tasks.Add(new FocusTask { Id = Tasks.Count, OrderText = $"{mainTaskCounter}.", Name = "", Level = 0, IsNew = true });
            mainTaskCounter++;
        }

        private void MenuAddSubTask_Execute(FocusTask parentTask)
        {
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
                Name = "",
                Level = parentTask.Level + 1,
                IsNew = true
            });
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Hide();
    }
}