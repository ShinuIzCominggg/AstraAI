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
                parentMain.forceClose = true; // Cấp thẻ bài miễn tử (cho phép tắt thật)
                parentMain.Close(); // Ra lệnh cho não bộ tự sát (lúc này OnClosing sẽ cho qua)
            }
            else
            {
                System.Windows.Application.Current.Shutdown(); // Đề phòng lỗi null
            }
            this.Close(); // Đóng chính cái bảng xác nhận này lại
        }
    }
}