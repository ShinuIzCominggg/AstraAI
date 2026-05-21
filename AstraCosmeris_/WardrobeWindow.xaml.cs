using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace AstraCosmeris_
{
    public partial class WardrobeWindow : Window
    {
        private MainWindow _main;

        // Trạng thái độc lập chỉ dùng cho Sân Khấu Preview
        private string currentPreviewState = "idle";
        private string currentPreviewOutfit = "Default";

        private bool isAnimating = false; // Cờ chặn spam click

        public WardrobeWindow(MainWindow main)
        {
            InitializeComponent();
            _main = main;

            // Lấy bộ đồ đang mặc gốc nạp vào Preview
            currentPreviewOutfit = DataManager.Data.CurrentOutfit;
            LoadPreviewImage(currentPreviewState, currentPreviewOutfit);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            _main.CloseExclusiveWindow();
        }

        // 👉 CLICK ĐỔI DÁNG (Pose)
        private async void StateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isAnimating) return; // Đang kéo rèm thì cấm bấm tiếp
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string newState)
            {
                if (currentPreviewState == newState) return;
                await PlayCurtainTransition(newState, currentPreviewOutfit);
            }
        }

        // 👉 CLICK VÀO THẺ BÀI OUTFIT (Bên phải)
        private async void OutfitCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (isAnimating) return;
            if (sender is Border card && card.Tag is string newOutfit)
            {
                if (currentPreviewOutfit == newOutfit) return;
                await PlayCurtainTransition(currentPreviewState, newOutfit);
            }
        }

        // =======================================================
        // MA THUẬT RÈM BUÔNG (CURTAIN TRANSITION)
        // =======================================================
        private async Task PlayCurtainTransition(string newState, string newOutfit)
        {
            isAnimating = true;

            // 1. KÉO RÈM XUỐNG (Thả từ 0 xuống Full chiều cao)
            DoubleAnimation dropAnim = new DoubleAnimation
            {
                To = PreviewContainer.ActualHeight,
                Duration = TimeSpan.FromSeconds(0.25),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            var tcsDrop = new TaskCompletionSource<bool>();
            dropAnim.Completed += (s, e) => tcsDrop.SetResult(true);
            Curtain.BeginAnimation(HeightProperty, dropAnim);

            await tcsDrop.Task; // Chờ rèm thả hết xuống

            // 2. THAY ĐỒ TRONG BÓNG TỐI
            currentPreviewState = newState;
            currentPreviewOutfit = newOutfit;
            LoadPreviewImage(currentPreviewState, currentPreviewOutfit);

            // 3. KÉO RÈM LÊN BẬT MÍ
            DoubleAnimation riseAnim = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.25),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var tcsRise = new TaskCompletionSource<bool>();
            riseAnim.Completed += (s, e) => tcsRise.SetResult(true);
            Curtain.BeginAnimation(HeightProperty, riseAnim);

            await tcsRise.Task; // Chờ rèm cuốn xong
            isAnimating = false;
        }

        // =======================================================
        // GẮP ĐƯỜNG DẪN THÔNG MINH
        // =======================================================
        private void LoadPreviewImage(string state, string outfit)
        {
            string basePath = AppContext.BaseDirectory;
            string targetPath;

            // Nội suy theo cấu trúc Folder của Shinu:
            if (string.IsNullOrEmpty(outfit) || outfit == "Default")
            {
                // VD: assets/idle/idle.png
                targetPath = Path.Combine(basePath, "assets", state, $"{state}.png");
            }
            else
            {
                // VD: assets/happy/wardrobe/school/happy_school.png
                targetPath = Path.Combine(basePath, "assets", state, "wardrobe", outfit, $"{state}_{outfit}.png");
            }

            if (File.Exists(targetPath))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(targetPath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad; // Nạp khẩn cấp lên RAM
                bmp.EndInit();
                bmp.Freeze();
                PreviewImage.Source = bmp;
            }
            else
            {
                // Gắn Fallback nếu lỡ quên làm ảnh cho bộ này ở state này
                PreviewImage.Source = null;
            }
        }

        // =======================================================
        // CHỐT ĐƠN (Áp dụng lên Astra thật ngoài màn hình)
        // =======================================================
        private void BtnEquip_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lưu lại vào bộ nhớ Database
            DataManager.Data.CurrentOutfit = currentPreviewOutfit;
            DataManager.SaveData();

            // 2. Ép Astra ngoài Desktop mặc đồ ngay lập tức
            _main.RefreshOutfit();

            // 3. Thả bong bóng thoại ăn mừng
            _main.ChangeState(PetState.Happy);
            string speech = currentPreviewOutfit == "Default" ? "Tớ về lại bộ mặc định cho thoải mái rồi nha!" : $"Tớ mặc bộ {currentPreviewOutfit} này nhìn có đẹp không?";
            new SpeechBubble(speech, _main, 4000).Show();

            // 4. Tắt rèm ra về =))
            _main.CloseExclusiveWindow();
        }
    }
}