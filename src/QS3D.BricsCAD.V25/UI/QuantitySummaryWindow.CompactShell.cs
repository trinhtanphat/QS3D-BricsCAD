using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only responsive tuning for the quantity summary window.
    /// Existing bindings and command handlers remain unchanged.
    /// </summary>
    public partial class QuantitySummaryWindow
    {
        private static bool QuantitySummaryCompactShellRegistered { get; } = RegisterQuantitySummaryCompactShell();
        private bool _quantitySummaryCompactShellApplied;

        private static bool RegisterQuantitySummaryCompactShell()
        {
            EventManager.RegisterClassHandler(
                typeof(QuantitySummaryWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnQuantitySummaryCompactShellLoaded),
                true);
            return true;
        }

        private static void OnQuantitySummaryCompactShellLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is QuantitySummaryWindow window)
                window.ApplyQuantitySummaryCompactShell();
        }

        private void ApplyQuantitySummaryCompactShell()
        {
            if (_quantitySummaryCompactShellApplied)
                return;

            _quantitySummaryCompactShellApplied = true;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            TuneQuantitySummaryHeader();
            TuneQuantitySummaryFilterBar();
        }

        private void TuneQuantitySummaryHeader()
        {
            if (!(Content is Grid root))
                return;

            var headerBorder = root.Children
                .OfType<Border>()
                .FirstOrDefault(border => Grid.GetRow(border) == 0 && border.Child is DockPanel);
            if (!(headerBorder?.Child is DockPanel header) || header.Children.Count < 2)
                return;

            var titleStack = header.Children[0] as StackPanel;
            var actions = header.Children[header.Children.Count - 1] as StackPanel;
            if (titleStack == null || actions == null)
                return;

            // With LastChildFill=True the final action strip ignores Dock=Right. Combined with
            // the long Vietnamese subtitle, the two regions can consume the same visual space
            // near the window's 1020 px minimum width.
            header.LastChildFill = false;
            DockPanel.SetDock(titleStack, Dock.Left);
            DockPanel.SetDock(actions, Dock.Right);
            titleStack.MinWidth = 0;
            titleStack.Margin = new Thickness(0, 0, 12, 0);
            actions.HorizontalAlignment = HorizontalAlignment.Right;
            actions.VerticalAlignment = VerticalAlignment.Center;

            var subtitle = titleStack.Children
                .OfType<TextBlock>()
                .LastOrDefault();
            if (subtitle != null)
            {
                subtitle.TextWrapping = TextWrapping.NoWrap;
                subtitle.TextTrimming = TextTrimming.CharacterEllipsis;
                subtitle.ToolTip = subtitle.Text;
            }

            Border? reviewBadge = null;
            var titleRow = titleStack.Children.OfType<StackPanel>().FirstOrDefault();
            if (titleRow != null)
                reviewBadge = titleRow.Children.OfType<Border>().FirstOrDefault();

            void ApplyHeaderBreakpoint()
            {
                if (header.ActualWidth <= 0)
                    return;

                var actionWidth = Math.Max(actions.ActualWidth, actions.DesiredSize.Width);
                titleStack.MaxWidth = Math.Max(250, header.ActualWidth - actionWidth - 12);

                var compact = header.ActualWidth < 1160;
                if (reviewBadge != null)
                    reviewBadge.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;

                foreach (var button in actions.Children.OfType<Button>())
                    button.Padding = compact ? new Thickness(5, 2, 5, 2) : new Thickness(6, 2, 6, 2);
            }

            header.SizeChanged += (_, __) => ApplyHeaderBreakpoint();
            actions.SizeChanged += (_, __) => ApplyHeaderBreakpoint();
            ApplyHeaderBreakpoint();
        }

        private void TuneQuantitySummaryFilterBar()
        {
            var filterGrid = VisualTreeHelper.GetParent(FloorCombo) as Grid;
            if (filterGrid == null || filterGrid.ColumnDefinitions.Count < 7)
                return;

            void ApplyFilterBreakpoint()
            {
                var compact = filterGrid.ActualWidth > 0 && filterGrid.ActualWidth < 1120;

                filterGrid.ColumnDefinitions[1].Width = new GridLength(compact ? 125 : 150);
                filterGrid.ColumnDefinitions[4].Width = new GridLength(compact ? 190 : 250);

                DetailModeRadio.Content = compact ? "Chi tiết" : "Diễn giải chi tiết";
                DetailModeRadio.Margin = compact ? new Thickness(8, 0, 0, 0) : new Thickness(12, 0, 0, 0);
                AutoRevealCheck.Margin = compact ? new Thickness(8, 0, 0, 0) : new Thickness(12, 0, 0, 0);
            }

            filterGrid.SizeChanged += (_, __) => ApplyFilterBreakpoint();
            ApplyFilterBreakpoint();
        }
    }
}
