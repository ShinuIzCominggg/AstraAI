using System.Windows;
using System.Windows.Input;

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