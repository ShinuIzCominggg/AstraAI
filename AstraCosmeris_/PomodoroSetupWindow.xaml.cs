using System.Windows;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public partial class PomodoroSetupWindow : Window
    {
        private int workTime = 25;
        private int breakTime = 5;
        private MainWindow parentMain;

        public PomodoroSetupWindow(MainWindow main)
        {
            InitializeComponent();
            parentMain = main;
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            TxtWork.Text = workTime.ToString();
            TxtBreak.Text = breakTime.ToString();
            BtnWorkUp.Foreground = workTime >= 35 ? System.Windows.Media.Brushes.Gray : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2F3273"));
            BtnWorkDown.Foreground = workTime <= 15 ? System.Windows.Media.Brushes.Gray : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2F3273"));
            BtnBreakUp.Foreground = breakTime >= 15 ? System.Windows.Media.Brushes.Gray : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2F3273"));
            BtnBreakDown.Foreground = breakTime <= 5 ? System.Windows.Media.Brushes.Gray : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2F3273"));
        }

        // BIẾN BẢO KÊ ĐẾM EASTER EGG NẰM Ở ĐÂY
        private int easterEggCounter = 0;

        private void BtnWorkDown_Click(object sender, RoutedEventArgs e)
        {
            if (workTime > 15)
            {
                workTime--;
                easterEggCounter = 0; // Đang giảm bình thường thì reset
                UpdateButtonStates();
            }
            else
            {
                easterEggCounter++;
                if (easterEggCounter == 5)
                {
                    workTime = 1;
                    TxtWork.Text = "1"; // Ép hiển thị 1 phút
                }
                else if (easterEggCounter == 10)
                {
                    workTime = -1; // CỜ HIỆU ĐẶC BIỆT CHO 1 GIÂY
                    TxtWork.Text = "1s"; // Ép hiển thị 1s
                }
            }
        }

        private void BtnWorkUp_Click(object sender, RoutedEventArgs e)
        {
            if (workTime < 35)
            {
                // Nếu đang dính Easter egg mà bấm Tăng thì lôi cổ về mức 15p
                if (workTime == -1 || workTime == 1) workTime = 15;
                else workTime++;

                easterEggCounter = 0;
                UpdateButtonStates();
            }
        }
        private void BtnBreakUp_Click(object sender, RoutedEventArgs e) { if (breakTime < 15) { breakTime++; UpdateButtonStates(); } }
        private void BtnBreakDown_Click(object sender, RoutedEventArgs e) { if (breakTime > 5) { breakTime--; UpdateButtonStates(); } }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnStart_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show($"Cậu có chắc chắn muốn vào chế độ Pomodoro với {workTime}p làm việc và {breakTime}p nghỉ không?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                parentMain.isFocusMode = true;
                parentMain.Hide();

                // THÊM DÒNG NÀY VÀO ĐỂ GIẾT SẠCH CHAT ĐANG MỞ KHI VÀO POMODORO
                parentMain.CloseAllChats();

                FocusAstraWindow focusAstra = new FocusAstraWindow(parentMain);
                // ... (Giữ nguyên phần dưới)
                focusAstra.Show();

                // FIX: Tự động mở Checklist ngay lập tức
                focusAstra.MenuChecklist_Click(null!, null!);

                TomatoTimerWindow timer = new TomatoTimerWindow(workTime, breakTime, focusAstra, parentMain);
                timer.Show();

                this.Close();
            }
        }
    }
}