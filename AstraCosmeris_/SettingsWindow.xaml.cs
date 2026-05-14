using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        // --- ĐỔ DỮ LIỆU LÊN UI ---
        private void LoadCurrentSettings()
        {
            var data = DataManager.Data;
            TxtApiKey.Text = data.ApiKey;
            TxtPrompt.Text = data.SystemPrompt;
            ListFacts.ItemsSource = data.Facts;

            CboProvider.SelectedIndex = data.ApiProvider switch
            {
                "OpenAI" => 1,
                "Gemini" => 2,
                "Claude" => 3,
                "Ollama" => 4,
                "OpenRouter" => 5,
                _ => 0
            };

            string savedModel = data.ApiModel;
            bool foundInList = false;

            for (int i = 0; i < CboModel.Items.Count; i++)
            {
                if (CboModel.Items[i].ToString() == savedModel)
                {
                    CboModel.SelectedIndex = i;
                    foundInList = true;
                    break;
                }
            }

            TxtModel.Text = (!foundInList && !string.IsNullOrEmpty(savedModel)) ? savedModel : "";
        }

        // --- DANH SÁCH MODEL 2026 TỰ ĐỘNG ---
        private void CboProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboModel == null) return;
            CboModel.Items.Clear();
            var models = new List<string>();

            switch (CboProvider.SelectedIndex)
            {
                case 0: models.AddRange(new[] { "meta-llama/llama-4-scout-17b-16e-instruct", "llama-3.3-70b-versatile", "qwen/qwen3-32b", "groq/compound", "llama-3.1-8b-instant" }); break;
                case 1: models.AddRange(new[] { "gpt-4.5-turbo", "gpt-4o", "o3-mini" }); break;
                case 2: models.AddRange(new[] { "gemini-3.1-pro-preview", "gemini-3-flash-preview", "gemini-3.1-flash-lite-preview", "gemini-2.5-flash" }); break;
                case 3: models.AddRange(new[] { "claude-3.5-sonnet-20241022", "claude-3-opus-20240229", "claude-3.5-haiku-20241022" }); break;
                case 4: models.AddRange(new[] { "llama3", "mistral", "qwen2.5", "deepseek-coder" }); break;
                case 5: models.AddRange(new[] { "meta-llama/llama-3-8b-instruct:free", "google/gemini-pro", "anthropic/claude-3-sonnet" }); break;
            }

            foreach (var model in models) CboModel.Items.Add(model);
            if (CboModel.Items.Count > 0) CboModel.SelectedIndex = 0;
        }

        // --- LƯU DỮ LIỆU ---
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var data = DataManager.Data;
            data.ApiKey = TxtApiKey.Text.Trim();
            data.SystemPrompt = TxtPrompt.Text.Trim();

            string customModel = TxtModel.Text.Trim();
            data.ApiModel = !string.IsNullOrEmpty(customModel) ? customModel : (CboModel.SelectedItem?.ToString() ?? "");

            data.ApiProvider = CboProvider.SelectedIndex switch
            {
                1 => "OpenAI",
                2 => "Gemini",
                3 => "Claude",
                4 => "Ollama",
                5 => "OpenRouter",
                _ => "Groq"
            };

            DataManager.SaveData();
            System.Windows.MessageBox.Show("Đã lưu cấu hình của Astra thành công!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuBtn_Click(object sender, RoutedEventArgs e)
        {
            TabApi.Visibility = Visibility.Collapsed; TabPrompt.Visibility = Visibility.Collapsed;
            TabMemory.Visibility = Visibility.Collapsed; TabSystem.Visibility = Visibility.Collapsed;

            var btn = sender as System.Windows.Controls.Button;
            if (btn == BtnTabApi) TabApi.Visibility = Visibility.Visible;
            if (btn == BtnTabPrompt) TabPrompt.Visibility = Visibility.Visible;
            if (btn == BtnTabMemory) TabMemory.Visibility = Visibility.Visible;
            if (btn == BtnTabSystem) TabSystem.Visibility = Visibility.Visible;
        }

        private void BtnClearChat_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Cậu có chắc muốn xóa sạch lịch sử trò chuyện?", "Cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                DataManager.Data.History.Clear();
                DataManager.SaveData();
                System.Windows.MessageBox.Show("Đã xóa lịch sử chat!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnFactoryReset_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("CẢNH BÁO: Astra sẽ quên hết mọi thứ về cậu. Cậu có chắc chắn không?", "KHÔI PHỤC CÀI ĐẶT GỐC", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                DataManager.SaveData();
                System.Windows.MessageBox.Show("Đã khôi phục cài đặt gốc.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }
    }
}