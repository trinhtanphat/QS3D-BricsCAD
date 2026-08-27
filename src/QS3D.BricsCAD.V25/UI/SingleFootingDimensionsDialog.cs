using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using QS3D.Core.Geometry;

namespace QS3D.BricsCAD.V25.UI
{
    internal sealed class SingleFootingDimensionsDialog : Window
    {
        private readonly Dictionary<string, TextBox> _inputs = new Dictionary<string, TextBox>(StringComparer.Ordinal);
        private readonly TextBlock _validation;

        public SingleFootingDimensionsDialog()
        {
            Title = "Kích thước móng đơn (mm)";
            Width = 920;
            Height = 570;
            MinWidth = 820;
            MinHeight = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;
            Background = Brush("#202830");
            Foreground = Brushes.White;

            var root = new Grid { Margin = new Thickness(14) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = root;

            var title = new TextBlock
            {
                Text = "Kích thước móng đơn (mm) — nhập trực tiếp trên sơ đồ",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 14)
            };
            root.Children.Add(title);

            var diagrams = new Grid();
            diagrams.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            diagrams.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(diagrams, 1);
            root.Children.Add(diagrams);

            var plan = BuildPlanPanel();
            plan.Margin = new Thickness(10, 0, 20, 0);
            diagrams.Children.Add(plan);

            var section = BuildSectionPanel();
            Grid.SetColumn(section, 1);
            diagrams.Children.Add(section);

            var footer = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            _validation = new TextBlock
            {
                Foreground = Brush("#FFB35C"),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 12, 0)
            };
            footer.Children.Add(_validation);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal };
            Grid.SetColumn(buttons, 1);
            footer.Children.Add(buttons);

            var ok = new Button
            {
                Content = "Đồng ý (OK)",
                MinWidth = 132,
                Height = 36,
                Margin = new Thickness(0, 0, 10, 0),
                Background = Brush("#3D8BFF"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsDefault = true
            };
            ok.Click += OnOk;
            buttons.Children.Add(ok);

            var cancel = new Button
            {
                Content = "Hủy",
                MinWidth = 105,
                Height = 36,
                Background = Brush("#2A333E"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsCancel = true
            };
            buttons.Children.Add(cancel);

            LoadDefaults(SingleFootingContract.Defaults);
        }

        public SingleFootingDimensions? Dimensions { get; private set; }

        private FrameworkElement BuildPlanPanel()
        {
            var host = new Grid();
            host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            host.Children.Add(new TextBlock
            {
                Text = "MẶT BẰNG",
                Foreground = Brush("#9DB4CC"),
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var canvas = new Canvas { MinHeight = 350 };
            Grid.SetRow(canvas, 1);
            host.Children.Add(canvas);

            AddRectangle(canvas, 105, 65, 250, 250, "#44C8FF", 2);
            AddRectangle(canvas, 165, 125, 130, 130, "#44C8FF", 1.5);
            AddLine(canvas, 105, 65, 165, 125, "#44C8FF", true);
            AddLine(canvas, 355, 65, 295, 125, "#44C8FF", true);
            AddLine(canvas, 105, 315, 165, 255, "#44C8FF", true);
            AddLine(canvas, 355, 315, 295, 255, "#44C8FF", true);

            AddCaption(canvas, "Đỉnh (L2 × W2)", 168, 185, "#44C8FF");
            AddCaption(canvas, "Đáy (L1 × W1)", 110, 290, "#44C8FF");

            AddInput(canvas, "L1", "L1", 188, 328, 88);
            AddInput(canvas, "W1", "W1", 8, 173, 88);
            AddInput(canvas, "L2", "L2", 188, 18, 88);
            AddInput(canvas, "W2", "W2", 360, 173, 88);
            return host;
        }

        private FrameworkElement BuildSectionPanel()
        {
            var host = new Grid();
            host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            host.Children.Add(new TextBlock
            {
                Text = "MẶT CẮT",
                Foreground = Brush("#9DB4CC"),
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var canvas = new Canvas { MinHeight = 350 };
            Grid.SetRow(canvas, 1);
            host.Children.Add(canvas);

            // H1 base prism and H2 tapered cap.
            AddLine(canvas, 80, 285, 80, 220, "#44C8FF", false, 2);
            AddLine(canvas, 80, 285, 315, 285, "#44C8FF", false, 2);
            AddLine(canvas, 315, 285, 315, 220, "#44C8FF", false, 2);
            AddLine(canvas, 80, 220, 315, 220, "#44C8FF", false, 2);
            AddLine(canvas, 80, 220, 140, 145, "#44C8FF", false, 2);
            AddLine(canvas, 315, 220, 255, 145, "#44C8FF", false, 2);
            AddLine(canvas, 140, 145, 255, 145, "#44C8FF", false, 2);
            AddLine(canvas, 50, 292, 340, 292, "#6B7A88", false, 1);

            AddCaption(canvas, "Mặt đài", 142, 125, "#44C8FF");
            AddCaption(canvas, "Đế đài", 92, 242, "#44C8FF");
            AddInput(canvas, "H2", "H2", 320, 160, 82);
            AddInput(canvas, "H1", "H1", 320, 235, 82);
            AddCaption(canvas, "H2 = 0 → đài hộp trơn. Có chóp vát khi L2×W2 nhỏ hơn L1×W1.", 46, 320, "#44C8FF", 12);
            return host;
        }

        private void AddInput(Canvas canvas, string label, string key, double left, double top, double width)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var caption = new TextBlock
            {
                Text = label,
                Foreground = Brush("#66F08B"),
                Width = 26,
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(caption);

            var input = new TextBox
            {
                Width = width,
                Height = 27,
                Background = Brush("#253240"),
                Foreground = Brushes.White,
                BorderBrush = Brush("#438EFF"),
                BorderThickness = new Thickness(1),
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(3)
            };
            _inputs[key] = input;
            panel.Children.Add(input);
            Canvas.SetLeft(panel, left);
            Canvas.SetTop(panel, top);
            canvas.Children.Add(panel);
        }

        private void LoadDefaults(SingleFootingDimensions dimensions)
        {
            Set("L1", dimensions.L1M * 1000d);
            Set("W1", dimensions.W1M * 1000d);
            Set("L2", dimensions.L2M * 1000d);
            Set("W2", dimensions.W2M * 1000d);
            Set("H1", dimensions.H1M * 1000d);
            Set("H2", dimensions.H2M * 1000d);
        }

        private void Set(string key, double millimeters) =>
            _inputs[key].Text = millimeters.ToString("0.###", CultureInfo.InvariantCulture);

        private void OnOk(object sender, RoutedEventArgs e)
        {
            try
            {
                Dimensions = new SingleFootingDimensions(
                    ReadMillimeters("L1") / 1000d,
                    ReadMillimeters("W1") / 1000d,
                    ReadMillimeters("L2") / 1000d,
                    ReadMillimeters("W2") / 1000d,
                    ReadMillimeters("H1") / 1000d,
                    ReadMillimeters("H2") / 1000d);
                _validation.Text = string.Empty;
                DialogResult = true;
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentOutOfRangeException || ex is OverflowException)
            {
                _validation.Text = "Kích thước không hợp lệ: " + ex.Message;
            }
        }

        private double ReadMillimeters(string key)
        {
            var text = (_inputs[key].Text ?? string.Empty).Trim();
            if ((!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                 !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new FormatException(key + " phải là số hữu hạn (mm).");
            return value;
        }

        private static SolidColorBrush Brush(string hex) =>
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        private static void AddRectangle(Canvas canvas, double left, double top, double width, double height, string color, double thickness)
        {
            var shape = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = Brush(color),
                StrokeThickness = thickness,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(shape, left);
            Canvas.SetTop(shape, top);
            canvas.Children.Add(shape);
        }

        private static void AddLine(Canvas canvas, double x1, double y1, double x2, double y2, string color, bool dashed, double thickness = 1d)
        {
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = Brush(color),
                StrokeThickness = thickness
            };
            if (dashed) line.StrokeDashArray = new DoubleCollection { 4d, 3d };
            canvas.Children.Add(line);
        }

        private static void AddCaption(Canvas canvas, string text, double left, double top, string color, double size = 12d)
        {
            var block = new TextBlock { Text = text, Foreground = Brush(color), FontSize = size, TextWrapping = TextWrapping.Wrap };
            Canvas.SetLeft(block, left);
            Canvas.SetTop(block, top);
            canvas.Children.Add(block);
        }
    }
}
