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
        }

        private void TuneWorkspaceGrid()
        {
            if (!(Content is Grid root) || root.RowDefinitions.Count < 3)
                return;

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

            workspace.ColumnDefinitions[0].Width = new GridLength(165);
            workspace.ColumnDefinitions[0].MinWidth = 145;
            workspace.ColumnDefinitions[1].Width = new GridLength(4);
            workspace.ColumnDefinitions[2].Width = new GridLength(255);
            workspace.ColumnDefinitions[2].MinWidth = 220;
            workspace.ColumnDefinitions[3].Width = new GridLength(4);
            workspace.ColumnDefinitions[4].Width = new GridLength(1, GridUnitType.Star);
            workspace.ColumnDefinitions[4].MinWidth = 190;

            var familyAndProperties = workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate => Grid.GetColumn(candidate) == 2 && candidate.RowDefinitions.Count == 3);
            if (familyAndProperties != null)
            {
                familyAndProperties.RowDefinitions[0].Height = new GridLength(235);
                familyAndProperties.RowDefinitions[0].MinHeight = 150;
            }

            var roomAndSelection = workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate => Grid.GetColumn(candidate) == 4 && candidate.RowDefinitions.Count == 3);
            if (roomAndSelection != null)
            {
                roomAndSelection.RowDefinitions[0].Height = new GridLength(200);
                roomAndSelection.RowDefinitions[0].MinHeight = 125;
            }

            foreach (var splitter in workspace.Children.OfType<GridSplitter>())
            {
                splitter.ShowsPreview = true;
                splitter.Focusable = false;
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
            if (!(Content is Grid root))
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
                status.Margin = narrow ? new Thickness(4, 0) : new Thickness(8, 0);
                status.MinWidth = 0;
                status.TextAlignment = TextAlignment.Center;
            }

            branding.Margin = new Thickness(0, 0, narrow ? 4 : 8, 0);
            actions.Margin = new Thickness(narrow ? 2 : 4, 0, 0, 0);

            foreach (var button in actions.Children.OfType<Button>())
            {
                button.Padding = narrow ? new Thickness(5, 2) : new Thickness(6, 2);

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

            if (!string.IsNullOrWhiteSpace(current) &&
                current.IndexOf(shortcut, StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            element.ToolTip = string.IsNullOrWhiteSpace(current)
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
