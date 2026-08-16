using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.Updates;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// BLT3D-familiar Start Center surface designed to live inside a native BricsCAD PaletteSet.
    /// It deliberately has no top-level Window ownership so invoking KHỞI ĐẦU never creates a
    /// separate Windows application window.
    /// </summary>
    internal sealed class BltStartCenterPanel : UserControl
    {
        private static readonly Brush ShellBrush = BrushFromRgb(29, 29, 29);
        private static readonly Brush PanelBrush = BrushFromRgb(39, 39, 39);
        private static readonly Brush PanelHoverBrush = BrushFromRgb(47, 47, 47);
        private static readonly Brush ShellBorderBrush = BrushFromRgb(67, 67, 67);
        private static readonly Brush MutedBrush = BrushFromRgb(174, 179, 188);
        private static readonly Brush AccentBrush = BrushFromRgb(20, 113, 236);
        private static readonly Brush TextBrush = Brushes.White;
        private static readonly ControlTemplate ClickSurfaceTemplate = CreateClickSurfaceTemplate();

        private readonly StackPanel _recentPanel = new StackPanel();
        private readonly TextBlock _floorText = new TextBlock();
        private readonly TextBlock _elevationText = new TextBlock();
        private readonly TextBlock _statusText = new TextBlock();

        public BltStartCenterPanel()
        {
            Background = ShellBrush;
            Content = BuildShell();
            Loaded += (_, __) => RefreshFromActiveDocument();
        }

        public void RefreshFromActiveDocument()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document != null)
            {
                var path = document.Name ?? string.Empty;
                if (StartCenterUserStateStore.TryNormalizeDwgPath(path, out var normalized))
                    StartCenterUserStateStore.RecordProject(normalized);
            }

            _floorText.Text = "Tầng —";
            _elevationText.Text = "•  Cao độ 0.000 m";
            if (document != null)
            {
                try
                {
                    if (ProjectContextCoordinator.TryGetReadOnly(document, out var project) &&
                        !string.IsNullOrWhiteSpace(project.ActiveFloorId))
                    {
                        var floor = project.FindFloor(project.ActiveFloorId);
                        if (floor != null)
                        {
                            if (!string.IsNullOrWhiteSpace(floor.Name))
                                _floorText.Text = "Tầng " + floor.Name;
                            _elevationText.Text = "•  Cao độ " + floor.ElevationM.ToString("0.000", CultureInfo.InvariantCulture) + " m";
                        }
                    }
                }
                catch
                {
                    // Start Center is display-only. A project read failure must never mutate CAD state.
                }
            }

            RefreshRecentProjects();
        }

        private UIElement BuildShell()
        {
            var root = new Grid { Background = ShellBrush };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });

            var body = new Grid { Margin = new Thickness(34, 34, 34, 18) };
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(41, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(59, GridUnitType.Star) });
            Grid.SetRow(body, 0);
            root.Children.Add(body);

            var left = BuildLeftPane();
            Grid.SetColumn(left, 0);
            body.Children.Add(left);

            var divider = new Border
            {
                Width = 1,
                Background = ShellBorderBrush,
                Margin = new Thickness(18, 6, 28, 6)
            };
            Grid.SetColumn(divider, 1);
            body.Children.Add(divider);

            var right = BuildRecentPane();
            Grid.SetColumn(right, 2);
            body.Children.Add(right);

            var status = BuildStatusBar();
            Grid.SetRow(status, 1);
            root.Children.Add(status);
            return root;
        }

        private Grid BuildLeftPane()
        {
            var grid = new Grid { Margin = new Thickness(0, 12, 22, 0) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var brand = new StackPanel { Orientation = Orientation.Horizontal };
            brand.Children.Add(new TextBlock
            {
                Text = "✦",
                Foreground = AccentBrush,
                FontSize = 34,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, -2, 14, 0),
                VerticalAlignment = VerticalAlignment.Top
            });
            var brandText = new StackPanel();
            brandText.Children.Add(new TextBlock
            {
                Text = "BLT3D",
                Foreground = TextBrush,
                FontSize = 27,
                FontWeight = FontWeights.Bold
            });
            brandText.Children.Add(new TextBlock
            {
                Text = "BIM Modeling & Quantity Application",
                Foreground = MutedBrush,
                FontSize = 11,
                Margin = new Thickness(1, 1, 0, 0)
            });
            brand.Children.Add(brandText);
            Grid.SetRow(brand, 0);
            grid.Children.Add(brand);

            var description = new TextBlock
            {
                Text = "Giải pháp mô hình hóa thông tin công trình BIM 3D trực quan và tối ưu\nhóa bóc tách khối lượng trong BricsCAD.",
                Foreground = MutedBrush,
                FontSize = 14,
                LineHeight = 20,
                Margin = new Thickness(0, 28, 0, 26)
            };
            Grid.SetRow(description, 1);
            grid.Children.Add(description);

            var quickTitle = new TextBlock
            {
                Text = "QUY TRÌNH NHANH",
                Foreground = MutedBrush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(quickTitle, 2);
            grid.Children.Add(quickTitle);

            var actions = new StackPanel();
            actions.Children.Add(CreateActionCard(
                "＋",
                "Tạo dự án mới",
                "Bắt đầu bản vẽ trắng sạch hoàn toàn",
                ProjectFileUiService.CreateNewDrawing));
            actions.Children.Add(CreateActionCard(
                "▱",
                "Mở tệp dự án...",
                "Chọn tệp BLT3D/QS3D hiện có từ máy tính",
                ProjectFileUiService.OpenProjectFromPicker));

            var saveRow = new Grid();
            saveRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            saveRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            saveRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var save = CreateActionCard("▣", "Lưu", "Lưu project QS3D", ProjectFileUiService.SaveCurrentProject, compact: true);
            Grid.SetColumn(save, 0);
            saveRow.Children.Add(save);
            var saveAs = CreateActionCard("▤", "Lưu thành...", "Tạo bản sao BLT3D", ProjectFileUiService.SaveCurrentProjectAs, compact: true);
            Grid.SetColumn(saveAs, 2);
            saveRow.Children.Add(saveAs);
            actions.Children.Add(saveRow);
            actions.Children.Add(CreateActionCard("↻", "Cập nhật", "Kiểm tra và tải bản cập nhật QS3D", () => UpdateCenterWindowHost.Show()));

            Grid.SetRow(actions, 3);
            grid.Children.Add(actions);

            var version = new TextBlock
            {
                Text = "Phiên bản " + DisplayVersion() + " • BLT3D Team",
                Foreground = MutedBrush,
                FontSize = 10,
                Margin = new Thickness(0, 16, 0, 0)
            };
            Grid.SetRow(version, 5);
            grid.Children.Add(version);
            return grid;
        }

        private Grid BuildRecentPane()
        {
            var grid = new Grid { Margin = new Thickness(18, 12, 0, 0) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var title = new TextBlock
            {
                Text = "DỰ ÁN GẦN ĐÂY",
                Foreground = MutedBrush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);

            var help = new TextBlock
            {
                Text = "Nhấp vào dự án để mở trực tiếp và bắt đầu làm việc",
                Foreground = MutedBrush,
                FontSize = 12,
                Margin = new Thickness(0, 6, 0, 18)
            };
            Grid.SetRow(help, 1);
            grid.Children.Add(help);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _recentPanel
            };
            Grid.SetRow(scroll, 2);
            grid.Children.Add(scroll);
            return grid;
        }

        private Border BuildStatusBar()
        {
            var border = new Border
            {
                Background = BrushFromRgb(35, 35, 35),
                BorderBrush = BrushFromRgb(48, 48, 48),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(9, 5, 12, 4)
            };

            var dock = new DockPanel { LastChildFill = true };
            border.Child = dock;

            var right = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(right, Dock.Right);
            right.Children.Add(StatusItem("○ Nền sáng"));
            right.Children.Add(StatusItem("◐ Tương phản"));
            right.Children.Add(StatusItem("⌞ Vuông góc"));
            right.Children.Add(StatusItem("⌖ Bắt điểm", highlighted: true));
            dock.Children.Add(right);

            var left = new StackPanel { Orientation = Orientation.Horizontal };
            left.Children.Add(StatusButton("Mô hình", () =>
            {
                StartCenterPaletteCoordinator.Hide();
                new Commands().ShowWorkspace();
            }));
            left.Children.Add(StatusButton("BQ", () =>
            {
                StartCenterPaletteCoordinator.Hide();
                new Commands().ShowQuantitySummary();
            }));

            _floorText.Foreground = MutedBrush;
            _floorText.FontSize = 12;
            _floorText.VerticalAlignment = VerticalAlignment.Center;
            _floorText.Margin = new Thickness(12, 0, 0, 0);
            left.Children.Add(_floorText);

            _elevationText.Foreground = MutedBrush;
            _elevationText.FontSize = 12;
            _elevationText.VerticalAlignment = VerticalAlignment.Center;
            _elevationText.Margin = new Thickness(10, 0, 0, 0);
            left.Children.Add(_elevationText);

            _statusText.Foreground = MutedBrush;
            _statusText.FontSize = 10;
            _statusText.VerticalAlignment = VerticalAlignment.Center;
            _statusText.Margin = new Thickness(16, 0, 0, 0);
            left.Children.Add(_statusText);

            dock.Children.Add(left);
            return border;
        }

        private Button CreateActionCard(string glyph, string title, string subtitle, Action action, bool compact = false)
        {
            var frame = new Border
            {
                Background = PanelBrush,
                BorderBrush = ShellBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = compact ? new Thickness(14, 10, 14, 10) : new Thickness(14, 11, 14, 11)
            };

            var content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.Children.Add(new TextBlock
            {
                Text = glyph,
                Foreground = AccentBrush,
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var texts = new StackPanel
            {
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            texts.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = TextBrush,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14
            });
            texts.Children.Add(new TextBlock
            {
                Text = subtitle,
                Foreground = MutedBrush,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(texts, 1);
            content.Children.Add(texts);
            frame.Child = content;

            var button = CreateClickSurface(frame, Cursors.Hand);
            button.Margin = new Thickness(0, 0, 0, 11);
            button.MinHeight = compact ? 54 : 58;
            button.ToolTip = title;
            button.MouseEnter += (_, __) => frame.Background = PanelHoverBrush;
            button.MouseLeave += (_, __) => frame.Background = PanelBrush;
            button.Click += (_, __) => RunUiAction(action);
            return button;
        }

        private UIElement StatusButton(string text, Action action)
        {
            var frame = new Border
            {
                Background = AccentBrush,
                BorderBrush = AccentBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(12, 3, 12, 3)
            };
            frame.Child = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };

            var button = CreateClickSurface(frame, Cursors.Hand);
            button.Margin = new Thickness(0, 0, 7, 0);
            button.ToolTip = text;
            button.Click += (_, __) => RunUiAction(action);
            return button;
        }

        private static Button CreateClickSurface(UIElement content, Cursor cursor)
        {
            return new Button
            {
                Template = ClickSurfaceTemplate,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Cursor = cursor,
                Content = content
            };
        }

        private static ControlTemplate CreateClickSurfaceTemplate()
        {
            var root = new FrameworkElementFactory(typeof(Border));
            root.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.ContentSourceProperty, "Content");
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            root.AppendChild(presenter);

            return new ControlTemplate(typeof(Button))
            {
                VisualTree = root
            };
        }

        private TextBlock StatusItem(string text, bool highlighted = false)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = highlighted ? TextBrush : MutedBrush,
                FontSize = 12,
                FontWeight = highlighted ? FontWeights.SemiBold : FontWeights.Normal,
                Margin = new Thickness(16, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void RefreshRecentProjects()
        {
            _recentPanel.Children.Clear();
            var projects = StartCenterUserStateStore.GetSnapshot().RecentProjects
                .OrderByDescending(item => item.IsPinned)
                .ThenByDescending(item => item.LastOpenedUtc)
                .ToList();

            if (projects.Count == 0)
            {
                _recentPanel.Children.Add(new TextBlock
                {
                    Text = "Chưa có dự án gần đây.",
                    Foreground = MutedBrush,
                    FontSize = 12,
                    Margin = new Thickness(0, 8, 0, 0)
                });
                return;
            }

            foreach (var recent in projects)
                _recentPanel.Children.Add(CreateRecentRow(recent));
        }

        private UIElement CreateRecentRow(StartCenterRecentProject recent)
        {
            var frame = new Border
            {
                BorderBrush = BrushFromRgb(42, 42, 42),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 12, 6, 12)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconFrame = new Border
            {
                Width = 25,
                Height = 25,
                BorderBrush = ShellBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = "▥",
                    Foreground = AccentBrush,
                    FontSize = 17,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            grid.Children.Add(iconFrame);

            string fileName;
            try { fileName = Path.GetFileName(recent.Path); }
            catch { fileName = recent.DisplayName; }

            var text = new StackPanel();
            text.Children.Add(new TextBlock
            {
                Text = fileName,
                Foreground = recent.Exists ? TextBrush : MutedBrush,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13
            });
            text.Children.Add(new TextBlock
            {
                Text = recent.Path,
                Foreground = MutedBrush,
                FontSize = 10,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 3, 12, 0)
            });
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            var date = new TextBlock
            {
                Text = recent.LastOpenedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                Foreground = MutedBrush,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(12, 2, 0, 0)
            };
            Grid.SetColumn(date, 2);
            grid.Children.Add(date);
            frame.Child = grid;

            var button = CreateClickSurface(frame, recent.Exists ? Cursors.Hand : Cursors.Arrow);
            button.IsEnabled = recent.Exists;
            button.ToolTip = recent.Exists ? "Mở " + fileName : "Tệp không còn tồn tại";
            button.MouseEnter += (_, __) => frame.Background = PanelBrush;
            button.MouseLeave += (_, __) => frame.Background = Brushes.Transparent;
            button.Click += (_, __) => OpenRecentProject(recent);
            return button;
        }

        private void OpenRecentProject(StartCenterRecentProject recent)
        {
            if (!StartCenterUserStateStore.TryNormalizeDwgPath(recent.Path, out var normalized) || !File.Exists(normalized))
            {
                _statusText.Text = "Tệp gần đây không còn tồn tại.";
                RefreshRecentProjects();
                return;
            }

            try
            {
                Application.DocumentManager.Open(normalized, false);
                StartCenterUserStateStore.RecordProject(normalized);
                _statusText.Text = "Đã mở " + Path.GetFileName(normalized) + ".";
                RefreshRecentProjects();
            }
            catch (Exception ex)
            {
                _statusText.Text = "Không thể mở: " + ex.Message;
            }
        }

        private void RunUiAction(Action action)
        {
            try
            {
                action();
                _statusText.Text = string.Empty;
                RefreshFromActiveDocument();
            }
            catch (Exception ex)
            {
                _statusText.Text = ex.Message;
            }
        }

        private static string DisplayVersion()
        {
            var informational = typeof(BltStartCenterPanel).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (informational is string version && !string.IsNullOrWhiteSpace(version))
                return version.Split('+')[0];
            return typeof(BltStartCenterPanel).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        }

        private static SolidColorBrush BrushFromRgb(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
