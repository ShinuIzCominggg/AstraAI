using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AstraCosmeris_
{
    public partial class UcCanvas : System.Windows.Controls.UserControl
    {
        private DashboardWindow parentDash;

        public UcCanvas(DashboardWindow parent)
        {
            InitializeComponent();
            parentDash = parent;

            // Mặc định bút đen, nét mượt
            DrawingAttributes inkDA = new DrawingAttributes();
            inkDA.Color = Colors.Black;
            inkDA.Width = 3;
            inkDA.Height = 3;
            inkDA.FitToCurve = true;
            DrawCanvas.DefaultDrawingAttributes = inkDA;
        }

        private void BtnPen_Click(object sender, RoutedEventArgs e) => DrawCanvas.EditingMode = InkCanvasEditingMode.Ink;
        private void BtnEraser_Click(object sender, RoutedEventArgs e) => DrawCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;

        private void BtnColor_Click(object sender, RoutedEventArgs e)
        {
            DrawCanvas.EditingMode = InkCanvasEditingMode.Ink;
            if (sender is System.Windows.Controls.Button btn && btn.Background is SolidColorBrush brush)
            {
                DrawCanvas.DefaultDrawingAttributes.Color = brush.Color;
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            BtnSettings.ContextMenu.IsOpen = true;
        }

        private void MenuToggleSidebar_Click(object sender, RoutedEventArgs e) => parentDash.ToggleSidebar();
        private void MenuFullScreen_Click(object sender, RoutedEventArgs e) => parentDash.ToggleFullScreen();
        private void MenuClear_Click(object sender, RoutedEventArgs e) => DrawCanvas.Strokes.Clear();

        private void MenuExport_Click(object sender, RoutedEventArgs e)
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(DrawCanvas);
            if (bounds.IsEmpty) { System.Windows.MessageBox.Show("Bảng trống mà cậu!"); return; }

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)DrawCanvas.ActualWidth, (int)DrawCanvas.ActualHeight, 96d, 96d, PixelFormats.Default);
            rtb.Render(DrawCanvas);

            BitmapEncoder pbEncoder = new PngBitmapEncoder();
            pbEncoder.Frames.Add(BitmapFrame.Create(rtb));

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog { DefaultExt = ".png", Filter = "PNG Image|*.png", FileName = "AstraCanvas.png" };
            if (dlg.ShowDialog() == true)
            {
                using (var fs = File.OpenWrite(dlg.FileName)) pbEncoder.Save(fs);
                System.Windows.MessageBox.Show("Đã xuất ảnh thành công!");
            }
        }
    }
}