using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public partial class ChatHistoryWindow : Window
    {
        private MainWindow parentPet;

        public ChatHistoryWindow(MainWindow parent)
        {
            InitializeComponent();
            parentPet = parent;

            if (DataManager.Data != null)
                ChatList.ItemsSource = DataManager.Data.History;

            this.Loaded += (s, e) => ChatScroll.ScrollToEnd();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            parentPet.isBigChatOpen = false;
            parentPet.CloseExclusiveWindow();
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

            DataManager.Data.History.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userText } });
            ChatScroll.ScrollToEnd();

            parentPet.ChangeState(PetState.Thinking);

            // --- GỌI BỘ NÃO TRUNG TÂM ---
            string reply = await AstraBrain.ThinkAndReply();

            DataManager.Data.History.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", reply } });
            DataManager.SaveData();

            ChatScroll.ScrollToEnd();
            parentPet.ChangeState(PetState.Happy);
        }
    }
}