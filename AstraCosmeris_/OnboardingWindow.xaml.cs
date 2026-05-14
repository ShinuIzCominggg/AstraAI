using System.Windows;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public partial class OnboardingWindow : Window
    {
        public OnboardingWindow()
        {
            InitializeComponent();
            CboProvider.SelectedIndex = 0; // Mặc định chọn Groq
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown(); // Tắt luôn app nếu không chịu cài đặt
        }

        private void BtnTrial_Click(object sender, RoutedEventArgs e)
        {
            // Bơm Key Trial của m vào đây để người ta xài thử
            MemoryManager.Data.ApiKey = "";
            MemoryManager.Data.ApiProvider = "Groq";
            MemoryManager.Data.ApiModel = "llama-3.1-8b-instant";

            MemoryManager.SaveMemory();
            System.Windows.MessageBox.Show("Đã kích hoạt chế độ Dùng thử! Hãy trải nghiệm Astra nhé!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close(); // Đóng Onboarding, Astra sẽ hiện ra
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            string key = TxtApiKey.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                System.Windows.MessageBox.Show("Cậu quên nhập API Key kìa!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MemoryManager.Data.ApiKey = key;
            MemoryManager.Data.ApiProvider = CboProvider.SelectedIndex switch
            {
                1 => "OpenAI",
                2 => "Gemini",
                _ => "Groq"
            };

            MemoryManager.SaveMemory();
            System.Windows.MessageBox.Show("Kết nối thành công! Astra đã sẵn sàng!", "Welcome", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}