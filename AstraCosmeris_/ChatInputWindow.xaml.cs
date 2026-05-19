using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public class PaletteItem
    {
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ActionCommand { get; set; } = "";
        public string MetaData { get; set; } = ""; // Dùng để lưu đường dẫn file, URL hoặc thông số phụ
    }

    public partial class ChatInputWindow : Window
    {
        private MainWindow parentPet;
        private SpeechBubble? thinkingBubble;
        private static List<string> _history = new List<string>();
        private int _historyIndex = -1;
        private List<PaletteItem> _systemCommands = new List<PaletteItem>();

        // --- WIN API CHO SYSTEM CONTROLS ---
        [DllImport("user32.dll")]
        public static extern void LockWorkStation();

        [DllImport("powrprof.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        const int VK_VOLUME_MUTE = 0xAD;

        public ChatInputWindow(MainWindow parent)
        {
            InitializeComponent();
            parentPet = parent;
            InitSystemCommands();

            this.Loaded += (s, e) => {
                InputBox.Focus();
            };
        }

        private void InitSystemCommands()
        {
            _systemCommands = new List<PaletteItem>
            {
                new PaletteItem { Icon = "🔒", Title = "/lock", Description = "Khóa màn hình ngay lập tức", ActionCommand = "sys_lock" },
                new PaletteItem { Icon = "🌙", Title = "/sleep", Description = "Đưa máy vào chế độ ngủ (Sleep)", ActionCommand = "sys_sleep" },
                new PaletteItem { Icon = "🔇", Title = "/mute", Description = "Tắt/Bật âm thanh hệ thống", ActionCommand = "sys_mute" },
                new PaletteItem { Icon = "🛑", Title = "/shutdown", Description = "Tắt máy tính", ActionCommand = "sys_shutdown" },
                new PaletteItem { Icon = "🔄", Title = "/reboot", Description = "Khởi động lại máy tính", ActionCommand = "sys_reboot" },
                new PaletteItem { Icon = "⏱️", Title = "/timer", Description = "Hẹn giờ Pomodoro nhanh (VD: /timer 15)", ActionCommand = "sys_timer" },
                new PaletteItem { Icon = "🔍", Title = "/find", Description = "Tìm kiếm file nhanh trong máy", ActionCommand = "sys_find" },
                new PaletteItem { Icon = "🌐", Title = "/edge", Description = "Mở Microsoft Edge", ActionCommand = "edge" },
                new PaletteItem { Icon = "📄", Title = "/word", Description = "Mở Microsoft Word", ActionCommand = "word" },
                new PaletteItem { Icon = "🗓️", Title = "/calendar", Description = "Mở Lịch trình", ActionCommand = "calendar" },
                new PaletteItem { Icon = "⚙️", Title = "/settings", Description = "Mở Cài đặt", ActionCommand = "settings" },
                new PaletteItem { Icon = "💬", Title = "/bigchat", Description = "Mở Chat Lịch sử", ActionCommand = "bigchat" },
                new PaletteItem { Icon = "👋", Title = "/exit", Description = "Đóng Astra", ActionCommand = "exit" }
            };
        }

        private async void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = InputBox.Text;
            TxtPlaceholder.Visibility = string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(text))
            {
                LstSuggestions.Visibility = Visibility.Collapsed;
                return;
            }

            // 1. TÍNH TOÁN MATH TRỰC TIẾP (Calculator)
            if (Regex.IsMatch(text, @"^[\d\+\-\*\/\(\)\.\s]+$") && text.Any(char.IsDigit) && text.Any(c => "+-*/".Contains(c)))
            {
                try
                {
                    var result = new DataTable().Compute(text, null);
                    ShowQuickList(new List<PaletteItem> { new PaletteItem { Icon = "🧮", Title = result.ToString() ?? "", Description = "Kết quả phép tính (Enter để chép)", ActionCommand = "copy_clipboard", MetaData = result.ToString() ?? "" } });
                    return;
                }
                catch { }
            }

            // 2. LỆNH HỆ THỐNG CƠ BẢN (/)
            if (text.StartsWith("/"))
            {
                if (text.StartsWith("/find "))
                {
                    string query = text.Substring(6).Trim();
                    if (query.Length > 2) await LiveSearchFilesAsync(query);
                    return;
                }

                if (text.StartsWith("/timer "))
                {
                    string mins = text.Substring(7).Trim();
                    ShowQuickList(new List<PaletteItem> { new PaletteItem { Icon = "⏱️", Title = $"Bật đồng hồ {mins} phút", Description = "Enter để đếm ngược ngay", ActionCommand = "start_timer", MetaData = mins } });
                    return;
                }

                var filtered = _systemCommands.Where(c => c.Title.ToLower().Contains(text.ToLower())).ToList();
                ShowQuickList(filtered);
            }
            // 3. QUICK ADD (+, @)
            else if (text.StartsWith("+") || text.StartsWith("@"))
            {
                ShowQuickList(new List<PaletteItem>
                {
                    new PaletteItem { Icon = "✅", Title = "+task [nội dung]", Description = "Thêm nhanh công việc", ActionCommand = "quick_task" },
                    new PaletteItem { Icon = "🎉", Title = "+event [tên sự kiện]", Description = "Tạo nhanh sự kiện", ActionCommand = "quick_event" }
                });
            }
            // 4. WEB SEARCH HOẶC MỞ URL (?, >)
            else if (text.StartsWith("?"))
            {
                string query = text.Substring(1).Trim();
                ShowQuickList(new List<PaletteItem> { new PaletteItem { Icon = "🌍", Title = $"Tìm Google: {query}", Description = "Mở trình duyệt tìm kiếm", ActionCommand = "web_search", MetaData = query } });
            }
            else if (text.StartsWith(">"))
            {
                string url = text.Substring(1).Trim();
                ShowQuickList(new List<PaletteItem> { new PaletteItem { Icon = "🔗", Title = $"Truy cập: {url}", Description = "Mở đường dẫn này", ActionCommand = "open_url", MetaData = url } });
            }
            else
            {
                LstSuggestions.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowQuickList(List<PaletteItem> items)
        {
            if (items.Any())
            {
                LstSuggestions.ItemsSource = items;
                LstSuggestions.Visibility = Visibility.Visible;
                if (LstSuggestions.SelectedIndex == -1) LstSuggestions.SelectedIndex = 0;
            }
            else LstSuggestions.Visibility = Visibility.Collapsed;
        }

        private async Task LiveSearchFilesAsync(string query)
        {
            try
            {
                var results = await Task.Run(() =>
                {
                    var found = new List<PaletteItem>();
                    string[] searchPaths = {
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                    };

                    foreach (var path in searchPaths)
                    {
                        if (!Directory.Exists(path)) continue;
                        var files = Directory.GetFiles(path, $"*{query}*", SearchOption.AllDirectories).Take(5);
                        foreach (var f in files)
                        {
                            found.Add(new PaletteItem { Icon = "📄", Title = Path.GetFileName(f), Description = f, ActionCommand = "open_file", MetaData = f });
                        }
                    }
                    return found;
                });

                if (results.Any()) ShowQuickList(results);
                else ShowQuickList(new List<PaletteItem> { new PaletteItem { Icon = "❌", Title = "Không tìm thấy", Description = "Không có file nào khớp trên Desktop/Doc/Downloads" } });
            }
            catch { }
        }

        private void InputBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (LstSuggestions.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Down) { e.Handled = true; if (LstSuggestions.SelectedIndex < LstSuggestions.Items.Count - 1) LstSuggestions.SelectedIndex++; return; }
                if (e.Key == Key.Up) { e.Handled = true; if (LstSuggestions.SelectedIndex > 0) LstSuggestions.SelectedIndex--; return; }
            }
            else
            {
                if (e.Key == Key.Up && _history.Any()) { e.Handled = true; if (_historyIndex == -1) _historyIndex = _history.Count - 1; else if (_historyIndex > 0) _historyIndex--; InputBox.Text = _history[_historyIndex]; InputBox.CaretIndex = InputBox.Text.Length; return; }
                if (e.Key == Key.Down && _history.Any()) { e.Handled = true; if (_historyIndex >= 0 && _historyIndex < _history.Count - 1) { _historyIndex++; InputBox.Text = _history[_historyIndex]; } else { _historyIndex = -1; InputBox.Text = ""; } InputBox.CaretIndex = InputBox.Text.Length; return; }
            }

            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                e.Handled = true;
                ExecuteCurrentInput();
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Escape) this.Close(); }
        private void LstSuggestions_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => ExecuteCurrentInput();

        private void ExecuteCurrentInput()
        {
            if (LstSuggestions.Visibility == Visibility.Visible && LstSuggestions.SelectedItem is PaletteItem selectedItem)
            {
                string cmd = selectedItem.ActionCommand;
                if (cmd == "quick_task" || cmd == "quick_event" || cmd == "sys_find" || cmd == "sys_timer")
                {
                    InputBox.Text = selectedItem.Title.Split('[')[0].Trim() + " ";
                    InputBox.CaretIndex = InputBox.Text.Length;
                    return;
                }

                HandleSystemAction(cmd, selectedItem.MetaData);
                this.Close();
                return;
            }

            string rawText = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(rawText)) return;

            if (!_history.Contains(rawText)) _history.Add(rawText);
            _historyIndex = -1;
            InputBox.Text = "";

            if (rawText.StartsWith("+task") || rawText.StartsWith("@task"))
            {
                string content = rawText.Substring(rawText.IndexOf("task") + 4).Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    string todayKey = DateTime.Now.ToString("yyyy-MM-dd");
                    if (DataManager.Data.Tasks.TryGetValue(todayKey, out string? existingTasks)) DataManager.Data.Tasks[todayKey] = existingTasks + Environment.NewLine + content;
                    else DataManager.Data.Tasks[todayKey] = content;
                    DataManager.SaveData();
                    ShowActionBubble($"✅ Thêm nhanh Task thành công!");
                }
                this.Close(); return;
            }

            if (rawText.StartsWith("+event") || rawText.StartsWith("@event"))
            {
                string title = rawText.Substring(rawText.IndexOf("event") + 5).Trim();
                if (!string.IsNullOrEmpty(title))
                {
                    DataManager.Data.Events.Add(new AstraEvent { Title = title, Date = DateTime.Now, Type = "Khác", Location = "", Repeat = "Không lặp" });
                    DataManager.SaveData();
                    ShowActionBubble($"🎉 Đã lên lịch: {title}");
                }
                this.Close(); return;
            }

            ProcessAIChat(rawText);
        }

        private void HandleSystemAction(string action, string metaData)
        {
            switch (action)
            {
                case "sys_lock": LockWorkStation(); break;
                case "sys_sleep": SetSuspendState(false, true, true); break;
                case "sys_mute": keybd_event(VK_VOLUME_MUTE, 0, 0, 0); ShowActionBubble("🔇 Đã chỉnh âm thanh!"); break;
                case "sys_shutdown": Process.Start("shutdown", "/s /t 0"); break;
                case "sys_reboot": Process.Start("shutdown", "/r /t 0"); break;

                case "start_timer":
                    if (int.TryParse(metaData, out int mins)) { new TomatoTimerWindow(mins, 0, null, parentPet).Show(); ShowActionBubble($"⏱️ Đã đặt {mins} phút!"); }
                    break;
                case "open_file": Process.Start(new ProcessStartInfo(metaData) { UseShellExecute = true }); break;
                case "copy_clipboard": System.Windows.Clipboard.SetText(metaData); ShowActionBubble("📋 Đã copy kết quả!"); break;

                case "web_search": Process.Start(new ProcessStartInfo($"https://www.google.com/search?q={Uri.EscapeDataString(metaData)}") { UseShellExecute = true }); break;
                case "open_url":
                    if (!metaData.StartsWith("http")) metaData = "https://" + metaData;
                    Process.Start(new ProcessStartInfo(metaData) { UseShellExecute = true });
                    break;

                case "edge": Process.Start(new ProcessStartInfo("msedge") { UseShellExecute = true }); break;
                case "word": Process.Start(new ProcessStartInfo("winword") { UseShellExecute = true }); break;
                case "calendar": parentPet.OpenDashboard(); break;
                case "settings": parentPet.OpenExclusiveWindow(new SettingsWindow()); break;
                case "bigchat":
                    if (!parentPet.isBigChatOpen)
                    {
                        parentPet.isBigChatOpen = true;
                        parentPet.bigChatWindow = new ChatHistoryWindow(parentPet);
                        parentPet.bigChatWindow.Closed += (s, args) => { parentPet.isBigChatOpen = false; parentPet.bigChatWindow = null; parentPet.ChangeState(PetState.Idle); };
                        parentPet.OpenExclusiveWindow(parentPet.bigChatWindow);
                    }
                    break;
                case "exit": Task.Delay(500).ContinueWith(_ => System.Windows.Application.Current.Dispatcher.Invoke(() => parentPet.Close())); break;
            }
        }

        private async void ProcessAIChat(string userText)
        {
            string lowerMsg = userText.ToLower();
            if (lowerMsg.Contains("tên tớ là") || lowerMsg.Contains("my name is")) DataManager.AddFact("Tên", userText.Substring(lowerMsg.IndexOf("là") + 2).Trim());

            DataManager.Data.History.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userText } });
            DataManager.SaveData();

            parentPet.ChangeState(PetState.Thinking);
            thinkingBubble = new SpeechBubble("💭 Hmm... để tớ nghĩ xíu~", parentPet, 99999);
            thinkingBubble.Show();
            this.Hide();

            string reply = await AstraBrain.ThinkAndReply();

            DataManager.Data.History.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", reply } });
            DataManager.SaveData();

            thinkingBubble.CloseBubble(keepState: true);
            parentPet.ChangeState(PetState.Happy);
            new SpeechBubble(reply, parentPet, 10000).Show();
            this.Close();
        }

        private void ShowActionBubble(string text)
        {
            parentPet.ChangeState(PetState.Happy);
            new SpeechBubble(text, parentPet, 3000).Show();
        }
    }
}