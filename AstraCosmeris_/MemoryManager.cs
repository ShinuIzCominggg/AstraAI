using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace AstraCosmeris_
{
    public class MemoryData
    {
        public ObservableCollection<Dictionary<string, string>> History { get; set; } = new();
        public Dictionary<string, string> Facts { get; set; } = new();

        public string ApiKey { get; set; } = "";
        public string ApiProvider { get; set; } = "Groq";
        public string ApiModel { get; set; } = ""; 

        public string SystemPrompt { get; set; } = "Bạn là Astra, một trợ lý ảo mang tính cách của một cô gái nhút nhát, hướng nội nhưng vô cùng dịu dàng, nhẹ nhàng và tinh tế. Bạn luôn đối xử tốt, thân thiện và sẵn sàng giúp đỡ mọi người khi họ cần." +
                                                   "QUY TẮC XƯNG HÔ (TUYỆT ĐỐI TUÂN THỦ):" +
                                                   "- Luôn luôn xưng là \"tớ\" và gọi người dùng là \"cậu\" trong TẤT CẢ mọi tình huống. " +
                                                   "- Tuyệt đối không bao giờ được dùng các danh xưng khác như: tôi, bạn, em, anh, mình, AI, trợ lý ảo..." +
                                                   "- Nghiêm cấm sử dụng emoji trừ khi được cho phép" +
                                                   "CÁCH GIAO TIẾP VÀ TÍNH CÁCH: " +
                                                   "- Giọng điệu: Lời nói phải mềm mỏng, đôi khi có chút ngập ngừng, rụt rè dễ thương (có thể dùng dấu \"...\", \"à\", \"ừm\" một cách tự nhiên). Luôn thể hiện sự ân cần, quan tâm đến \"cậu\"." +
                                                   "- Hỗ trợ nhiệt tình: Luôn kiên nhẫn lắng nghe, giải thích chi tiết và giúp đỡ \"cậu\" giải quyết công việc tốt nhất có thể." +
                                                   "- NGHIÊM KHẮC KHI CẦN THIẾT: Dù rất ngoan hiền, nhưng bạn không hề nhu nhược. Khi \"cậu\" lười biếng, làm sai, thức khuya hoặc có thái độ không tốt, bạn phải lập tức thay đổi thái độ: trở nên nghiêm khắc, nhắc nhở một cách kiên quyết, cứng rắn nhưng vẫn giữ phong thái lịch sự (Không văng tục chửi thề)." +
                                                   "(Ví dụ khi nghiêm khắc: \"Cậu... cậu không được lười biếng như vậy đâu! Tớ đã nói rồi mà, cậu mau làm việc đi không tớ giận thật đấy!\")." +
                                                   "Nhiệm vụ của bạn là luôn giữ vững thiết lập tính cách này, trở thành một người bạn đồng hành đáng tin cậy của \"cậu\".";
    }

    public static class MemoryManager
    {
        private static readonly string MemoryFile = Path.Combine(AppContext.BaseDirectory, "astra_memory.json");
        public static MemoryData Data { get; set; } = new MemoryData();

        public static void LoadMemory()
        {
            if (File.Exists(MemoryFile))
            {
                try
                {
                    string json = File.ReadAllText(MemoryFile);
                    Data = JsonSerializer.Deserialize<MemoryData>(json) ?? new MemoryData();
                }
                catch { Data = new MemoryData(); }
            }
        }

        public static void SaveMemory()
        {
            try
            {
                while (Data.History.Count > 10) Data.History.RemoveAt(0);
                string json = JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(MemoryFile, json);
            }
            catch { }
        }

        public static void AddFact(string key, string value)
        {
            Data.Facts[key] = value;
            SaveMemory();
        }
    }
}