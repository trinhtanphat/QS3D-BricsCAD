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

        private Grid? ResolveWorkspaceRoot()
        {
            if (Content is Grid grid)
                return grid;

            // WorkspacePanel.xaml intentionally wraps the root Grid in a ScrollViewer.
            // The old compact-shell code only handled a direct Grid, so every grid/header
            // breakpoint silently became a no-op in the actual BricsCAD palette.
            if (Content is ScrollViewer scrollViewer && scrollViewer.Content is Grid wrappedGrid)
                return wrappedGrid;

            return null;
        }

        private void TuneWorkspaceGrid()
        {
            var root = ResolveWorkspaceRoot();
            if (root == null || root.RowDefinitions.Count < 3)
                return;

            root.MinWidth = 0;
            root.HorizontalAlignment = HorizontalAlignment.Stretch;

            if (Content is ScrollViewer overflow)
            {
                // The body now reflows instead of requiring the historical 560 px canvas.
                // Removing the horizontal scrollbar is important in docked/narrow palettes:
                // otherwise BricsCAD clips the useful right edge exactly as in the screenshot.
                overflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                overflow.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                overflow.PanningMode = PanningMode.None;
            }

            // Compact chrome leaves more room for model/property inspection on 1366x768-class CAD workstations.
            root.RowDefinitions[0].Height = new GridLength(40);
            root.RowDefinitions[2].Height = new GridLength(30);

            var workspace = root.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate =>
                    Grid.GetRow(candidate) == 1 &&
                    candidate.ColumnDefinitions.Count == 5);
            if (workspace == null)
                return;

            while (workspace.RowDefinitions.Count < 3)
                workspace.RowDefinitions.Add(new RowDefinition());

            var modelPane = workspace.Children
                .OfType<FrameworkElement>()
                .FirstOrDefault(candidate =>
                    !(candidate is GridSplitter) && Grid.GetColumn(candidate) == 0);
            var familyAndProperties = workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate => Grid.GetColumn(candidate) == 2 && candidate.RowDefinitions.Count == 3);
            var roomAndSelection = workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate => Grid.GetColumn(candidate) == 4 && candidate.RowDefinitions.Count == 3);
            var splitters = workspace.Children
                .OfType<GridSplitter>()
                .OrderBy(Grid.GetColumn)
                .ToArray();

            if (modelPane == null || familyAndProperties == null || roomAndSelection == null || splitters.Length < 2)
                return;

            foreach (var splitter in splitters)
            {
                splitter.ShowsPreview = true;
                splitter.Focusable = false;
            }

            void ApplyBreakpoint()
            {
                ApplyWorkspaceBreakpoint(
                    workspace,
                    modelPane,
                    splitters[0],
                    familyAndProperties,
                    splitters[1],
                    roomAndSelection);
            }

            workspace.SizeChanged += (_, __) => ApplyBreakpoint();
            ApplyBreakpoint();
        }

        private static void ApplyWorkspaceBreakpoint(
            Grid workspace,
            FrameworkElement modelPane,
            GridSplitter primarySplitter,
            Grid familyAndProperties,
            GridSplitter secondarySplitter,
            Grid roomAndSelection)
        {
            if (workspace.RowDefinitions.Count < 3 || workspace.ColumnDefinitions.Count < 5)
                return;

            var width = workspace.ActualWidth;
            var narrow = width > 0 && width < 680;

            if (narrow)
            {
                // Two-tier layout for docked palettes: model + family/property on top,
                // room/selection below. This keeps all three work areas visible without
                // a horizontal scroll canvas or clipped controls.
                workspace.RowDefinitions[0].Height = new GridLength(1.25, GridUnitType.Star);
                workspace.RowDefinitions[0].MinHeight = 245;
                workspace.RowDefinitions[1].Height = new GridLength(4);
                workspace.RowDefinitions[1].MinHeight = 0;
                workspace.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
                workspace.RowDefinitions[2].MinHeight = 190;

                workspace.ColumnDefinitions[0].Width = new GridLength(width < 470 ? 132 : 150);
                workspace.ColumnDefinitions[0].MinWidth = width < 470 ? 112 : 125;
                workspace.ColumnDefinitions[1].Width = new GridLength(4);
                workspace.ColumnDefinitions[1].MinWidth = 4;
                workspace.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                workspace.ColumnDefinitions[2].MinWidth = 0;
                workspace.ColumnDefinitions[3].Width = new GridLength(0);
                workspace.ColumnDefinitions[3].MinWidth = 0;
                workspace.ColumnDefinitions[4].Width = new GridLength(0);
                workspace.ColumnDefinitions[4].MinWidth = 0;

                Place(modelPane, 0, 0, 1, 1);
                Place(primarySplitter, 0, 1, 1, 1);
                Place(familyAndProperties, 0, 2, 1, 1);
                Place(secondarySplitter, 1, 0, 1, 3);
                Place(roomAndSelection, 2, 0, 1, 3);

                primarySplitter.ResizeDirection = GridResizeDirection.Columns;
                primarySplitter.Width = 4;
                primarySplitter.Height = double.NaN;
                primarySplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                primarySplitter.VerticalAlignment = VerticalAlignment.Stretch;

                secondarySplitter.ResizeDirection = GridResizeDirection.Rows;
                secondarySplitter.Width = double.NaN;
                secondarySplitter.Height = 4;
                secondarySplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                secondarySplitter.VerticalAlignment = VerticalAlignment.Stretch;

                familyAndProperties.RowDefinitions[0].Height = new GridLength(190);
                familyAndProperties.RowDefinitions[0].MinHeight = 125;
                roomAndSelection.RowDefinitions[0].Height = new GridLength(150);
                roomAndSelection.RowDefinitions[0].MinHeight = 95;
                return;
            }

            // Original three-column desktop arrangement, with sane minimums.
            workspace.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            workspace.RowDefinitions[0].MinHeight = 0;
            workspace.RowDefinitions[1].Height = new GridLength(0);
            workspace.RowDefinitions[1].MinHeight = 0;
            workspace.RowDefinitions[2].Height = new GridLength(0);
            workspace.RowDefinitions[2].MinHeight = 0;

            workspace.ColumnDefinitions[0].Width = new GridLength(165);
            workspace.ColumnDefinitions[0].MinWidth = 145;
            workspace.ColumnDefinitions[1].Width = new GridLength(4);
            workspace.ColumnDefinitions[1].MinWidth = 4;
            workspace.ColumnDefinitions[2].Width = new GridLength(255);
            workspace.ColumnDefinitions[2].MinWidth = 220;
            workspace.ColumnDefinitions[3].Width = new GridLength(4);
            workspace.ColumnDefinitions[3].MinWidth = 4;
            workspace.ColumnDefinitions[4].Width = new GridLength(1, GridUnitType.Star);
            workspace.ColumnDefinitions[4].MinWidth = 190;

            Place(modelPane, 0, 0, 1, 1);
            Place(primarySplitter, 0, 1, 1, 1);
            Place(familyAndProperties, 0, 2, 1, 1);
            Place(secondarySplitter, 0, 3, 1, 1);
            Place(roomAndSelection, 0, 4, 1, 1);

            primarySplitter.ResizeDirection = GridResizeDirection.Columns;
            primarySplitter.Width = 4;
            primarySplitter.Height = double.NaN;
            primarySplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            primarySplitter.VerticalAlignment = VerticalAlignment.Stretch;

            secondarySplitter.ResizeDirection = GridResizeDirection.Columns;
            secondarySplitter.Width = 4;
            secondarySplitter.Height = double.NaN;
            secondarySplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            secondarySplitter.VerticalAlignment = VerticalAlignment.Stretch;

            familyAndProperties.RowDefinitions[0].Height = new GridLength(235);
            familyAndProperties.RowDefinitions[0].MinHeight = 150;
            roomAndSelection.RowDefinitions[0].Height = new GridLength(200);
            roomAndSelection.RowDefinitions[0].MinHeight = 125;
        }

        private static void Place(FrameworkElement element, int row, int column, int rowSpan, int columnSpan)
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
            Grid.SetRowSpan(element, rowSpan);
            Grid.SetColumnSpan(element, columnSpan);
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
                if (text.FontSize < 11.5)
                    text.FontSize = 11.5;
            }
        }

        private void TuneResponsiveHeader()
        {
            var root = ResolveWorkspaceRoot();
            if (root == null)
                return;

            var headerBorder = root.Children
                .OfType<Border>()
                .FirstOrDefault(border => Grid.GetRow(border) == 0);
            if (!(headerBorder?.Child is Grid header) || header.ColumnDefinitions.Count != 3)
                return;

            header.SizeChanged += OnCompactHeaderSizeChanged;
            ApplyCompactHeaderBreakpoint(header);
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
            var ultraNarrow = width > 0 && width < 460;
            var narrow = width > 0 && width < 610;
            var compact = width > 0 && width < 760;

            var workspaceBadge = FindHeaderBadge(branding, "BIM WORKSPACE");
            if (workspaceBadge != null)
                workspaceBadge.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;

            var semanticBadge = FindHeaderBadge(branding, "SEMANTIC MODEL");
            if (semanticBadge != null)
                semanticBadge.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;

            if (status != null)
            {
                status.Visibility = ultraNarrow ? Visibility.Collapsed : Visibility.Visible;
                status.Margin = narrow ? new Thickness(3, 0, 3, 0) : new Thickness(8, 0, 8, 0);
                status.MinWidth = 0;
                status.TextAlignment = TextAlignment.Center;
            }

            branding.Margin = new Thickness(0, 0, narrow ? 3 : 8, 0);
            actions.Margin = new Thickness(narrow ? 1 : 4, 0, 0, 0);

            foreach (var button in actions.Children.OfType<Button>())
            {
                button.Padding = narrow ? new Thickness(4, 2, 4, 2) : new Thickness(6, 2, 6, 2);

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