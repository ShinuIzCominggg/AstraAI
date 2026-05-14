using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

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
            TxtApiKey.Text = MemoryManager.Data.ApiKey;
            TxtPrompt.Text = MemoryManager.Data.SystemPrompt;
            ListFacts.ItemsSource = MemoryManager.Data.Facts;

            CboProvider.SelectedIndex = MemoryManager.Data.ApiProvider switch
            {
                "OpenAI" => 1,
                "Gemini" => 2,
                "Claude" => 3,
                "Ollama" => 4,
                "OpenRouter" => 5,
                _ => 0
            };

            string savedModel = MemoryManager.Data.ApiModel;
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

            if (!foundInList && !string.IsNullOrEmpty(savedModel)) TxtModel.Text = savedModel;
            else TxtModel.Text = "";
        }

        // --- DANH SÁCH MODEL 2026 TỰ ĐỘNG ---
        private void CboProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboModel == null) return;
            CboModel.Items.Clear();
            List<string> models = new List<string>();

            switch (CboProvider.SelectedIndex)
            {
                case 0: // Groq
                    models.AddRange(new[] { "meta-llama/llama-4-scout-17b-16e-instruct", "llama-3.3-70b-versatile", "qwen/qwen3-32b", "groq/compound", "llama-3.1-8b-instant" }); break;
                case 1: // OpenAI
                    models.AddRange(new[] { "gpt-4.5-turbo", "gpt-4o", "o3-mini" }); break;
                case 2: // Gemini
                    models.AddRange(new[] { "gemini-3.1-pro-preview", "gemini-3-flash-preview", "gemini-3.1-flash-lite-preview", "gemini-2.5-flash" }); break;
                case 3: // Claude
                    models.AddRange(new[] { "claude-3.5-sonnet-20241022", "claude-3-opus-20240229", "claude-3.5-haiku-20241022" }); break;
                case 4: // Ollama
                    models.AddRange(new[] { "llama3", "mistral", "qwen2.5", "deepseek-coder" }); break;
                case 5: // OpenRouter
                    models.AddRange(new[] { "meta-llama/llama-3-8b-instruct:free", "google/gemini-pro", "anthropic/claude-3-sonnet" }); break;
            }

            foreach (var model in models) CboModel.Items.Add(model);
            if (CboModel.Items.Count > 0) CboModel.SelectedIndex = 0;
        }

        // --- LƯU DỮ LIỆU ---
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            MemoryManager.Data.ApiKey = TxtApiKey.Text.Trim();
            MemoryManager.Data.SystemPrompt = TxtPrompt.Text.Trim();

            // Lưu Model (Ưu tiên TextBox tự gõ)
            string customModel = TxtModel.Text.Trim();
            if (!string.IsNullOrEmpty(customModel)) MemoryManager.Data.ApiModel = customModel;
            else if (CboModel.SelectedItem != null) MemoryManager.Data.ApiModel = CboModel.SelectedItem.ToString();
            else MemoryManager.Data.ApiModel = "";

            // Lưu Provider
            MemoryManager.Data.ApiProvider = CboProvider.SelectedIndex switch
            {
                1 => "OpenAI",
                2 => "Gemini",
                3 => "Claude",
                4 => "Ollama",
                5 => "OpenRouter",
                _ => "Groq"
            };

            MemoryManager.SaveMemory();
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
                MemoryManager.Data.History.Clear(); MemoryManager.SaveMemory(); System.Windows.MessageBox.Show("Đã xóa lịch sử chat!");
            }
        }

        private void BtnFactoryReset_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("CẢNH BÁO: Astra sẽ quên hết mọi thứ về cậu. Cậu có chắc chắn không?", "KHÔI PHỤC CÀI ĐẶT GỐC", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                MemoryManager.Data = new MemoryData(); MemoryManager.SaveMemory();
                System.Windows.MessageBox.Show("Đã khôi phục cài đặt gốc."); this.Close();
            }
        }
    }
}