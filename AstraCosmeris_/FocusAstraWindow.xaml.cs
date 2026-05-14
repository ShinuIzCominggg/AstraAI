using System.Windows;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public partial class FocusAstraWindow : Window
    {
        private MainWindow parentMain;
        public PomodoroChecklistWindow? checklist; // Thêm ? ở đây
        public bool forceClose = false;

        public FocusAstraWindow(MainWindow main)
        {
            InitializeComponent();
            parentMain = main;
            this.Left = main.Left; this.Top = main.Top;
        }
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                // Cho phép kéo Astra học bài đi quanh màn hình
                this.DragMove();
            }
        }

        // THÊM: Click đúp để mở Checklist cho nhanh (Khỏi phải chuột phải)
        private void Window_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MenuChecklist_Click(null!, null!);
        }

        // Sửa từ private thành public
        public void MenuChecklist_Click(object? sender, RoutedEventArgs? e)
        {
            if (checklist == null || !checklist.IsLoaded)
            {
                checklist = new PomodoroChecklistWindow();
                checklist.Show();
            }
            checklist.Show();
            checklist.Activate();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!forceClose) e.Cancel = true;
            base.OnClosing(e);
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            forceClose = true; // Cấp phép đóng cửa sổ này
            parentMain.isFocusMode = false; // Mở khóa mõm
            parentMain.isBigChatOpen = false; // Reset phòng hờ lỗi kẹt double click
            parentMain.Show(); // Hiện lại Astra gốc

            if (checklist != null) checklist.Close();

            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                if (w is TomatoTimerWindow t)
                {
                    t.forceClose = true; // Cấp phép đóng đồng hồ
                    t.Close();
                }
            }

            this.Close();
        }
    }
}