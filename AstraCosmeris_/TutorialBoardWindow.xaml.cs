using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AstraCosmeris_
{
    public partial class TutorialBoardWindow : Window
    {
        private MainWindow _parent;

        public TutorialBoardWindow(MainWindow parent)
        {
            InitializeComponent();
            _parent = parent;

            TutorialManager.Initialize();
            LvQuests.ItemsSource = TutorialManager.Quests;

            // Neo vị trí bảng Quest cạnh con Astra
            this.Left = _parent.Left + _parent.Width + 10;
            this.Top = _parent.Top;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void BtnHint_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string questId)
            {
                TutorialManager.ShowHint(questId, _parent);
            }
        }

        public void PlayVictoryEffect()
        {
            TxtVictory.Visibility = Visibility.Visible;
            DoubleAnimation bounce = new DoubleAnimation
            {
                From = 0.5,
                To = 1.5,
                Duration = TimeSpan.FromMilliseconds(400),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };
            VictoryScale.BeginAnimation(ScaleTransform.ScaleXProperty, bounce);
            VictoryScale.BeginAnimation(ScaleTransform.ScaleYProperty, bounce);
        }
    }

    // Converters dùng cho UI (có thể nhét vào chung file này)
    public class BoolToCheckConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => (bool)value ? "✅" : "🔲";
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToInvisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => (bool)value ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}