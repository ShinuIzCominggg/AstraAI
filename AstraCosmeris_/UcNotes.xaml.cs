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
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public class AstraNote
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = "";
        public bool IsFloating { get; set; } = false;
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;
        public long OrderIndex { get; set; } = DateTime.Now.Ticks;
    }

    public static class NoteManager
    {
        public static List<AstraNote> AllNotes = new();
        public static string FilePath = Path.Combine(AppContext.BaseDirectory, "notes_v2.json");

        public static void LoadNotes()
        {
            if (File.Exists(FilePath))
            {
                try { AllNotes = JsonSerializer.Deserialize<List<AstraNote>>(File.ReadAllText(FilePath)) ?? new(); }
                catch { AllNotes = new(); }
            }
        }

        public static void SaveNotes()
        {
            try { File.WriteAllText(FilePath, JsonSerializer.Serialize(AllNotes, new JsonSerializerOptions { WriteIndented = true })); }
            catch { }
        }
    }

    public partial class UcNotes : System.Windows.Controls.UserControl
    {
        private System.Windows.Point startDragPoint;
        private bool isDragging = false;

        public UcNotes() => InitializeComponent();

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => LoadNotes()), DispatcherPriority.Background);
            }
        }

        public void LoadNotes()
        {
            NotesContainer.Children.Clear();
            foreach (var note in NoteManager.AllNotes.OrderBy(n => n.OrderIndex).ToList())
            {
                if (note.IsFloating)
                {
                    bool isOpened = System.Windows.Application.Current.Windows.OfType<FloatingNoteWindow>().Any(w => w.NoteData.Id == note.Id);
                    if (!isOpened) new FloatingNoteWindow(note).Show();
                }
                else
                {
                    NotesContainer.Children.Add(CreateNoteCard(note));
                }
            }
        }

        private Border CreateNoteCard(AstraNote noteData)
        {
            System.Windows.Controls.TextBox txt = new System.Windows.Controls.TextBox
            {
                Text = noteData.Content,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new System.Windows.Thickness(0),
                FontSize = 18,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                AcceptsReturn = true,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            txt.TextChanged += (s, e) => { noteData.Content = txt.Text; NoteManager.SaveNotes(); };

            System.Windows.Controls.Button btnDel = new System.Windows.Controls.Button
            {
                Content = "🗑️",
                Width = 30,
                Height = 30,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new System.Windows.Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = System.Windows.VerticalAlignment.Top
            };

            btnDel.Click += (s, e) => {
                NoteManager.AllNotes.Remove(noteData);
                NoteManager.SaveNotes();
                LoadNotes();
            };

            Grid grid = new Grid();
            grid.Children.Add(txt);
            grid.Children.Add(btnDel);

            Border card = new Border
            {
                Width = 220,
                Height = 220,
                Margin = new Thickness(15),
                Padding = new Thickness(15),
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF2CC")),
                CornerRadius = new CornerRadius(15),
                Effect = new DropShadowEffect { BlurRadius = 10, Opacity = 0.2, ShadowDepth = 3 },
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = noteData
            };

            card.Child = grid;

            card.PreviewMouseLeftButtonDown += (s, e) => { startDragPoint = e.GetPosition(null); isDragging = true; };
            card.PreviewMouseLeftButtonUp += (s, e) => { isDragging = false; };
            card.PreviewMouseMove += Card_PreviewMouseMove;

            return card;
        }

        private void Card_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isDragging && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && sender is Border card && card.Tag is AstraNote noteData)
            {
                if (noteData.IsFloating) return;

                System.Windows.Point currentPoint = e.GetPosition(null);
                if (Math.Abs(currentPoint.X - startDragPoint.X) > 10 || Math.Abs(currentPoint.Y - startDragPoint.Y) > 10)
                {
                    System.Windows.Point screenPos = card.PointToScreen(new System.Windows.Point(0, 0));
                    noteData.IsFloating = true; noteData.Left = screenPos.X; noteData.Top = screenPos.Y;

                    card.ReleaseMouseCapture();
                    NoteManager.SaveNotes();

                    FloatingNoteWindow fw = new FloatingNoteWindow(noteData);
                    fw.Show();
                    if (Mouse.LeftButton == MouseButtonState.Pressed) fw.DragMove();
                    LoadNotes();
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
    }
}