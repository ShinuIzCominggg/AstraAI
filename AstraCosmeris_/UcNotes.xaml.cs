using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AstraCosmeris_
{
    public class AstraNote
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = "";
        public bool IsFloating { get; set; } = false;
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;

        // THÊM DÒNG NÀY: Dùng Ticks để lưu thời gian chính xác đến từng mili-giây
        public long OrderIndex { get; set; } = DateTime.Now.Ticks;
    }

    public static class NoteManager
    {
        public static List<AstraNote> AllNotes = new List<AstraNote>();
        public static string FilePath = Path.Combine(AppContext.BaseDirectory, "notes_v2.json");

        public static void LoadNotes()
        {
            if (File.Exists(FilePath))
            {
                try { AllNotes = JsonSerializer.Deserialize<List<AstraNote>>(File.ReadAllText(FilePath)) ?? new List<AstraNote>(); }
                catch { AllNotes = new List<AstraNote>(); }
            }
        }
        public static void SaveNotes() => File.WriteAllText(FilePath, JsonSerializer.Serialize(AllNotes, new JsonSerializerOptions { WriteIndented = true }));
    }

    public partial class UcNotes : System.Windows.Controls.UserControl
    {
        private System.Windows.Point startDragPoint; // <-- Đã bọc thép

        public UcNotes()
        {
            InitializeComponent();
            NoteManager.LoadNotes();
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                // FIX CRASH: Đẩy lệnh LoadNotes ra hàng đợi (chờ WPF vẽ xong tab Notes mới được đẻ cửa sổ Note lơ lửng)
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => LoadNotes()), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        public void LoadNotes()
        {
            NotesContainer.Children.Clear();

            // Sắp xếp note theo thời gian tạo
            foreach (var note in NoteManager.AllNotes.OrderBy(n => n.OrderIndex).ToList())
            {
                if (note.IsFloating)
                {
                    bool isOpened = false;

                    // FIX CRASH 2: Ép danh sách Window hiện tại thành List() tĩnh để vòng lặp không bị lỗi khi có cửa sổ mới đẻ ra
                    var openWindows = System.Windows.Application.Current.Windows.Cast<Window>().ToList();
                    foreach (Window w in openWindows)
                    {
                        if (w is FloatingNoteWindow fw && fw.NoteData.Id == note.Id)
                        {
                            isOpened = true;
                            break; // Thấy rồi thì ngắt vòng lặp luôn cho tối ưu
                        }
                    }

                    if (!isOpened) new FloatingNoteWindow(note).Show();
                }
                else
                {
                    NotesContainer.Children.Add(CreateNoteCard(note));
                }
            }
        }

        private void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            var newNote = new AstraNote { Content = "Ghi chú mới..." };
            NoteManager.AllNotes.Add(newNote);
            NoteManager.SaveNotes();
            LoadNotes();
        }

        private Border CreateNoteCard(AstraNote note)
        {
            Border card = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF9B1")),
                CornerRadius = new CornerRadius(15),
                Width = 220,
                MinHeight = 200,
                Margin = new Thickness(0, 0, 25, 25),
                Padding = new Thickness(20),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = note, // <-- Đã bọc thép Cursors
                Effect = new DropShadowEffect { Color = System.Windows.Media.Colors.Gray, Direction = 315, ShadowDepth = 3, BlurRadius = 10, Opacity = 0.2 }
            };
            card.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            RotateTransform wiggleTransform = new RotateTransform(0);
            card.RenderTransform = wiggleTransform;

            card.PreviewMouseLeftButtonDown += (s, e) => {
                startDragPoint = e.GetPosition(null);

                // BẬT ANIMATION LẮC LƯ KHI GIỮ CHUỘT
                var wiggleAnim = new System.Windows.Media.Animation.DoubleAnimation(-2, 2, new Duration(TimeSpan.FromMilliseconds(100)))
                {
                    AutoReverse = true,
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                };
                wiggleTransform.BeginAnimation(RotateTransform.AngleProperty, wiggleAnim);
            };

            // TẮT LẮC LƯ KHI NHẢ CHUỘT HOẶC KÉO RA KHỎI CARD
            card.PreviewMouseUp += (s, e) => wiggleTransform.BeginAnimation(RotateTransform.AngleProperty, null);
            card.MouseLeave += (s, e) => wiggleTransform.BeginAnimation(RotateTransform.AngleProperty, null);

            card.PreviewMouseMove += Card_PreviewMouseMove;

            Grid innerGrid = new Grid();
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            System.Windows.Controls.TextBox txt = new System.Windows.Controls.TextBox
            {
                Text = note.Content,
                FontSize = 20,
                Foreground = System.Windows.Media.Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent
            };
            txt.TextChanged += (s, e) => { note.Content = txt.Text; NoteManager.SaveNotes(); };
            Grid.SetRow(txt, 0);

            System.Windows.Controls.Button btnDel = new System.Windows.Controls.Button
            {
                Content = "Xóa",
                Foreground = System.Windows.Media.Brushes.Red,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Cursor = System.Windows.Input.Cursors.Hand // <-- Đã bọc thép HorizontalAlignment và Cursors
            };
            btnDel.Click += (s, e) => { NoteManager.AllNotes.Remove(note); NoteManager.SaveNotes(); LoadNotes(); };
            Grid.SetRow(btnDel, 1);

            innerGrid.Children.Add(txt); innerGrid.Children.Add(btnDel);
            card.Child = innerGrid;
            return card;
        }

        // <-- Đã bọc thép MouseEventArgs
        private void Card_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Border card = sender as Border;
                AstraNote noteData = card.Tag as AstraNote;

                // 1. Nếu Note đã "bay" rồi thì phớt lờ luôn!
                if (noteData.IsFloating) return;

                System.Windows.Point currentPoint = e.GetPosition(null);
                if (Math.Abs(currentPoint.X - startDragPoint.X) > 10 || Math.Abs(currentPoint.Y - startDragPoint.Y) > 10)
                {
                    // 2. Lấy tọa độ CHUẨN XÁC 100% của tờ giấy trên màn hình (Không trừ hao nữa)
                    System.Windows.Point screenPos = card.PointToScreen(new System.Windows.Point(0, 0));

                    noteData.IsFloating = true;
                    noteData.Left = screenPos.X;
                    noteData.Top = screenPos.Y;

                    // 3. Ép Dashboard nhả chuột ra NGAY LẬP TỨC
                    card.ReleaseMouseCapture();
                    NoteManager.SaveNotes();

                    // 4. Đẻ cửa sổ bay ra trước
                    FloatingNoteWindow fw = new FloatingNoteWindow(noteData);
                    fw.Show();

                    // 5. BÍ THUẬT: Bắt cửa sổ mới CƯỚP lại con chuột đang giữ để kéo mượt 1 mạch
                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                    {
                        fw.DragMove();
                    }

                    // 6. Mới gọi LoadNotes để dọn dẹp Dashboard
                    LoadNotes();
                }
            }
        }
    }
}