using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only density/discoverability layer for the existing RightPanel.
    /// Existing Xref/layer handlers remain the only behavior and mutation paths.
    /// </summary>
    public partial class RightPanel
    {
        private bool _rightCompactShellApplied;

        static RightPanel()
        {
            EventManager.RegisterClassHandler(
                typeof(RightPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnRightCompactShellLoaded),
                true);
        }

        private static void OnRightCompactShellLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is RightPanel panel)
                panel.ApplyRightCompactShellPresentation();
        }

        private void ApplyRightCompactShellPresentation()
        {
            if (_rightCompactShellApplied)
                return;

            _rightCompactShellApplied = true;
            MinWidth = 220;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            TuneRightPanelGrid();
            TuneRightPanelLists();
            TuneRightPanelActions();
            TuneRightPanelSectionHierarchy();
            TuneRightPanelHeaderCollisions();
            TuneDrawingColumns();
            TuneRightPanelWidthBreakpoint();
        }

        private void TuneRightPanelGrid()
        {
            if (!(Content is Grid root) || root.RowDefinitions.Count < 4)
                return;

            root.RowDefinitions[0].Height = new GridLength(238);
            root.RowDefinitions[0].MinHeight = 145;
            root.RowDefinitions[1].Height = new GridLength(4);
            root.RowDefinitions[3].Height = new GridLength(28);

            foreach (var splitter in root.Children.OfType<GridSplitter>())
            {
                splitter.ShowsPreview = true;
                splitter.Focusable = false;
            }
        }

        private void TuneRightPanelLists()
        {
            DrawingList.MinHeight = 105;
            DrawingList.FontSize = 11.5;
            LayerList.MinHeight = 165;
            LayerList.FontSize = 11.5;

            ScrollViewer.SetHorizontalScrollBarVisibility(DrawingList, ScrollBarVisibility.Disabled);
            ScrollViewer.SetHorizontalScrollBarVisibility(LayerList, ScrollBarVisibility.Disabled);

            LayerSearchBox.MinHeight = 24;
            LayerSearchBox.Padding = new Thickness(5, 1, 5, 1);

            AppendRightShortcutHint(LayerSearchBox, "Ctrl+F");
        }

        private void TuneRightPanelActions()
        {
            foreach (var wrap in FindRightVisualChildren<WrapPanel>(this))
            {
                wrap.HorizontalAlignment = HorizontalAlignment.Left;
                wrap.VerticalAlignment = VerticalAlignment.Center;
            }

            foreach (var button in FindRightVisualChildren<Button>(this))
            {
                if (button.MinHeight < 24)
                    button.MinHeight = 24;
            }

            AppendRightShortcutHint(FindRightButton("Làm mới"), "F5");
            AppendRightShortcutHint(LayerSearchBox, "Esc xóa bộ lọc");
        }

        private void TuneRightPanelSectionHierarchy()
        {
            var sectionTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "QUẢN LÝ BẢN VẼ",
                "QUẢN LÝ LỚP"
            };

            foreach (var text in FindRightVisualChildren<TextBlock>(this))
            {
                if (!sectionTitles.Contains(text.Text ?? string.Empty))
                    continue;

                text.FontWeight = FontWeights.SemiBold;
                if (text.FontSize < 11.5)
                    text.FontSize = 11.5;
            }
        }

        private void TuneRightPanelHeaderCollisions()
        {
            foreach (var titleText in new[] { "QUẢN LÝ BẢN VẼ", "QUẢN LÝ LỚP" })
            {
                var title = FindRightVisualChildren<TextBlock>(this)
                    .FirstOrDefault(text => string.Equals(text.Text, titleText, StringComparison.OrdinalIgnoreCase));
                if (title == null)
                    continue;

                var titleStack = VisualTreeHelper.GetParent(title) as StackPanel;
                var titleChrome = titleStack == null ? null : VisualTreeHelper.GetParent(titleStack) as Grid;
                var header = titleChrome == null ? null : VisualTreeHelper.GetParent(titleChrome) as DockPanel;
                var actions = header?.Children
                    .OfType<StackPanel>()
                    .FirstOrDefault(panel => panel.Orientation == Orientation.Horizontal);

                if (titleStack == null || titleChrome == null || header == null || actions == null)
                    continue;

                // The action stack is the final child in XAML. With DockPanel.LastChildFill=True
                // its Dock=Right is ignored, which lets badges/buttons paint through the title.
                header.LastChildFill = false;
                DockPanel.SetDock(titleChrome, Dock.Left);
                DockPanel.SetDock(actions, Dock.Right);
                titleChrome.MinWidth = 0;
                titleStack.MinWidth = 0;
                titleChrome.Margin = new Thickness(0, 0, 6, 0);
                actions.HorizontalAlignment = HorizontalAlignment.Right;
                actions.VerticalAlignment = VerticalAlignment.Top;

                foreach (var label in titleStack.Children.OfType<TextBlock>())
                {
                    label.TextWrapping = TextWrapping.NoWrap;
                    label.TextTrimming = TextTrimming.CharacterEllipsis;
                }

                void UpdateHeader()
                {
                    if (header.ActualWidth <= 0)
                        return;

                    var actionsWidth = Math.Max(actions.ActualWidth, actions.DesiredSize.Width);
                    titleChrome.MaxWidth = Math.Max(72, header.ActualWidth - actionsWidth - 6);

                    var narrow = header.ActualWidth < 320;
                    foreach (var badge in actions.Children.OfType<Border>())
                        badge.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
                }

                UpdateHeader();
                header.SizeChanged += (_, __) => UpdateHeader();
                actions.SizeChanged += (_, __) => UpdateHeader();
            }
        }

        private void TuneDrawingColumns()
        {
            if (!(DrawingList.View is GridView gridView) || gridView.Columns.Count < 4)
                return;

            void UpdateColumns()
            {
                if (DrawingList.ActualWidth <= 0)
                    return;

                // Keep all four headers visible inside the real palette width. The previous
                // fixed 105+52+28+66 widths overflow once borders/scrollbar are included.
                var compact = DrawingList.ActualWidth < 310;
                var lockWidth = compact ? 46 : 52;
                var countWidth = compact ? 30 : 34;
                var scaleWidth = compact ? 58 : 66;
                var chromeAllowance = 24;
                var nameWidth = Math.Max(
                    72,
                    DrawingList.ActualWidth - lockWidth - countWidth - scaleWidth - chromeAllowance);

                gridView.Columns[0].Width = nameWidth;
                gridView.Columns[1].Width = lockWidth;
                gridView.Columns[2].Width = countWidth;
                gridView.Columns[3].Width = scaleWidth;
            }

            DrawingList.SizeChanged += (_, __) => UpdateColumns();
            UpdateColumns();
        }

        private void TuneRightPanelWidthBreakpoint()
        {
            void Apply()
            {
                var narrow = ActualWidth > 0 && ActualWidth < 320;
                foreach (var button in FindRightVisualChildren<Button>(this))
                    button.Padding = narrow ? new Thickness(5, 2, 5, 2) : new Thickness(7, 3, 7, 3);
            }

            SizeChanged += (_, __) => Apply();
            Apply();
        }

        private Button? FindRightButton(string content)
        {
            return FindRightVisualChildren<Button>(this)
                .FirstOrDefault(button => string.Equals(button.Content as string, content, StringComparison.Ordinal));
        }

        private static void AppendRightShortcutHint(FrameworkElement? element, string shortcut)
        {
            if (element == null || string.IsNullOrWhiteSpace(shortcut))
                return;

            var current = element.ToolTip as string;
            if (element.ToolTip != null && current == null)
                return;

            if (current != null && !string.IsNullOrWhiteSpace(current) &&
                current.IndexOf(shortcut, StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            element.ToolTip = current == null || string.IsNullOrWhiteSpace(current)
                ? shortcut
                : current.TrimEnd() + "  •  " + shortcut;
            ToolTipService.SetShowDuration(element, 10000);
        }

        private static IEnumerable<T> FindRightVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
                yield break;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                    yield return match;

                foreach (var descendant in FindRightVisualChildren<T>(child))
                    yield return descendant;
            }
        }
    }
}