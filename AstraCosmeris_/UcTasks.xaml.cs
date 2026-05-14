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
        private readonly string tasksFilePath = Path.Combine(AppContext.BaseDirectory, "tasks.json");
        private readonly Dictionary<DayOfWeek, string> dayColors = new() {
            { DayOfWeek.Monday, "#FF6B6B" }, { DayOfWeek.Tuesday, "#FF9F43" },
            { DayOfWeek.Wednesday, "#FECA57" }, { DayOfWeek.Thursday, "#1DD1A1" },
            { DayOfWeek.Friday, "#54A0FF" }, { DayOfWeek.Saturday, "#5F27CD" }, { DayOfWeek.Sunday, "#FF9FF3" }
        };

        public UcTasks() => InitializeComponent();

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue) LoadTasksData();
        }

        private void LoadTasksData()
        {
            PanelUpcoming.Children.Clear();
            PanelUrgent.Children.Clear();

            if (!File.Exists(tasksFilePath)) return;

            try
            {
                var allTasks = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(tasksFilePath)) ?? new();
                DateTime today = DateTime.Today;
                DateTime endOfWeek = today.AddDays(7 - (int)today.DayOfWeek);

                foreach (var kvp in allTasks.OrderBy(k => k.Key))
                {
                    if (DateTime.TryParse(kvp.Key, out DateTime taskDate) && taskDate >= today && taskDate <= endOfWeek && !string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        bool isUrgent = (taskDate == today || taskDate == today.AddDays(1));
                        var card = CreateTaskCard(taskDate, kvp.Value);

                        if (isUrgent) PanelUrgent.Children.Add(card);
                        else PanelUpcoming.Children.Add(card);
                    }
                }
            }
            catch { }
        }

        private Border CreateTaskCard(DateTime date, string content)
        {
            var headerBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(dayColors[date.DayOfWeek]));

            Grid cardGrid = new Grid();
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Border headerBorder = new Border { Background = headerBrush, CornerRadius = new CornerRadius(15, 15, 0, 0), Padding = new Thickness(20, 12, 20, 12) };
            headerBorder.Child = new TextBlock { Text = date.ToString("dddd, dd/MM/yyyy"), Foreground = System.Windows.Media.Brushes.White, FontSize = 20, FontWeight = System.Windows.FontWeights.Bold };
            Grid.SetRow(headerBorder, 0);

            Border bodyBorder = new Border { Background = System.Windows.Media.Brushes.White, CornerRadius = new CornerRadius(0, 0, 15, 15), Padding = new Thickness(20) };
            bodyBorder.Child = new TextBlock { Text = content, Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333")), FontSize = 22, TextWrapping = System.Windows.TextWrapping.Wrap };
            Grid.SetRow(bodyBorder, 1);

            cardGrid.Children.Add(headerBorder);
            cardGrid.Children.Add(bodyBorder);

            return new Border
            {
                Margin = new Thickness(0, 0, 0, 20),
                CornerRadius = new CornerRadius(15),
                Child = cardGrid,
                Effect = new DropShadowEffect { Color = System.Windows.Media.Colors.Gray, Direction = 315, ShadowDepth = 3, BlurRadius = 10, Opacity = 0.3 }
            };
        }
    }
}