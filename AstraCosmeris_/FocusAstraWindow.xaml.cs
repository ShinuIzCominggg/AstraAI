using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AstraCosmeris_
{
    public partial class FocusAstraWindow : Window
    {
        private MainWindow parentMain;
        public PomodoroChecklistWindow? checklist;
        public bool forceClose = false;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Left = SystemParameters.WorkArea.Width - Width;
            Top = (SystemParameters.WorkArea.Height - Height) / 2;
        }

        public FocusAstraWindow(MainWindow main)
        {
            InitializeComponent();
            parentMain = main;
            this.Left = main.Left;
            this.Top = main.Top;

            string state = "working";
            string outfit = DataManager.Data.CurrentOutfit;

            // 👉 SỬA BIỆN PHÁP: Tách biệt nạp Pack URI hoặc Ảnh Disk của tủ đồ
            if (string.IsNullOrEmpty(outfit) || outfit == "Default")
            {
                AstraImage.Source = new BitmapImage(new Uri($"/assets/{state}/{state}.png", UriKind.RelativeOrAbsolute));
            }
            else
            {
                string basePath = AppContext.BaseDirectory;
                string imagePath = Path.Combine(basePath, "assets", state, "wardrobe", outfit, $"{state}_{outfit}.png");

                if (File.Exists(imagePath))
                {
                    AstraImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                }
                else
                {
                    AstraImage.Source = new BitmapImage(new Uri($"/assets/{state}/{state}.png", UriKind.RelativeOrAbsolute));
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e) => MenuChecklist_Click(null, null);

        public void MenuChecklist_Click(object? sender, RoutedEventArgs? e)
        {
            if (checklist == null || !checklist.IsLoaded)
            {
                checklist = new PomodoroChecklistWindow();
            }
            checklist.Show();
            checklist.Activate();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!forceClose) e.Cancel = true;
            base.OnClosing(e);
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            forceClose = true;
            parentMain.isFocusMode = false;
            parentMain.isBigChatOpen = false;
            parentMain.Show();

            checklist?.Close();

            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                if (w is TomatoTimerWindow t)
                {
                    t.forceClose = true;
                    t.Close();
                }
            }
            this.Close();
        }
    }
}