using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AstraCosmeris_
{
    public partial class PomodoroSetupWindow : Window
    {
        private int workTime = 25;
        private int breakTime = 5;
        private MainWindow parentMain;
        private int easterEggCounter = 0; // Biến bảo kê Easter Egg

        public PomodoroSetupWindow(MainWindow main)
        {
            InitializeComponent();
            parentMain = main;
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            TxtWork.Text = workTime == -1 ? "1s" : workTime.ToString();
            TxtBreak.Text = breakTime.ToString();

            BtnWorkUp.Foreground = workTime >= 35 ? System.Windows.Media.Brushes.Gray : new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2F3273"));
            BtnWorkDown.Foreground = workTime <= 15 && workTime > 0 && easterEggCounter < 5 ? System.Windows.Media.Brushes.Gray : new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2F3273"));
            BtnBreakUp.Foreground = breakTime >= 15 ? System.Windows.Media.Brushes.Gray : new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2F3273"));
            BtnBreakDown.Foreground = breakTime <= 5 ? System.Windows.Media.Brushes.Gray : new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2F3273"));
        }

        private void BtnWorkDown_Click(object sender, RoutedEventArgs e)
        {
            if (workTime > 15)
            {
                workTime--;
                easterEggCounter = 0;
            }
            else
            {
                easterEggCounter++;
                if (easterEggCounter == 5) workTime = 1;
                else if (easterEggCounter == 10) workTime = -1; // Cờ hiệu 1 giây
            }
            UpdateButtonStates();
        }

        private void BtnWorkUp_Click(object sender, RoutedEventArgs e)
        {
            if (workTime < 35)
            {
                if (workTime == -1 || workTime == 1) workTime = 15; // Phá Easter Egg thì về mức 15p
                else workTime++;

                easterEggCounter = 0;
                UpdateButtonStates();
            }
        }

        private void BtnBreakUp_Click(object sender, RoutedEventArgs e) { if (breakTime < 15) { breakTime++; UpdateButtonStates(); } }
        private void BtnBreakDown_Click(object sender, RoutedEventArgs e) { if (breakTime > 5) { breakTime--; UpdateButtonStates(); } }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            string workText = workTime == -1 ? "1s" : $"{workTime}p";
            var result = System.Windows.MessageBox.Show($"Cậu có chắc chắn muốn vào chế độ Pomodoro với {workText} làm việc và {breakTime}p nghỉ không?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                parentMain.isFocusMode = true;
                parentMain.Hide();
                parentMain.CloseAllChats();

                FocusAstraWindow focusAstra = new FocusAstraWindow(parentMain);
                focusAstra.Show();
                focusAstra.MenuChecklist_Click(null, null);

                new TomatoTimerWindow(workTime, breakTime, focusAstra, parentMain).Show();
                this.Close();
            }
        }
    }
}