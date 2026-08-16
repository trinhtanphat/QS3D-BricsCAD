using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Adds a discoverable color-picker experience to the existing Dim color setting
    /// without changing the persisted #RRGGBB contract used by quantity settings.
    /// </summary>
    internal static class QuantitySettingsColorPickerEnhancer
    {
        private static readonly string[] PresetColors =
        {
            "#FFFFFF", "#D9D9D9", "#A6A6A6", "#737373", "#404040", "#000000",
            "#FF6B6B", "#FF922B", "#FFD43B", "#94D82D", "#51CF66", "#20C997",
            "#22B8CF", "#339AF0", "#4C6EF5", "#7950F2", "#BE4BDB", "#F06595"
        };

        public static void Attach(QuantitySettingsWindow window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            var colorBox = window.FindName("DimColorBox") as TextBox;
            var fieldGrid = colorBox?.Parent as Grid;
            if (colorBox == null || fieldGrid == null) return;

            var previewPanel = fieldGrid.Children
                .OfType<StackPanel>()
                .FirstOrDefault(x => Grid.GetRow(x) == 0 && Grid.GetColumn(x) == 2);
            if (previewPanel == null) return;

            var oldPreview = previewPanel.Children.OfType<Border>().FirstOrDefault();
            var insertionIndex = oldPreview == null ? 0 : previewPanel.Children.IndexOf(oldPreview);
            if (oldPreview != null) previewPanel.Children.Remove(oldPreview);

            var swatch = new Border
            {
                Width = 26,
                Height = 20,
                CornerRadius = new CornerRadius(2),
                BorderThickness = new Thickness(1),
                IsHitTestVisible = false
            };
            swatch.BorderBrush = GetBrush(window, "BorderStrongBrush", Brushes.DimGray);

            var pickerButton = new Button
            {
                Width = 34,
                Height = 28,
                Padding = new Thickness(3),
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Content = swatch,
                ToolTip = "Chọn màu dim"
            };

            previewPanel.Children.Insert(Math.Max(0, insertionIndex), pickerButton);

            var hint = previewPanel.Children.OfType<TextBlock>().FirstOrDefault();
            if (hint != null)
            {
                hint.Text = "Nhập #RRGGBB hoặc bấm ô màu để chọn; chỉ lưu khi giá trị hợp lệ.";
            }

            Action refreshSwatch = () =>
            {
                Color color;
                string normalized;
                if (TryParseHex(colorBox.Text, out color, out normalized))
                {
                    swatch.Background = new SolidColorBrush(color);
                    pickerButton.ToolTip = "Chọn màu dim • " + normalized;
                }
                else
                {
                    swatch.Background = GetBrush(window, "BgInputBrush", Brushes.Transparent);
                    pickerButton.ToolTip = "Mã màu chưa hợp lệ. Bấm để chọn màu.";
                }
            };

            colorBox.TextChanged += delegate { refreshSwatch(); };
            pickerButton.Click += delegate
            {
                ShowPicker(window, pickerButton, colorBox);
            };

            refreshSwatch();
        }

        private static void ShowPicker(Window owner, Button placementTarget, TextBox target)
        {
            Color initial;
            string ignored;
            if (!TryParseHex(target.Text, out initial, out ignored)) initial = Colors.White;

            var popup = new Popup
            {
                PlacementTarget = placementTarget,
                Placement = PlacementMode.Bottom,
                HorizontalOffset = 0,
                VerticalOffset = 4,
                StaysOpen = false,
                AllowsTransparency = true
            };

            var shell = new Border
            {
                Width = 334,
                Padding = new Thickness(14),
                CornerRadius = new CornerRadius(7),
                BorderThickness = new Thickness(1),
                Background = GetBrush(owner, "BgRaisedBrush", new SolidColorBrush(Color.FromRgb(36, 42, 50))),
                BorderBrush = GetBrush(owner, "BorderStrongBrush", new SolidColorBrush(Color.FromRgb(78, 90, 104)))
            };

            var body = new StackPanel();
            shell.Child = body;

            body.Children.Add(new TextBlock
            {
                Text = "Chọn màu dim",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = GetBrush(owner, "TextBrush", Brushes.White),
                Margin = new Thickness(0, 0, 0, 3)
            });
            body.Children.Add(new TextBlock
            {
                Text = "Chọn nhanh hoặc tinh chỉnh RGB. Giá trị lưu vẫn là #RRGGBB.",
                Foreground = GetBrush(owner, "MutedBrush", Brushes.LightGray),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var presetPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            body.Children.Add(presetPanel);

            var redSlider = CreateSlider(initial.R);
            var greenSlider = CreateSlider(initial.G);
            var blueSlider = CreateSlider(initial.B);
            var redValue = CreateValueText(owner);
            var greenValue = CreateValueText(owner);
            var blueValue = CreateValueText(owner);

            body.Children.Add(CreateChannelRow(owner, "R", redSlider, redValue));
            body.Children.Add(CreateChannelRow(owner, "G", greenSlider, greenValue));
            body.Children.Add(CreateChannelRow(owner, "B", blueSlider, blueValue));

            var previewRow = new Grid { Margin = new Thickness(0, 8, 0, 10) };
            previewRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            previewRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            previewRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var preview = new Border
            {
                Width = 44,
                Height = 30,
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                BorderBrush = GetBrush(owner, "BorderStrongBrush", Brushes.DimGray)
            };
            Grid.SetColumn(preview, 0);
            previewRow.Children.Add(preview);

            var hexText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = GetBrush(owner, "TextBrush", Brushes.White)
            };
            Grid.SetColumn(hexText, 2);
            previewRow.Children.Add(hexText);
            body.Children.Add(previewRow);

            Action refreshCandidate = () =>
            {
                var color = Color.FromRgb(
                    (byte)Math.Round(redSlider.Value),
                    (byte)Math.Round(greenSlider.Value),
                    (byte)Math.Round(blueSlider.Value));
                preview.Background = new SolidColorBrush(color);
                redValue.Text = color.R.ToString(CultureInfo.InvariantCulture);
                greenValue.Text = color.G.ToString(CultureInfo.InvariantCulture);
                blueValue.Text = color.B.ToString(CultureInfo.InvariantCulture);
                hexText.Text = ToHex(color);
            };

            foreach (var presetHex in PresetColors)
            {
                Color preset;
                string normalized;
                if (!TryParseHex(presetHex, out preset, out normalized)) continue;

                var presetButton = new Button
                {
                    Width = 31,
                    Height = 29,
                    Padding = new Thickness(3),
                    Margin = new Thickness(0, 0, 5, 5),
                    ToolTip = normalized,
                    Content = new Border
                    {
                        Width = 21,
                        Height = 19,
                        CornerRadius = new CornerRadius(2),
                        BorderBrush = GetBrush(owner, "BorderStrongBrush", Brushes.DimGray),
                        BorderThickness = new Thickness(1),
                        Background = new SolidColorBrush(preset)
                    }
                };

                var captured = preset;
                presetButton.Click += delegate
                {
                    redSlider.Value = captured.R;
                    greenSlider.Value = captured.G;
                    blueSlider.Value = captured.B;
                    refreshCandidate();
                };
                presetPanel.Children.Add(presetButton);
            }

            redSlider.ValueChanged += delegate { refreshCandidate(); };
            greenSlider.ValueChanged += delegate { refreshCandidate(); };
            blueSlider.ValueChanged += delegate { refreshCandidate(); };

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var cancel = new Button
            {
                Content = "Hủy",
                MinWidth = 68,
                Margin = new Thickness(0, 0, 6, 0)
            };
            var apply = new Button
            {
                Content = "Áp dụng",
                MinWidth = 84
            };

            cancel.Click += delegate { popup.IsOpen = false; };
            apply.Click += delegate
            {
                var selected = Color.FromRgb(
                    (byte)Math.Round(redSlider.Value),
                    (byte)Math.Round(greenSlider.Value),
                    (byte)Math.Round(blueSlider.Value));
                target.Text = ToHex(selected);
                target.CaretIndex = target.Text.Length;
                popup.IsOpen = false;
            };

            actions.Children.Add(cancel);
            actions.Children.Add(apply);
            body.Children.Add(actions);

            popup.Child = shell;
            refreshCandidate();
            popup.IsOpen = true;
        }

        private static Grid CreateChannelRow(Window owner, string label, Slider slider, TextBlock value)
        {
            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });

            var caption = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Foreground = GetBrush(owner, "MutedBrush", Brushes.LightGray)
            };
            Grid.SetColumn(caption, 0);
            Grid.SetColumn(slider, 1);
            Grid.SetColumn(value, 2);
            row.Children.Add(caption);
            row.Children.Add(slider);
            row.Children.Add(value);
            return row;
        }

        private static Slider CreateSlider(byte value)
        {
            return new Slider
            {
                Minimum = 0,
                Maximum = 255,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Value = value,
                Margin = new Thickness(2, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static TextBlock CreateValueText(Window owner)
        {
            return new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Consolas"),
                Foreground = GetBrush(owner, "TextBrush", Brushes.White)
            };
        }

        private static bool TryParseHex(string text, out Color color, out string normalized)
        {
            color = Colors.White;
            normalized = string.Empty;
            var value = (text ?? string.Empty).Trim();
            if (value.Length != 7 || value[0] != '#') return false;

            byte r;
            byte g;
            byte b;
            if (!byte.TryParse(value.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) ||
                !byte.TryParse(value.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) ||
                !byte.TryParse(value.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
                return false;

            color = Color.FromRgb(r, g, b);
            normalized = ToHex(color);
            return true;
        }

        private static string ToHex(Color color)
        {
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        private static Brush GetBrush(FrameworkElement owner, object resourceKey, Brush fallback)
        {
            return owner.TryFindResource(resourceKey) as Brush ?? fallback;
        }
    }
}
