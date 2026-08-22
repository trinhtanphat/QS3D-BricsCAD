using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    internal sealed class ProjectOperationResultWindow : Window
    {
        private static readonly Brush ShellBrush = FrozenBrush(31, 31, 31);
        private static readonly Brush PanelBrush = FrozenBrush(39, 39, 39);
        private static readonly Brush BorderBrushValue = FrozenBrush(48, 113, 221);
        private static readonly Brush TextBrush = Brushes.White;
        private static readonly Brush MutedBrush = FrozenBrush(205, 209, 216);
        private static readonly Brush SuccessBrush = FrozenBrush(59, 196, 118);
        private static readonly Brush AccentBrush = FrozenBrush(25, 113, 238);

        private ProjectOperationResultWindow(string title, string summary, string detail)
        {
            Title = title;
            Width = 420;
            Height = 244;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = System.Windows.WindowStyle.None;
            AllowsTransparency = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            Content = BuildContent(title, summary, detail);
            KeyDown += (_, args) =>
            {
                if (args.Key != Key.Escape) return;
                Close();
                args.Handled = true;
            };
        }

        public static void ShowOpenSuccess(string projectPath, ProjectState project, long readMilliseconds, long bindMilliseconds, long totalMilliseconds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var fileName = SafeFileName(projectPath);
            var summary = "Đã mở \"" + fileName + "\" — " + project.Zones.Count + " zone, " + project.Elements.Count + " element.";
            var detail =
                "⚙ Đọc tệp " + readMilliseconds + " ms\n" +
                "   Mở bản vẽ + dựng project " + bindMilliseconds + " ms\n" +
                "   Tổng thời gian " + totalMilliseconds + " ms";
            new ProjectOperationResultWindow("Mở dự án", summary, detail).ShowDialog();
        }

        public static void ShowSaveSuccess(string projectPath, ProjectState project, long elapsedMilliseconds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var fileName = SafeFileName(projectPath);
            var summary = "Đã lưu \"" + fileName + "\" — " + project.Zones.Count + " zone, " + project.Elements.Count + " element.";
            var detail = "Đã lưu DWG hiện hành và project QS3D trong " + elapsedMilliseconds + " ms.";
            new ProjectOperationResultWindow("Lưu dự án", summary, detail).ShowDialog();
        }

        public static void ShowSaveAsSuccess(string drawingPath, string projectPath, ProjectState project, long elapsedMilliseconds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var drawingName = SafeFileName(drawingPath);
            var projectName = SafeFileName(projectPath);
            var summary = "Đã lưu thành \"" + drawingName + "\" — " + project.Zones.Count + " zone, " + project.Elements.Count + " element.";
            var detail = "DWG hiện hành đã chuyển sang đường dẫn mới. Project QS3D: " + projectName + "\nHoàn tất trong " + elapsedMilliseconds + " ms.";
            new ProjectOperationResultWindow("Lưu thành", summary, detail).ShowDialog();
        }

        private UIElement BuildContent(string title, string summary, string detail)
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

            var titleBar = new Grid
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 0, 0, 10)
            };
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var dragSurface = new Grid { Background = Brushes.Transparent };
            dragSurface.MouseLeftButtonDown += (_, args) =>
            {
                if (args.ChangedButton == MouseButton.Left)
                    DragMove();
            };

            var heading = new TextBlock
            {
                Text = title,
                Foreground = TextBrush,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            dragSurface.Children.Add(heading);
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
                FontWeight = FontWeights.Normal,
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

            var successCircle = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(19),
                Background = SuccessBrush,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            successCircle.Child = new TextBlock
            {
                Text = "✓",
                Foreground = Brushes.White,
                FontSize = 23,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            body.Children.Add(successCircle);

            var texts = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            texts.Children.Add(new TextBlock
            {
                Text = summary,
                Foreground = TextBrush,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap
            });
            texts.Children.Add(new Border
            {
                Height = 1,
                Background = PanelBrush,
                Margin = new Thickness(0, 10, 0, 8)
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
                Margin = new Thickness(0, 12, 0, 0),
                IsDefault = true
            };
            ok.Click += (_, __) => Close();
            Grid.SetRow(ok, 2);
            root.Children.Add(ok);

            frame.Child = root;
            return frame;
        }

        private static string SafeFileName(string path)
        {
            try
            {
                var name = Path.GetFileName(path);
                return string.IsNullOrWhiteSpace(name) ? path : name;
            }
            catch
            {
                return path;
            }
        }

        private static SolidColorBrush FrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
