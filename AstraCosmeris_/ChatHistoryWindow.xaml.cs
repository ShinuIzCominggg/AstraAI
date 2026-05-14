using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace AstraCosmeris_
{
    public partial class ChatHistoryWindow : Window
    {
        private MainWindow parentPet;

        public ChatHistoryWindow(MainWindow parent)
        {
            InitializeComponent();
            parentPet = parent;

            if (MemoryManager.Data != null)
            {
                ChatList.ItemsSource = MemoryManager.Data.History;
            }

            this.Loaded += (s, e) => ChatScroll.ScrollToEnd();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            parentPet.isBigChatOpen = false;
            this.Close();
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

            MemoryManager.Data.History.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userText } });
            ChatScroll.ScrollToEnd();

            parentPet.ChangeState(PetState.Thinking);

            // --- GỌI BỘ NÃO TRUNG TÂM ---
            string reply = await AstraBrain.ThinkAndReply();

            MemoryManager.Data.History.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", reply } });
            MemoryManager.SaveMemory();

            ChatScroll.ScrollToEnd();
            parentPet.ChangeState(PetState.Happy);
        }
    }
}