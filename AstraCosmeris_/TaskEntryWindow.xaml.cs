using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public partial class TaskEntryWindow : Window
    {
        private readonly string tasksFilePath = Path.Combine(AppContext.BaseDirectory, "tasks.json");
        private readonly string currentDateKey;
        private Dictionary<string, string> allTasks = new();

        public TaskEntryWindow(DateTime selectedDate)
        {
            InitializeComponent();
            currentDateKey = selectedDate.ToString("yyyy-MM-dd");
            TxtTitle.Text = $"Lịch trình: {selectedDate:dd/MM/yyyy}";
            LoadTasks();
        }

        private void LoadTasks()
        {
            if (File.Exists(tasksFilePath))
            {
                try { allTasks = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(tasksFilePath)) ?? new(); }
                catch { allTasks = new(); }
            }
            if (allTasks.TryGetValue(currentDateKey, out string? value)) TxtTasks.Text = value;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string[] lines = TxtTasks.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var processedLines = new List<string>();

            var relativeRegex = new Regex(@"^\+?(\d+)[pP]\s+(.+)$");
            var absoluteRegex = new Regex(@"^(\d{1,2}:\d{2})\s+(.+)$");
            DateTime now = DateTime.Now;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                var relMatch = relativeRegex.Match(trimmed);
                if (relMatch.Success && int.TryParse(relMatch.Groups[1].Value, out int minutes))
                {
                    processedLines.Add($"[{now.AddMinutes(minutes):HH:mm}] {relMatch.Groups[2].Value}");
                    continue;
                }

                var absMatch = absoluteRegex.Match(trimmed);
                if (absMatch.Success)
                {
                    string time = absMatch.Groups[1].Value.PadLeft(5, '0');
                    processedLines.Add($"[{time}] {absMatch.Groups[2].Value}");
                    continue;
                }

                processedLines.Add(trimmed);
            }

            TxtTasks.Text = string.Join(Environment.NewLine, processedLines);
            allTasks[currentDateKey] = TxtTasks.Text;

            File.WriteAllText(tasksFilePath, JsonSerializer.Serialize(allTasks, new JsonSerializerOptions { WriteIndented = true }));

            TxtStatus.Text = "✅ Đã lưu!";
            await Task.Delay(1500);
            TxtStatus.Text = "";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }
    }
}