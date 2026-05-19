using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public partial class ChatInputWindow : Window
    {
        private MainWindow parentPet;
        private SpeechBubble? thinkingBubble;

        public ChatInputWindow(MainWindow parent)
        {
            InitializeComponent();
            parentPet = parent;

            this.Loaded += (s, e) => {
                this.Left = parentPet.Left - (this.ActualWidth / 2) + (parentPet.Width / 2);
                this.Top = parentPet.Top + parentPet.Height + 5;
                InputBox.Focus();
            };
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e) => await SendMessage();

        private async void InputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                e.Handled = true;
                await SendMessage();
            }
        }

        private async Task SendMessage()
        {
            string userText = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            InputBox.Text = "";

            if (HandleSystemCommand(userText)) return;

            // --- BỘ MÁY QUÉT TRÍ NHỚ ---
            string lowerMsg = userText.ToLower();
            if (lowerMsg.Contains("tên tớ là") || lowerMsg.Contains("my name is"))
                DataManager.AddFact("Tên", userText.Substring(lowerMsg.IndexOf("là") + 2).Trim());
            else if (lowerMsg.Contains("tớ thích") || lowerMsg.Contains("i like"))
                DataManager.AddFact("Sở thích", userText.Substring(lowerMsg.IndexOf("thích") + 5).Trim());
            else if (lowerMsg.Contains("tớ ghét") || lowerMsg.Contains("i hate"))
                DataManager.AddFact("Điểm ghét", userText.Substring(lowerMsg.IndexOf("ghét") + 5).Trim());
            else if (lowerMsg.Contains("tớ đang làm") || lowerMsg.Contains("nghề của tớ là"))
                DataManager.AddFact("Công việc", userText.Substring(lowerMsg.IndexOf("là") + 2).Trim());
            else if (lowerMsg.Contains("tớ ở") || lowerMsg.Contains("tớ sống tại"))
                DataManager.AddFact("Nơi ở", userText.Substring(lowerMsg.IndexOf("ở") + 2).Trim());
            else if (lowerMsg.Contains("mục tiêu của tớ là") || lowerMsg.Contains("tớ muốn đạt được"))
                DataManager.AddFact("Mục tiêu", userText.Substring(lowerMsg.IndexOf("là") + 2).Trim());

            DataManager.Data.History.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userText } });
            DataManager.SaveData();

            parentPet.ChangeState(PetState.Thinking);
            thinkingBubble = new SpeechBubble("💭 Hmm... để tớ nghĩ xíu~", parentPet, 99999);
            thinkingBubble.Show();

            string reply = await AstraBrain.ThinkAndReply();

            DataManager.Data.History.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", reply } });
            DataManager.SaveData();

            thinkingBubble.CloseBubble(keepState: true);

            parentPet.ChangeState(PetState.Happy);
            new SpeechBubble(reply, parentPet, 10000).Show();
        }

        private bool HandleSystemCommand(string msg)
        {
            string lowerMsg = msg.ToLower().Trim();

            if (lowerMsg.Contains("mở edge") || (lowerMsg.Contains("open") && lowerMsg.Contains("edge"))) { Process.Start(new ProcessStartInfo("msedge") { UseShellExecute = true }); ShowActionBubble("🌐 Đang mở Edge cho cậu lướt web nè~"); return true; }
            if (lowerMsg.Contains("mở word") || (lowerMsg.Contains("open") && lowerMsg.Contains("word"))) { Process.Start(new ProcessStartInfo("winword") { UseShellExecute = true }); ShowActionBubble("📄 Đã mở Word! Chúc cậu làm việc vui vẻ nha"); return true; }
            if (lowerMsg.Contains("mở excel") || (lowerMsg.Contains("open") && lowerMsg.Contains("excel"))) { Process.Start(new ProcessStartInfo("excel") { UseShellExecute = true }); ShowActionBubble("📊 Excel đã sẵn sàng!"); return true; }
            if (lowerMsg.Contains("mở powerpoint") || (lowerMsg.Contains("open") && lowerMsg.Contains("powerpoint"))) { Process.Start(new ProcessStartInfo("powerpnt") { UseShellExecute = true }); ShowActionBubble("📈 PowerPoint đã mở!"); return true; }
            if (lowerMsg.Contains("mở notepad") || lowerMsg.Contains("open notepad")) { Process.Start(new ProcessStartInfo("notepad") { UseShellExecute = true }); ShowActionBubble("📓 Notepad lên sóng!"); return true; }
            if (lowerMsg is "quit" or "bye" or "exit" || lowerMsg.Contains("cút") || lowerMsg == "tắt") { ShowActionBubble("👋 Cậu định đi đâu đó..."); Task.Delay(1500).ContinueWith(_ => System.Windows.Application.Current.Dispatcher.Invoke(() => parentPet.Close())); return true; }
            if (lowerMsg is "restart" || lowerMsg.Contains("khởi động lại")) { ShowActionBubble("🔄 Tớ sẽ trở lại ngay~"); Task.Delay(1500).ContinueWith(_ => System.Windows.Application.Current.Dispatcher.Invoke(() => { Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? ""); System.Windows.Application.Current.Shutdown(); })); return true; }
            if (lowerMsg is "calendar" || lowerMsg.Contains("mở lịch") || lowerMsg.Contains("open calendar")) { ShowActionBubble("🗓️ Lịch của cậu đây"); parentPet.OpenDashboard(); return true; }
            if (lowerMsg is "guide" || lowerMsg.Contains("hướng dẫn") || lowerMsg.Contains("help"))
            {
                string guidePath = Path.Combine(AppContext.BaseDirectory, "assets", "index.html");
                if (File.Exists(guidePath)) { Process.Start(new ProcessStartInfo(guidePath) { UseShellExecute = true }); ShowActionBubble("📖 Tớ mở trang hướng dẫn cho cậu rồi nè~"); }
                else ShowActionBubble("❌ Ụa, tớ không tìm thấy file hướng dẫn ở đâu cả!");
                return true;
            }
            if (lowerMsg.Contains("thu nhỏ") || lowerMsg.Contains("minimize") || lowerMsg.Contains("smol")) { ShowActionBubble("🤸‍♀️ Thu nhỏ tớ lại nè!"); Task.Delay(1000).ContinueWith(_ => System.Windows.Application.Current.Dispatcher.Invoke(() => parentPet.ToggleSmolMode())); return true; }

            if (lowerMsg is "big chat" || lowerMsg.Contains("mở cửa sổ to") || lowerMsg.Contains("mở hộp thoại"))
            {
                if (!parentPet.isBigChatOpen)
                {
                    parentPet.isBigChatOpen = true;
                    parentPet.bigChatWindow = new ChatHistoryWindow(parentPet);
                    parentPet.bigChatWindow.Closed += (s, args) => {
                        parentPet.isBigChatOpen = false;
                        parentPet.bigChatWindow = null;
                        parentPet.ChangeState(PetState.Idle);
                    };
                    parentPet.OpenExclusiveWindow(parentPet.bigChatWindow);
                }
                this.Close();
                return true;
            }
            return false;
        }

        private void ShowActionBubble(string text)
        {
            parentPet.ChangeState(PetState.Happy);
            new SpeechBubble(text, parentPet, 3000).Show();
        }
    }
}