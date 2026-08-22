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
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            TuneRightPanelGrid();
            TuneRightPanelLists();
            TuneRightPanelActions();
            TuneRightPanelSectionHierarchy();
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

            LayerSearchBox.MinHeight = 24;
            LayerSearchBox.Padding = new Thickness(5, 1, 5, 1);

            AppendRightShortcutHint(LayerSearchBox, "Ctrl+F");
        }

        private void TuneRightPanelActions()
        {
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
                if (text.FontSize < 11)
                    text.FontSize = 11;
            }
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

            if (!string.IsNullOrWhiteSpace(current) &&
                current.IndexOf(shortcut, StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            element.ToolTip = string.IsNullOrWhiteSpace(current)
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
