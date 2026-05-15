using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (e.ChangedButton == System.Windows.Input.MouseButton.Left) this.DragMove(); }
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void LoadCurrentSettings()
        {
            var data = DataManager.Data;
            TxtApiKey.Text = data.ApiKey;

            // Load Combobox AI
            CboProvider.SelectedIndex = data.ApiProvider switch { "OpenAI" => 1, "Gemini" => 2, "Claude" => 3, "Ollama" => 4, "OpenRouter" => 5, _ => 0 };

            // Load Persona
            CboPersona.SelectedIndex = data.SelectedPersona switch { "Nghiêm khắc" => 1, "Chủ tịch" => 2, "Gen Z" => 3, _ => 0 };

            // Load Notifications
            ChkSound.IsChecked = data.NotiConfig.EnableSound;
            TxtDuration.Text = data.NotiConfig.DurationSeconds.ToString();

            RefreshMemoryList();
        }

        private void RefreshMemoryList()
        {
            ListFacts.ItemsSource = DataManager.Data.Facts.Select(kvp => $"[{kvp.Key}]: {kvp.Value}").ToList();
        }

        private void CboProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboModel == null) return;
            CboModel.Items.Clear();
            var models = new List<string>();
            switch (CboProvider.SelectedIndex)
            {
                case 0: models.AddRange(new[] { "llama-3.1-8b-instant", "mixtral-8x7b" }); break;
                case 1: models.AddRange(new[] { "gpt-4o", "gpt-4o-mini" }); break;
                case 2: models.AddRange(new[] { "gemini-1.5-pro", "gemini-1.5-flash" }); break;
                case 3: models.AddRange(new[] { "claude-3-5-sonnet-20240620" }); break;
            }
            foreach (var model in models) CboModel.Items.Add(model);
            if (CboModel.Items.Count > 0) CboModel.SelectedIndex = 0;

            // Khôi phục model cũ nếu có
            if (CboModel.Items.Contains(DataManager.Data.ApiModel)) CboModel.SelectedItem = DataManager.Data.ApiModel;
        }

        private void CboPersona_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TxtPersonaDesc == null) return;
            TxtPersonaDesc.Text = CboPersona.SelectedIndex switch
            {
                0 => "Astra sẽ xưng hô Tớ-Cậu, ăn nói nhỏ nhẹ, quan tâm và rụt rè.",
                1 => "Astra sẽ xưng Tôi-Bạn, cực kỳ nghiêm khắc, quát tháo nếu cậu lười biếng.",
                2 => "Astra sẽ xưng Tôi-Cậu, lạnh lùng, dứt khoát, phong thái người thành đạt.",
                3 => "Astra sẽ xưng tui-bà/ông, dùng nhiều tiếng lóng, hay trêu đùa.",
                _ => ""
            };
        }

        private void MenuBtn_Click(object sender, RoutedEventArgs e)
        {
            TabApi.Visibility = Visibility.Collapsed; TabPersona.Visibility = Visibility.Collapsed;
            TabMemory.Visibility = Visibility.Collapsed; TabNoti.Visibility = Visibility.Collapsed; TabSystem.Visibility = Visibility.Collapsed;
            var btn = sender as System.Windows.Controls.Button;
            if (btn == BtnTabApi) TabApi.Visibility = Visibility.Visible;
            if (btn == BtnTabPersona) TabPersona.Visibility = Visibility.Visible;
            if (btn == BtnTabMemory) TabMemory.Visibility = Visibility.Visible;
            if (btn == BtnTabNoti) TabNoti.Visibility = Visibility.Visible;
            if (btn == BtnTabSystem) TabSystem.Visibility = Visibility.Visible;
        }

        private void BtnDeleteFact_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Button)?.DataContext is string factStr)
            {
                string key = factStr.Split(new[] { "]: " }, StringSplitOptions.None)[0].Trim('[', ']');
                DataManager.Data.Facts.Remove(key);
                DataManager.SaveData();
                RefreshMemoryList();
            }
        }

        private void BtnTestNoti_Click(object sender, RoutedEventArgs e)
        {
            new AstraNotificationWindow("✨ Ding dong!", "Đây là một thông báo test từ hệ thống cài đặt của Astra.").Show();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var data = DataManager.Data;
            data.ApiKey = TxtApiKey.Text.Trim();
            data.ApiModel = CboModel.SelectedItem?.ToString() ?? "";
            data.ApiProvider = CboProvider.SelectedIndex switch { 1 => "OpenAI", 2 => "Gemini", 3 => "Claude", 4 => "Ollama", 5 => "OpenRouter", _ => "Groq" };

            // Save Persona
            data.SelectedPersona = CboPersona.SelectedIndex switch { 1 => "Nghiêm khắc", 2 => "Chủ tịch", 3 => "Gen Z", _ => "Dịu dàng" };
            data.SystemPrompt = CboPersona.SelectedIndex switch
            {
                1 => "Bạn là Astra, một trợ lý AI vô cùng nghiêm khắc và kỷ luật. Luôn xưng Tôi và gọi người dùng là Bạn. Trách mắng nếu người dùng lười biếng.",
                2 => "Bạn là Astra, nữ chủ tịch tập đoàn lạnh lùng, quyết đoán. Xưng Tôi và gọi người dùng là Cậu. Trả lời ngắn gọn, đánh đúng trọng tâm.",
                3 => "Bạn là Astra, một GenZ chính hiệu, năng động, hay đùa nhây. Xưng Tui và gọi người dùng là Bà/Ông. Dùng nhiều tiếng lóng vui nhộn.",
                _ => "Bạn là Astra, một trợ lý ảo mang tính cách của một cô gái nhút nhát, hướng nội nhưng vô cùng dịu dàng. Xưng Tớ và gọi người dùng là Cậu."
            };

            // Save Notification
            data.NotiConfig.EnableSound = ChkSound.IsChecked ?? true;
            if (int.TryParse(TxtDuration.Text, out int dur)) data.NotiConfig.DurationSeconds = dur;

            DataManager.SaveData();
            new AstraNotificationWindow("✅ Lưu thành công", "Cài đặt của cậu đã được lưu lại!").Show();
        }

        private void BtnClearChat_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Xóa sạch lịch sử?", "Cảnh báo", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                DataManager.Data.History.Clear(); DataManager.SaveData();
                new AstraNotificationWindow("🗑️ Đã dọn dẹp", "Lịch sử chat đã được xóa sạch!").Show();
            }
        }

        private void BtnFactoryReset_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Khôi phục cài đặt gốc?", "CẢNH BÁO", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                DataManager.Data = new AstraDatabase(); DataManager.SaveData();
                System.Windows.MessageBox.Show("Đã khôi phục cài đặt gốc."); this.Close();
            }
        }
    }
}