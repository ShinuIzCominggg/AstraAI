using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public partial class TaskEntryWindow : Window
    {
        private string tasksFilePath = Path.Combine(AppContext.BaseDirectory, "tasks.json");
        private string currentDateKey;
        private Dictionary<string, string> allTasks = new Dictionary<string, string>();

        public TaskEntryWindow(DateTime selectedDate)
        {
            InitializeComponent();

            // Format ngày làm key (VD: "2026-02-25")
            currentDateKey = selectedDate.ToString("yyyy-MM-dd");
            TxtTitle.Text = $"Lịch trình: {selectedDate.ToString("dd/MM/yyyy")}";

            LoadTasks();
        }

        private void LoadTasks()
        {
            if (File.Exists(tasksFilePath))
            {
                string json = File.ReadAllText(tasksFilePath);
                try
                {
                    allTasks = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
                catch { allTasks = new Dictionary<string, string>(); }
            }

            // Nếu ngày này có task rồi thì nhét vào TextBox
            if (allTasks.ContainsKey(currentDateKey))
            {
                TxtTasks.Text = allTasks[currentDateKey];
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // --- BÙA CHÚ BIẾN HÌNH FORMAT TEXT ---
            string rawText = TxtTasks.Text;
            string[] lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> processedLines = new List<string>();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                // Dạng 1: Thời gian tương đối (+10m, +2h)
                // Cú pháp: Bắt đầu bằng dấu +, theo sau là số, rồi chữ m hoặc h
                System.Text.RegularExpressions.Match relativeMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"^\+(\d+)(m|h)\s+(.*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (relativeMatch.Success)
                {
                    int value = int.Parse(relativeMatch.Groups[1].Value);
                    string unit = relativeMatch.Groups[2].Value.ToLower();
                    string content = relativeMatch.Groups[3].Value;

                    DateTime targetTime = DateTime.Now;
                    if (unit == "m") targetTime = targetTime.AddMinutes(value);
                    else if (unit == "h") targetTime = targetTime.AddHours(value);

                    // Tự động đóng ngoặc và cộng giờ
                    processedLines.Add($"[{targetTime:HH:mm}] {content}");
                    continue;
                }

                // Dạng 2: Thời gian tuyệt đối nhưng lười gõ ngoặc (14:30 làm gì đó)
                System.Text.RegularExpressions.Match absoluteMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"^(\d{1,2}:\d{2})\s+(.*)");
                if (absoluteMatch.Success)
                {
                    string time = absoluteMatch.Groups[1].Value;
                    // Độn thêm số 0 nếu user gõ 8:30 thay vì 08:30 cho chuẩn format
                    if (time.Length == 4) time = "0" + time;
                    string content = absoluteMatch.Groups[2].Value;

                    processedLines.Add($"[{time}] {content}");
                    continue;
                }

                // Nếu không thuộc 2 dạng trên (hoặc đã gõ chuẩn rồi) thì giữ nguyên
                processedLines.Add(trimmed);
            }

            // Cập nhật lại TextBox trên giao diện để m nhìn thấy nó tự biến hình
            TxtTasks.Text = string.Join(Environment.NewLine, processedLines);

            // --- LƯU VÀO JSON NHƯ BÌNH THƯỜNG ---
            allTasks[currentDateKey] = TxtTasks.Text;

            string json = JsonSerializer.Serialize(allTasks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tasksFilePath, json);

            TxtStatus.Text = "✅ Đã lưu!";
            await System.Threading.Tasks.Task.Delay(1500);
            TxtStatus.Text = "";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}