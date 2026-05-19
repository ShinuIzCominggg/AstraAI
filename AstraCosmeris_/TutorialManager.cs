using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AstraCosmeris_
{
    public class AstraQuest : INotifyPropertyChanged
    {
        private bool _isCompleted;
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Hint { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public bool IsCompleted
        {
            get => _isCompleted;
            set { _isCompleted = value; OnPropertyChanged(nameof(IsCompleted)); OnPropertyChanged(nameof(TextDecoration)); OnPropertyChanged(nameof(Opacity)); }
        }

        public TextDecorationCollection TextDecoration => IsCompleted ? TextDecorations.Strikethrough : new TextDecorationCollection();
        public double Opacity => IsCompleted ? 0.5 : 1.0;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public static class TutorialManager
    {
        public static ObservableCollection<AstraQuest> Quests { get; set; } = new ObservableCollection<AstraQuest>();
        public static TutorialBoardWindow? BoardWindow;

        public static void Initialize()
        {
            Quests.Clear();
            Quests.Add(new AstraQuest { Id = "drag", Title = "Thử túm cổ áo tớ kéo đi chỗ khác xem!", Hint = "Nhấn giữ chuột trái vào người tớ rồi kéo đi nhé!", SuccessMessage = "Á á chóng mặt quá! Nhưng cậu biết kéo tớ rồi đó!" });
            Quests.Add(new AstraQuest { Id = "smartra", Title = "Double-click vào tớ để mở Thanh lệnh.", Hint = "Nhấp đúp chuột trái (Double-click) thật nhanh vào người tớ.", SuccessMessage = "Thanh lệnh Smartra đã mở! Cậu có thể gõ mọi thứ ở đây." });
            Quests.Add(new AstraQuest { Id = "chat", Title = "Chat với tớ hoặc gõ 1 phép tính.", Hint = "Mở thanh lệnh lên, gõ '1+1' rồi Enter, hoặc trò chuyện với tớ nha.", SuccessMessage = "Tớ thông minh lắm đúng không? Hehe!" });
            Quests.Add(new AstraQuest { Id = "dashboard", Title = "Chuột phải chọn Dashboard (Nhà của tụi mình).", Hint = "Click chuột phải vào người tớ, sẽ có 1 menu hiện ra, chọn Dashboard.", SuccessMessage = "Chào mừng cậu đến với Căn cứ bí mật!" });
            Quests.Add(new AstraQuest { Id = "note", Title = "Vào Notes, kéo 1 tờ ghi chú ném ra ngoài.", Hint = "Trong Dashboard, chọn tab Notes, bấm vào 1 tờ ghi chú rồi kéo nó vứt ra Desktop.", SuccessMessage = "Bùa chú đã được dán thành công!" });
            Quests.Add(new AstraQuest { Id = "pomodoro", Title = "Bật chế độ Pomodoro để Focus.", Hint = "Chuột phải vào tớ, chọn Pomodoro rồi bấm Bắt đầu thôi!", SuccessMessage = "Tới giờ tập trung làm việc rồi!!" });
        }

        public static void CompleteQuest(string questId, MainWindow parent)
        {
            // Nếu đã qua bài hướng dẫn rồi thì bỏ qua
            if (DataManager.Data.Facts.ContainsKey("__TutorialDone") && DataManager.Data.Facts["__TutorialDone"] == "true") return;

            var quest = Quests.FirstOrDefault(q => q.Id == questId);
            if (quest != null && !quest.IsCompleted)
            {
                quest.IsCompleted = true;

                // Hiện bong bóng thoại khen ngợi
                parent.ChangeState(PetState.Happy);
                new SpeechBubble($"✨ {quest.SuccessMessage}", parent, 4000).Show();

                CheckAllCompleted(parent);
            }
        }

        public static void ShowHint(string questId, MainWindow parent)
        {
            var quest = Quests.FirstOrDefault(q => q.Id == questId);
            if (quest != null && !quest.IsCompleted)
            {
                new SpeechBubble($"💡 Ý tớ là:\n{quest.Hint}", parent, 5000).Show();
            }
        }

        private static async void CheckAllCompleted(MainWindow parent)
        {
            if (Quests.All(q => q.IsCompleted))
            {
                DataManager.AddFact("__TutorialDone", "true");

                if (BoardWindow != null)
                {
                    BoardWindow.PlayVictoryEffect();
                    await Task.Delay(2000); // Đợi pháo hoa nổ xong

                    new SpeechBubble("🎉 Đỉnh vl! Cậu đã nắm giữ toàn bộ sức mạnh của tớ rồi. Từ giờ chúng ta là đối tác chính thức nhé!", parent, 6000).Show();

                    await Task.Delay(3000);
                    BoardWindow.Close();
                    BoardWindow = null;
                }
            }
        }
    }
}