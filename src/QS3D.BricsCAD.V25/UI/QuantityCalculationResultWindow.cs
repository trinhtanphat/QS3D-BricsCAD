using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    internal sealed class QuantityCalculationResultWindow : Window
    {
        private static readonly Brush ShellBrush = FrozenBrush(31, 31, 31);
        private static readonly Brush DividerBrush = FrozenBrush(50, 50, 50);
        private static readonly Brush BorderBrushValue = FrozenBrush(48, 113, 221);
        private static readonly Brush TextBrush = Brushes.White;
        private static readonly Brush MutedBrush = FrozenBrush(205, 209, 216);
        private static readonly Brush SuccessBrush = FrozenBrush(59, 196, 118);
        private static readonly Brush AttentionBrush = FrozenBrush(209, 143, 41);
        private static readonly Brush ErrorBrush = FrozenBrush(219, 68, 68);
        private static readonly Brush AccentBrush = FrozenBrush(25, 113, 238);
        private bool _openModelRequested;

        private QuantityCalculationResultWindow(string title, string heading, string detail, bool success, bool offerModeling = false)
        {
            Title = title;
            Width = 420;
            Height = success ? 302 : offerModeling ? 270 : 238;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = System.Windows.WindowStyle.None;
            AllowsTransparency = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            Content = BuildContent(title, heading, detail, success, offerModeling);
            KeyDown += (_, args) =>
            {
                if (args.Key != Key.Escape) return;
                Close();
                args.Handled = true;
            };
        }

        public static void ShowSuccess(QuantityEngine2Summary summary)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));

            var heading = summary.ReusedExistingResult
                ? "Tính khối lượng thành công (dùng lại kết quả — model chưa đổi):"
                : "Tính khối lượng thành công (đã cập nhật " + summary.RegeneratedCount + " lượt cấu kiện dirty):";

            var detail =
                "• " + summary.ElementCount + " cấu kiện\n" +
                "• Bê tông: " + F3(summary.ConcreteM3) + " m³ (trừ giao " + F3(summary.DeductionM3) + " m³)\n" +
                "• Cốp pha: " + F3(summary.FormworkM2) + " m²\n" +
                "• Chiều dài (dầm/tường): " + F2(summary.BeamWallLengthM) + " m\n" +
                "• Chu vi biên (sàn/móng): ngoài " + F2(summary.OuterPerimeterM) + " m, trong " + F2(summary.InnerPerimeterM) + " m\n\n" +
                "Bấm “Xem khối lượng” để mở bảng tổng hợp chi tiết.";

            new QuantityCalculationResultWindow("Tính khối lượng", heading, detail, true).ShowDialog();
        }

        public static bool ShowNoElements(string message)
        {
            var detail = string.IsNullOrWhiteSpace(message)
                ? "Chưa có cấu kiện QS3D để tính khối lượng. Hãy Tạo mới/Capture cấu kiện trong Mô hình rồi chạy lại Engine2."
                : message.Trim();
            detail += "\n\nChọn “Về Mô hình” để tiếp tục đúng luồng Project/Floor/Family → Tạo mới/Capture → 3D → Khối lượng.";

            var window = new QuantityCalculationResultWindow(
                "Tính khối lượng",
                "Chưa có cấu kiện QS3D để tính khối lượng.",
                detail,
                false,
                offerModeling: true);
            window.ShowDialog();
            return window._openModelRequested;
        }

        public static void ShowError(string message)
        {
            var detail = string.IsNullOrWhiteSpace(message)
                ? "Không thể tính khối lượng. Kiểm tra project QS3D và dữ liệu cấu kiện rồi thử lại."
                : message.Trim();
            new QuantityCalculationResultWindow(
                "Tính khối lượng",
                "Tính khối lượng chưa hoàn tất.",
                detail,
                false).ShowDialog();
        }

        private UIElement BuildContent(string title, string heading, string detail, bool success, bool offerModeling)
        {
            var frame = new Border
            {
                BorderBrush = BorderBrushValue,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Background = ShellBrush,
                Padding = new Thickness(20, 10, 20, 14)
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleBar = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var dragSurface = new Grid { Background = Brushes.Transparent };
            dragSurface.MouseLeftButtonDown += (_, args) =>
            {
                if (args.ChangedButton == MouseButton.Left) DragMove();
            };
            dragSurface.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = TextBrush,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetColumn(dragSurface, 0);
            titleBar.Children.Add(dragSurface);

            var close = new Button
            {
                Content = "×",
                Width = 28,
                Height = 26,
                Padding = new Thickness(0),
                Margin = new Thickness(8, 0, -5, 0),
                Background = Brushes.Transparent,
                Foreground = TextBrush,
                BorderThickness = new Thickness(0),
                FontSize = 18,
                Focusable = false,
                IsTabStop = false,
                ToolTip = "Đóng"
            };
            close.Click += (_, __) => Close();
            Grid.SetColumn(close, 1);
            titleBar.Children.Add(close);
            Grid.SetRow(titleBar, 0);
            root.Children.Add(titleBar);

            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var stateCircle = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(19),
                Background = success ? SuccessBrush : offerModeling ? AttentionBrush : ErrorBrush,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            stateCircle.Child = new TextBlock
            {
                Text = success ? "✓" : offerModeling ? "→" : "!",
                Foreground = Brushes.White,
                FontSize = 23,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            body.Children.Add(stateCircle);

            var texts = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            texts.Children.Add(new TextBlock
            {
                Text = heading,
                Foreground = TextBrush,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap
            });
            texts.Children.Add(new Border
            {
                Height = 1,
                Background = DividerBrush,
                Margin = new Thickness(0, 9, 0, 8)
            });
            texts.Children.Add(new TextBlock
            {
                Text = detail,
                Foreground = MutedBrush,
                FontSize = 12,
                LineHeight = 18,
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(texts, 1);
            body.Children.Add(texts);
            Grid.SetRow(body, 1);
            root.Children.Add(body);

            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };

            if (offerModeling)
            {
                var dismiss = new Button
                {
                    Content = "Đóng",
                    Width = 88,
                    Height = 34,
                    Background = Brushes.Transparent,
                    Foreground = TextBrush,
                    BorderBrush = DividerBrush,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 8, 0)
                };
                dismiss.Click += (_, __) => Close();
                footer.Children.Add(dismiss);
            }

            var ok = new Button
            {
                Content = "OK",
                Width = 112,
                Height = 34,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = AccentBrush,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
                IsDefault = true
            };
            if (offerModeling)
            {
                ok.Content = "Về Mô hình";
                ok.Click += (_, __) =>
                {
                    _openModelRequested = true;
                    Close();
                };
            }
            else
            {
                ok.Click += (_, __) => Close();
            }
            footer.Children.Add(ok);
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            frame.Child = root;
            return frame;
        }

        private static string F3(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);
        private static string F2(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static SolidColorBrush FrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
