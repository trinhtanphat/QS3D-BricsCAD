using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only density/discoverability layer for the existing WorkspacePanel.
    /// It deliberately reuses the XAML controls and their existing handlers instead of
    /// introducing a second viewport, command surface, or semantic mutation path.
    /// </summary>
    public partial class WorkspacePanel
    {
        private bool _compactShellApplied;
        private Border? _compactHeaderChrome;
        private Border? _compactFooterChrome;

        static WorkspacePanel()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnCompactShellLoaded),
                true);
        }

        private static void OnCompactShellLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel)
                panel.ApplyCompactShellPresentation();
        }

        private void ApplyCompactShellPresentation()
        {
            if (_compactShellApplied)
                return;

            _compactShellApplied = true;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            TuneWorkspaceGrid();
            TuneNamedWorkspaceControls();
            TuneModelTreeDensity();
            TuneActionDensityAndShortcuts();
            TuneSectionHierarchy();
            TuneResponsiveHeader();
            TuneModelSectionHeaderCollision();
        }

        private void TuneWorkspaceGrid()
        {
            var root = WorkspaceContentRoot;
            if (root == null || root.RowDefinitions.Count < 3)
                return;

            root.MinWidth = 0;
            WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            WorkspaceOverflow.ScrollToHorizontalOffset(0);

            root.RowDefinitions[0].Height = new GridLength(40);
            root.RowDefinitions[2].Height = new GridLength(30);

            var workspace = root.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate =>
                    Grid.GetRow(candidate) == 1 &&
                    candidate.ColumnDefinitions.Count == 5);
            if (workspace == null)
                return;

            // The Family/Type + Properties and Room/Selection dashboard panes are retired.
            // Keep their named controls instantiated for code-behind compatibility, but remove
            // every retired column and splitter from the visible layout after persisted widths
            // have been restored. MaxWidth=0 prevents legacy saved widths from resurfacing them.
            var primaryColumn = workspace.ColumnDefinitions[0];
            primaryColumn.MinWidth = 0;
            primaryColumn.MaxWidth = double.PositiveInfinity;
            primaryColumn.Width = new GridLength(1, GridUnitType.Star);

            for (var index = 1; index < workspace.ColumnDefinitions.Count; index++)
            {
                var retiredColumn = workspace.ColumnDefinitions[index];
                retiredColumn.MinWidth = 0;
                retiredColumn.MaxWidth = 0;
                retiredColumn.Width = new GridLength(0);
            }

            foreach (UIElement child in workspace.Children)
            {
                if (Grid.GetColumn(child) > 0)
                    child.Visibility = Visibility.Collapsed;
            }
        }

        private void TuneNamedWorkspaceControls()
        {
            ZoneCombo.MinHeight = 25;
            ZoneCombo.Padding = new Thickness(5, 1, 5, 1);
            FloorCombo.MinHeight = 25;
            FloorCombo.Padding = new Thickness(5, 1, 5, 1);

            FamilySearch.MinHeight = 24;
            FamilySearch.Padding = new Thickness(5, 1, 5, 1);
            FamilyList.MinHeight = 82;
            PropertyList.MinHeight = 118;
            InspectionList.MinHeight = 96;

            SelectionCount.FontWeight = FontWeights.SemiBold;
            SelectionCount.Foreground = TryFindResource("SuccessBrush") as Brush ?? SelectionCount.Foreground;

            AppendShortcutHint(FamilySearch, "Ctrl+F");
        }

        private void TuneModelTreeDensity()
        {
            ModelTree.FontSize = 11.5;
            ModelTree.Padding = new Thickness(0);
            foreach (var item in ModelTree.Items.OfType<TreeViewItem>())
                TuneTreeItem(item, 0);
        }

        private static void TuneTreeItem(TreeViewItem item, int depth)
        {
            item.MinHeight = 22;
            item.Padding = new Thickness(depth == 0 ? 3 : 2, 1, 2, 1);
            item.Margin = new Thickness(0);

            foreach (var child in item.Items.OfType<TreeViewItem>())
                TuneTreeItem(child, depth + 1);
        }

        private void TuneActionDensityAndShortcuts()
        {
            foreach (var button in FindVisualChildren<Button>(this))
            {
                if (button.MinHeight < 24)
                    button.MinHeight = 24;
            }

            AppendShortcutHint(FindButton("Lưu"), "Ctrl+S");
            AppendShortcutHint(FindButton("Làm mới"), "F5");
            AppendShortcutHint(FindButton("BQ"), "Ctrl+B");
            AppendShortcutHint(FindButton("Xóa"), "Delete khi Family list đang focus");
        }

        private void TuneSectionHierarchy()
        {
            var sectionTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "PHẠM VI LÀM VIỆC",
                "MÔ HÌNH",
                "FAMILY / TYPE",
                "THUỘC TÍNH",
                "HT_PHÒNG",
                "ĐỐI TƯỢNG ĐANG CHỌN"
            };

            foreach (var text in FindVisualChildren<TextBlock>(this))
            {
                if (!sectionTitles.Contains(text.Text ?? string.Empty))
                    continue;

                text.FontWeight = FontWeights.SemiBold;
                if (text.FontSize < 11)
                    text.FontSize = 11;
            }
        }

        private void TuneResponsiveHeader()
        {
            var root = WorkspaceContentRoot;
            if (root == null)
                return;

            var headerBorder = root.Children
                .OfType<Border>()
                .FirstOrDefault(border => Grid.GetRow(border) == 0);
            if (!(headerBorder?.Child is Grid header) || header.ColumnDefinitions.Count != 3)
                return;

            _compactHeaderChrome = headerBorder;
            _compactFooterChrome = root.Children
                .OfType<Border>()
                .FirstOrDefault(border => Grid.GetRow(border) == 2);

            WorkspaceOverflow.SizeChanged += OnCompactViewportSizeChanged;
            WorkspaceOverflow.ScrollChanged += OnCompactViewportScrollChanged;
            PinCompactChromeToViewport();

            header.SizeChanged += OnCompactHeaderSizeChanged;
            ApplyCompactHeaderBreakpoint(header);
        }

        private void OnCompactViewportSizeChanged(object sender, SizeChangedEventArgs e)
        {
            PinCompactChromeToViewport();
        }

        private void OnCompactViewportScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(e.HorizontalChange) > 0.01 || Math.Abs(e.ViewportWidthChange) > 0.01)
                PinCompactChromeToViewport();
        }

        private void PinCompactChromeToViewport()
        {
            var viewportWidth = WorkspaceOverflow.ViewportWidth;
            if (double.IsNaN(viewportWidth) || double.IsInfinity(viewportWidth) || viewportWidth <= 0)
                viewportWidth = WorkspaceOverflow.ActualWidth;
            if (double.IsNaN(viewportWidth) || double.IsInfinity(viewportWidth) || viewportWidth <= 0)
                return;

            var horizontalOffset = WorkspaceOverflow.HorizontalOffset;
            PinCompactChrome(_compactHeaderChrome, viewportWidth, horizontalOffset);
            PinCompactChrome(_compactFooterChrome, viewportWidth, horizontalOffset);
        }

        private static void PinCompactChrome(Border? chrome, double viewportWidth, double horizontalOffset)
        {
            if (chrome == null)
                return;

            chrome.Width = viewportWidth;
            chrome.HorizontalAlignment = HorizontalAlignment.Left;
            chrome.RenderTransform = new TranslateTransform(horizontalOffset, 0);
        }

        private static void OnCompactHeaderSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Grid header)
                ApplyCompactHeaderBreakpoint(header);
        }

        private static void ApplyCompactHeaderBreakpoint(Grid header)
        {
            var branding = header.Children
                .OfType<StackPanel>()
                .FirstOrDefault(panel => Grid.GetColumn(panel) == 0);
            var actions = header.Children
                .OfType<StackPanel>()
                .FirstOrDefault(panel => Grid.GetColumn(panel) == 2);
            var status = header.Children
                .OfType<TextBlock>()
                .FirstOrDefault(text => Grid.GetColumn(text) == 1);

            if (branding == null || actions == null)
                return;

            var width = header.ActualWidth;
            var narrow = width > 0 && width < 570;
            var compact = width > 0 && width < 700;

            var workspaceBadge = FindHeaderBadge(branding, "BIM WORKSPACE");
            if (workspaceBadge != null)
                workspaceBadge.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;

            var semanticBadge = FindHeaderBadge(branding, "SEMANTIC MODEL");
            if (semanticBadge != null)
                semanticBadge.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;

            if (status != null)
            {
                status.Margin = narrow ? new Thickness(4, 0, 4, 0) : new Thickness(8, 0, 8, 0);
                status.MinWidth = 0;
                status.TextAlignment = TextAlignment.Center;
            }

            branding.Margin = new Thickness(0, 0, narrow ? 4 : 8, 0);
            actions.Margin = new Thickness(narrow ? 2 : 4, 0, 0, 0);

            foreach (var button in actions.Children.OfType<Button>())
            {
                button.Padding = narrow ? new Thickness(5, 2, 5, 2) : new Thickness(6, 2, 6, 2);

                var label = button.Content as string;
                if (string.Equals(label, "Xoay 3D", StringComparison.Ordinal) ||
                    string.Equals(label, "Xoay", StringComparison.Ordinal))
                {
                    button.Content = narrow ? "Xoay" : "Xoay 3D";
                }
                else if (string.Equals(label, "Zoom chọn", StringComparison.Ordinal) ||
                         string.Equals(label, "Zoom", StringComparison.Ordinal))
                {
                    button.Content = narrow ? "Zoom" : "Zoom chọn";
                }
            }
        }

        private void TuneModelSectionHeaderCollision()
        {
            var modelTitle = FindVisualChildren<TextBlock>(this)
                .FirstOrDefault(text => string.Equals(text.Text, "MÔ HÌNH", StringComparison.OrdinalIgnoreCase));
            var refreshButton = FindButton("Làm mới");
            if (modelTitle == null || refreshButton == null)
                return;

            var titleStack = VisualTreeHelper.GetParent(modelTitle) as StackPanel;
            var header = titleStack == null
                ? null
                : VisualTreeHelper.GetParent(titleStack) as DockPanel;
            if (titleStack == null || header == null || VisualTreeHelper.GetParent(refreshButton) != header)
                return;

            // The original DockPanel lets its final child fill the remaining width. In the narrow
            // model pane that can let "Làm mới" paint into the MÔ HÌNH/caption area. Reserve the
            // action at the right and constrain the title stack to the measured remaining width.
            header.LastChildFill = false;
            DockPanel.SetDock(titleStack, Dock.Left);
            DockPanel.SetDock(refreshButton, Dock.Right);
            titleStack.MinWidth = 0;
            titleStack.Margin = new Thickness(0, 0, 7, 0);
            refreshButton.HorizontalAlignment = HorizontalAlignment.Right;
            refreshButton.VerticalAlignment = VerticalAlignment.Top;

            foreach (var label in titleStack.Children.OfType<TextBlock>())
            {
                label.TextWrapping = TextWrapping.NoWrap;
                label.TextTrimming = TextTrimming.CharacterEllipsis;
            }

            void UpdateAvailableTitleWidth()
            {
                if (header.ActualWidth <= 0)
                    return;

                var refreshWidth = Math.Max(refreshButton.ActualWidth, refreshButton.DesiredSize.Width);
                titleStack.MaxWidth = Math.Max(48, header.ActualWidth - refreshWidth - 7);
            }

            UpdateAvailableTitleWidth();
            header.SizeChanged += (_, __) => UpdateAvailableTitleWidth();
            refreshButton.SizeChanged += (_, __) => UpdateAvailableTitleWidth();
        }

        private static Border? FindHeaderBadge(StackPanel branding, string text)
        {
            return branding.Children
                .OfType<Border>()
                .FirstOrDefault(border =>
                    border.Child is TextBlock label &&
                    string.Equals(label.Text, text, StringComparison.Ordinal));
        }

        private Button? FindButton(string content)
        {
            return FindVisualChildren<Button>(this)
                .FirstOrDefault(button => string.Equals(button.Content as string, content, StringComparison.Ordinal));
        }

        private static void AppendShortcutHint(FrameworkElement? element, string shortcut)
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

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
                yield break;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                    yield return match;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
    }
}
