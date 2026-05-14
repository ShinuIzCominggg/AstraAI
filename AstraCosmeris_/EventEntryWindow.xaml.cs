using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AstraCosmeris_
{
    public partial class EventEntryWindow : Window
    {
        private DateTime eventDate;

        public EventEntryWindow(DateTime date)
        {
            InitializeComponent();
            eventDate = date;
            TxtTitle.Text = $"🎉 Sự kiện ngày: {date:dd/MM/yyyy}";
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtEventName.Text))
            {
                System.Windows.MessageBox.Show("Cậu phải nhập tên sự kiện chứ!", "Astra nhắc nhở");
                return;
            }

            AstraEvent newEvent = new AstraEvent
            {
                Title = TxtEventName.Text.Trim(),
                Date = eventDate,
                Type = CboType.Text,
                Location = TxtLocation.Text.Trim(),
                Repeat = CboRepeat.Text
            };

            DataManager.Data.Events.Add(newEvent);
            DataManager.SaveData();

            TxtStatus.Text = "✅ Đã lưu!";
            await Task.Delay(1000);
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) this.DragMove(); }
    }
}