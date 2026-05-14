using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AstraCosmeris_;

namespace AstraCosmeris_
{
    public partial class FloatingNoteWindow : Window
    {
        public AstraNote NoteData;

        // Khai báo hiệu ứng lắc lư (Wiggle)
        private DoubleAnimation wiggleAnim = new DoubleAnimation(-2, 2, TimeSpan.FromMilliseconds(100)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
        private RotateTransform wiggleTransform = new RotateTransform();

        private bool _isDragging = false;

        public FloatingNoteWindow(AstraNote data)
        {
            InitializeComponent();
            NoteData = data;
            TxtContent.Text = data.Content;

            this.Left = data.Left;
            this.Top = data.Top;

            // Setup tâm xoay cho hiệu ứng lắc lư
            this.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            this.RenderTransform = wiggleTransform;

            // FIX 2: VẼ VÙNG DÁN (HOVER EFFECT GẦN GIỐNG WEB UPLOAD)
            this.LocationChanged += FloatingNoteWindow_LocationChanged;
        }

        private void TxtContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            NoteData.Content = TxtContent.Text;
            NoteManager.SaveNotes();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                StartDragging();
            }
        }

        // HÀM XỬ LÝ KÉO THẢ CHUNG (Bọc thép)
        private void StartDragging()
        {
            _isDragging = true;
            // Bật hiệu ứng lắc lư và làm mờ tờ giấy 1 xíu
            this.Opacity = 0.85;
            wiggleTransform.BeginAnimation(RotateTransform.AngleProperty, wiggleAnim);

            // Chặn code lại ở đây chờ user kéo chuột xong...
            this.DragMove();

            _isDragging = false;

            // Kéo xong thả chuột ra -> Tắt hiệu ứng
            wiggleTransform.BeginAnimation(RotateTransform.AngleProperty, null);
            this.Opacity = 1.0;

            // Gọi logic xử lý sau khi thả
            HandleDropLogic();
        }

        private void FloatingNoteWindow_LocationChanged(object sender, EventArgs e)
        {
            if (!_isDragging) return;

            var dash = System.Windows.Application.Current.Windows.OfType<DashboardWindow>().FirstOrDefault();
            if (dash != null && dash.IsVisible)
            {
                var contentControl = dash.FindName("MainContent") as ContentControl;
                if (contentControl?.Content is UcNotes ucNotes)
                {
                    // Lấy thẳng khung Scroll thay vì WrapPanel để vùng cắm chuẩn xác 100%
                    var notesScroll = ucNotes.FindName("NotesScroll") as ScrollViewer;
                    var dropZone = ucNotes.FindName("DropZoneOverlay") as Grid;

                    if (notesScroll != null && dropZone != null)
                    {
                        // KHÔNG CỘNG THÊM PIXEL NỮA - Kích thước chuẩn đét!
                        Rect myRect = new Rect(this.Left, this.Top, this.ActualWidth, this.ActualHeight);
                        System.Windows.Point scrollScreenPos = notesScroll.PointToScreen(new System.Windows.Point(0, 0));
                        Rect scrollRect = new Rect(scrollScreenPos.X, scrollScreenPos.Y, notesScroll.ActualWidth, notesScroll.ActualHeight);

                        // Chạm đúng vùng thì hiện viền đứt nét lên
                        if (myRect.IntersectsWith(scrollRect))
                            dropZone.Visibility = Visibility.Visible;
                        else
                            dropZone.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void HandleDropLogic()
        {
            var dash = System.Windows.Application.Current.Windows.OfType<DashboardWindow>().FirstOrDefault();
            if (dash != null && dash.IsVisible)
            {
                var contentControl = dash.FindName("MainContent") as ContentControl;
                if (contentControl?.Content is UcNotes ucNotes)
                {
                    var notesScroll = ucNotes.FindName("NotesScroll") as ScrollViewer;
                    var dropZone = ucNotes.FindName("DropZoneOverlay") as Grid;

                    if (notesScroll != null && dropZone != null)
                    {
                        // LỆNH TỐI THƯỢNG: NHẢ CHUỘT PHÁT LÀ PHẢI TẮT BẢNG DROP ZONE NGAY VÀ LUÔN!
                        dropZone.Visibility = Visibility.Collapsed;

                        Rect myRect = new Rect(this.Left, this.Top, this.ActualWidth, this.ActualHeight);
                        System.Windows.Point scrollScreenPos = notesScroll.PointToScreen(new System.Windows.Point(0, 0));
                        Rect scrollRect = new Rect(scrollScreenPos.X, scrollScreenPos.Y, notesScroll.ActualWidth, notesScroll.ActualHeight);

                        // HÚT VÀO BẢNG CHÍNH
                        if (myRect.IntersectsWith(scrollRect))
                        {
                            NoteData.IsFloating = false;
                            NoteData.OrderIndex = DateTime.Now.Ticks;
                            NoteManager.SaveNotes();
                            ucNotes.LoadNotes();
                            this.Close();
                            return;
                        }
                    }
                }
            }

            // Nhả ngoài Desktop thì sống kiếp trôi dạt
            NoteData.Left = this.Left;
            NoteData.Top = this.Top;
            NoteManager.SaveNotes();
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            // FIX LỖI 1: Dọn dẹp Drop Zone ngay lập tức nếu đang lơ lửng
            var dash = System.Windows.Application.Current.Windows.OfType<DashboardWindow>().FirstOrDefault();
            if (dash != null && dash.IsVisible)
            {
                var contentControl = dash.FindName("MainContent") as ContentControl;
                if (contentControl?.Content is UcNotes ucNotes)
                {
                    var dropZone = ucNotes.FindName("DropZoneOverlay") as Grid;
                    if (dropZone != null) dropZone.Visibility = Visibility.Collapsed;
                }
            }

            NoteManager.AllNotes.Remove(NoteData);
            NoteManager.SaveNotes();
            this.Close();
        }
        protected override void OnClosed(EventArgs e)
        {
            // BẢO KÊ: Dù bị tắt bằng Alt+F4 hay Taskbar thì cũng dọn dẹp sạch sẽ
            var dash = System.Windows.Application.Current.Windows.OfType<DashboardWindow>().FirstOrDefault();
            if (dash != null)
            {
                var contentControl = dash.FindName("MainContent") as ContentControl;
                if (contentControl?.Content is UcNotes ucNotes)
                {
                    var dropZone = ucNotes.FindName("DropZoneOverlay") as Grid;
                    if (dropZone != null) dropZone.Visibility = Visibility.Collapsed;
                }
            }
            base.OnClosed(e);
        }
    }
}