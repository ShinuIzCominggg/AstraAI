using System;
using System.Windows;

namespace AstraCosmeris_
{
    public partial class ExitConfirmWindow : Window
    {
        private MainWindow parentMain;

        public ExitConfirmWindow()
        {
            InitializeComponent();
        }

        public ExitConfirmWindow(MainWindow main)
        {
            InitializeComponent();
            parentMain = main;
        }

        private void BtnStay_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Đóng bảng cáu kỉnh, quay lại làm việc
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            if (parentMain != null)
            {
                parentMain.forceClose = true; // Cấp quyền khai tử
            }
            // Đóng toàn bộ hệ thống
            System.Windows.Application.Current.Shutdown();
        }
    }
}