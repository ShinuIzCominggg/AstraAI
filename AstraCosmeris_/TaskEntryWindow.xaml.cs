using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public partial class TaskEntryWindow : Window
    {
        private readonly string currentDateKey;

        public TaskEntryWindow(DateTime selectedDate)
        {
            InitializeComponent();
            currentDateKey = selectedDate.ToString("yyyy-MM-dd");
            TxtTitle.Text = $"Lịch trình: {selectedDate:dd/MM/yyyy}";
            LoadTasks();
        }

        private void LoadTasks()
        {
            if (DataManager.Data.Tasks.TryGetValue(currentDateKey, out string? value))
                TxtTasks.Text = value;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string[] lines = TxtTasks.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var processedLines = new List<string>();

            // HOT FIX: Regex bọc thép, bắt mọi kiểu gõ (vd: +5, +5p, 5m, 8:30, 08h30)
            var relativeRegex = new Regex(@"^\+?\s*(\d+)\s*[pPmM]*\s+(.+)$");
            var absoluteRegex = new Regex(@"^(\d{1,2})[:hH](\d{2})\s+(.+)$");
            DateTime now = DateTime.Now;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                // Đã đúng chuẩn [HH:mm] rồi thì tha cho nó
                if (Regex.IsMatch(trimmed, @"^\[\d{2}:\d{2}\]"))
                {
                    processedLines.Add(trimmed);
                    continue;
                }

                var relMatch = relativeRegex.Match(trimmed);
                if (relMatch.Success && int.TryParse(relMatch.Groups[1].Value, out int minutes))
                {
                    processedLines.Add($"[{now.AddMinutes(minutes):HH:mm}] {relMatch.Groups[2].Value}");
                    continue;
                }

                var absMatch = absoluteRegex.Match(trimmed);
                if (absMatch.Success)
                {
                    string h = absMatch.Groups[1].Value.PadLeft(2, '0');
                    string m = absMatch.Groups[2].Value;
                    processedLines.Add($"[{h}:{m}] {absMatch.Groups[3].Value}");
                    continue;
                }
                processedLines.Add(trimmed);
            }

            TxtTasks.Text = string.Join(Environment.NewLine, processedLines);
            DataManager.Data.Tasks[currentDateKey] = TxtTasks.Text;
            DataManager.SaveData();

            TxtStatus.Text = "✅ Đã lưu!";
            await Task.Delay(1500);
            TxtStatus.Text = "";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
    }
}