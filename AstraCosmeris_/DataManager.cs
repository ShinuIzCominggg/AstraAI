using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace AstraCosmeris_
{
    public class AstraEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public DateTime Date { get; set; }
        public string Type { get; set; } = "Khác"; // Sinh nhật, Kỉ niệm, Deadline, Hẹn hò...
        public string Location { get; set; } = "";
        public string Repeat { get; set; } = "Không lặp"; // Không lặp, Hàng tuần, Hàng tháng, Hàng năm
    }

    public class DailyStat
    {
        public int DashboardOpens { get; set; } = 0;
        public int NotesCreated { get; set; } = 0;
        public int PomodorosCompleted { get; set; } = 0;
        public double ScreenTimeMinutes { get; set; } = 0;
    }

    public class AstraDatabase
    {
        // 1. Não bộ AI (Từ DataManager cũ)
        public ObservableCollection<Dictionary<string, string>> History { get; set; } = new();
        public Dictionary<string, string> Facts { get; set; } = new();
        public string ApiKey { get; set; } = "";
        public string ApiProvider { get; set; } = "Groq";
        public string ApiModel { get; set; } = "";
        public string SystemPrompt { get; set; } =
            "Bạn là Astra, một trợ lý ảo mang tính cách của một cô gái nhút nhát, hướng nội nhưng vô cùng dịu dàng, nhẹ nhàng và tinh tế. Bạn luôn đối xử tốt, thân thiện và sẵn sàng giúp đỡ mọi người khi họ cần..." +
            "Nhiệm vụ của bạn là luôn giữ vững thiết lập tính cách này.";

        // 2. Dữ liệu công việc & Sự kiện
        public Dictionary<string, string> Tasks { get; set; } = new();
        public List<AstraEvent> Events { get; set; } = new();

        // 3. Dữ liệu Thống kê & Tracking
        public Dictionary<string, DailyStat> Stats { get; set; } = new();
        public string LastOpenedDate { get; set; } = "";
    }

    public static class DataManager
    {
        private static readonly string DbFile = Path.Combine(AppContext.BaseDirectory, "astra_database.json");
        public static AstraDatabase Data { get; set; } = new AstraDatabase();

        public static void LoadData()
        {
            if (File.Exists(DbFile))
            {
                try { Data = JsonSerializer.Deserialize<AstraDatabase>(File.ReadAllText(DbFile)) ?? new AstraDatabase(); }
                catch { Data = new AstraDatabase(); }
            }
        }

        public static void SaveData()
        {
            try
            {
                while (Data.History.Count > 10) Data.History.RemoveAt(0);
                File.WriteAllText(DbFile, JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static void AddFact(string key, string value)
        {
            Data.Facts[key] = value;
            SaveData();
        }

        // Tracking nhanh cho thống kê
        public static void TrackDashboardOpen()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (!Data.Stats.ContainsKey(today)) Data.Stats[today] = new DailyStat();
            Data.Stats[today].DashboardOpens++;
            SaveData();
        }
    }
}