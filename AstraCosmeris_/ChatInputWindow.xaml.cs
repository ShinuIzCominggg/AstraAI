using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;

namespace AstraCosmeris_
{
    public partial class ChatInputWindow : Window
    {
        private MainWindow parentPet;
        private SpeechBubble? thinkingBubble;
        private DashboardWindow? dashboard;

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
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

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

            // --- HỆ THỐNG BẮT KEYWORD ĐỂ LƯU TRÍ NHỚ ---
            string lowerMsg = userText.ToLower();
            if (lowerMsg.Contains("tên tớ là") || lowerMsg.Contains("my name is"))
            {
                string name = userText.Substring(userText.ToLower().IndexOf("là") + 2).Trim();
                MemoryManager.AddFact("User Name", name);
            }
            else if (lowerMsg.Contains("tớ thích") || lowerMsg.Contains("i like"))
            {
                string like = userText.Substring(userText.ToLower().IndexOf("thích") + 5).Trim();
                MemoryManager.AddFact("Likes", like);
            }

            MemoryManager.Data.History.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userText } });
            MemoryManager.SaveMemory();

            parentPet.ChangeState(PetState.Thinking);
            thinkingBubble = new SpeechBubble("💭 Hmm... để tớ nghĩ xíu~", parentPet, 99999);
            thinkingBubble.Show();

            // --- GỌI BỘ NÃO TRUNG TÂM ---
            string reply = await AstraBrain.ThinkAndReply();

            MemoryManager.Data.History.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", reply } });
            MemoryManager.SaveMemory();

            thinkingBubble.CloseBubble(keepState: true);

            parentPet.ChangeState(PetState.Happy);
            SpeechBubble replyBubble = new SpeechBubble(reply, parentPet, 10000);
            replyBubble.Show();
        }

        // --- HỆ THỐNG NHẬN DIỆN LỆNH WINDOWS ---
        private bool HandleSystemCommand(string msg)
        {
            string lowerMsg = msg.ToLower().Trim();

            if (lowerMsg.Contains("mở edge") || (lowerMsg.Contains("open") && lowerMsg.Contains("edge")))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("msedge") { UseShellExecute = true });
                ShowActionBubble("🌐 Đang mở Edge cho cậu lướt web nè~");
                return true;
            }

            if (lowerMsg.Contains("mở word") || (lowerMsg.Contains("open") && lowerMsg.Contains("word")))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("winword") { UseShellExecute = true });
                ShowActionBubble("📄 Đã mở Word! Chúc cậu làm việc vui vẻ nha");
                return true;
            }

            if (lowerMsg.Contains("mở notepad") || lowerMsg.Contains("open notepad"))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad") { UseShellExecute = true });
                ShowActionBubble("📓 Notepad lên sóng!");
                return true;
            }

            if (lowerMsg == "quit" || lowerMsg == "bye" || lowerMsg == "exit" || lowerMsg.Contains("cút") || lowerMsg == "tắt")
            {
                ShowActionBubble("👋 Tạm biệt cậu nha");
                System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ => System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown()));
                return true;
            }

            if (lowerMsg == "restart" || lowerMsg.Contains("khởi động lại"))
            {
                ShowActionBubble("🔄 Tớ sẽ trở lại ngay~");
                System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ => System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");
                    System.Windows.Application.Current.Shutdown();
                }));
                return true;
            }

            if (lowerMsg == "calendar" || lowerMsg.Contains("mở lịch") || lowerMsg.Contains("open calendar"))
            {
                ShowActionBubble("🗓️ Lịch của cậu đây");
                parentPet.OpenDashboard(); // Gọi sang não mẹ, cấm tự đẻ Dashboard riêng
                return true;
            }

            if (lowerMsg == "big chat" || lowerMsg.Contains("mở cửa sổ to") || lowerMsg.Contains("mở hộp thoại"))
            {
                if (!parentPet.isBigChatOpen)
                {
                    parentPet.isBigChatOpen = true;
                    var bigChat = new ChatHistoryWindow(parentPet);

                    bigChat.Closed += (s, args) => {
                        parentPet.isBigChatOpen = false;
                        parentPet.ChangeState(PetState.Idle);
                    };
                    bigChat.Show();
                }

                this.Close();
                return true;
            }

            if(lowerMsg == "guide" || lowerMsg.Contains("hướng dẫn") || lowerMsg.Contains("help"))
            {
                string guidePath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "assets", "index.html");

                if (System.IO.File.Exists(guidePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(guidePath) { UseShellExecute = true });
                    ShowActionBubble("📖 Tớ mở trang hướng dẫn cho cậu rồi nè~");
                }
                else
                {
                    ShowActionBubble("❌ Ụa, tớ không tìm thấy file hướng dẫn ở đâu cả!");
                }

                return true;
            }
            return false;
        }

        private void ShowActionBubble(string text)
        {
            parentPet.ChangeState(PetState.Happy);
            SpeechBubble bubble = new SpeechBubble(text, parentPet, 3000);
            bubble.Show();
        }
    }
}