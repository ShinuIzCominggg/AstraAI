using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AstraCosmeris_
{
    public partial class UcTasks : System.Windows.Controls.UserControl
    {
        private string tasksFilePath = Path.Combine(AppContext.BaseDirectory, "tasks.json");

        private Dictionary<DayOfWeek, string> dayColors = new Dictionary<DayOfWeek, string> {
            { DayOfWeek.Monday, "#FF6B6B" }, { DayOfWeek.Tuesday, "#FF9F43" },
            { DayOfWeek.Wednesday, "#FECA57" }, { DayOfWeek.Thursday, "#1DD1A1" },
            { DayOfWeek.Friday, "#54A0FF" }, { DayOfWeek.Saturday, "#5F27CD" }, { DayOfWeek.Sunday, "#FF9FF3" }
        };

        public UcTasks()
        {
            InitializeComponent();
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true) LoadTasksData();
        }

        private void LoadTasksData()
        {
            PanelUpcoming.Children.Clear();
            PanelUrgent.Children.Clear();

            if (!File.Exists(tasksFilePath)) return;

            Dictionary<string, string> allTasks;
            try { allTasks = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(tasksFilePath)) ?? new Dictionary<string, string>(); }
            catch { return; }

            DateTime today = DateTime.Today;
            var validTasks = new List<Tuple<DateTime, string>>();

            foreach (var item in allTasks)
            {
                if (DateTime.TryParseExact(item.Key, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                {
                    if (date >= today && !string.IsNullOrWhiteSpace(item.Value))
                        validTasks.Add(new Tuple<DateTime, string>(date, item.Value));
                }
            }

            validTasks = validTasks.OrderBy(t => t.Item1).ToList();

            foreach (var task in validTasks)
            {
                int daysLeft = (int)(task.Item1 - today).TotalDays;
                if (daysLeft <= 2) PanelUrgent.Children.Add(CreateTaskCard(task.Item1, task.Item2));
                else if (daysLeft <= 7) PanelUpcoming.Children.Add(CreateTaskCard(task.Item1, task.Item2));
            }
        }

        private Border CreateTaskCard(DateTime date, string content)
        {
            string headerColorHex = dayColors.ContainsKey(date.DayOfWeek) ? dayColors[date.DayOfWeek] : "#9FB2D4";
            var headerBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(headerColorHex));

            Border outerContainer = new Border
            {
                CornerRadius = new CornerRadius(15),
                Margin = new Thickness(0, 0, 0, 25),
                Background = System.Windows.Media.Brushes.Transparent,
                Effect = new DropShadowEffect { Color = System.Windows.Media.Colors.Gray, Direction = 315, ShadowDepth = 3, BlurRadius = 10, Opacity = 0.3 }
            };

            Grid cardGrid = new Grid();
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Border headerBorder = new Border { Background = headerBrush, CornerRadius = new CornerRadius(15, 15, 0, 0), Padding = new Thickness(20, 12, 20, 12) };
            TextBlock dateText = new TextBlock { Text = date.ToString("dddd, dd/MM/yyyy"), Foreground = System.Windows.Media.Brushes.White, FontSize = 20, FontWeight = FontWeights.Bold };
            headerBorder.Child = dateText;
            Grid.SetRow(headerBorder, 0);

            Border bodyBorder = new Border { Background = System.Windows.Media.Brushes.White, CornerRadius = new CornerRadius(0, 0, 15, 15), Padding = new Thickness(20) };
            TextBlock contentText = new TextBlock { Text = content, Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333")), FontSize = 22, TextWrapping = TextWrapping.Wrap };
            bodyBorder.Child = contentText;
            Grid.SetRow(bodyBorder, 1);

            cardGrid.Children.Add(headerBorder); cardGrid.Children.Add(bodyBorder);
            outerContainer.Child = cardGrid;
            return outerContainer;
        }
    }
}