using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Responsive presentation pass for recognition review chrome only.
    /// Recognition logic and review commands remain owned by the existing window.
    /// </summary>
    public partial class RecognitionWindow
    {
        private static bool RecognitionCompactShellRegistered { get; } = RegisterRecognitionCompactShell();
        private bool _recognitionCompactShellApplied;

        private static bool RegisterRecognitionCompactShell()
        {
            EventManager.RegisterClassHandler(
                typeof(RecognitionWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnRecognitionCompactShellLoaded),
                true);
            return true;
        }

        private static void OnRecognitionCompactShellLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is RecognitionWindow window)
                window.ApplyRecognitionCompactShell();
        }

        private void ApplyRecognitionCompactShell()
        {
            if (_recognitionCompactShellApplied)
                return;

            _recognitionCompactShellApplied = true;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            if (!(Content is Grid root))
                return;

            TuneRecognitionHeader(root);
            TuneRecognitionFooter(root);
        }

        private void TuneRecognitionHeader(Grid root)
        {
            var headerBorder = root.Children
                .OfType<Border>()
                .FirstOrDefault(border => Grid.GetRow(border) == 0 && border.Child is DockPanel);
            if (!(headerBorder?.Child is DockPanel header) || header.Children.Count < 2)
                return;

            var titleStack = header.Children[0] as StackPanel;
            var actions = header.Children[header.Children.Count - 1] as StackPanel;
            if (titleStack == null || actions == null)
                return;

            header.LastChildFill = false;
            DockPanel.SetDock(titleStack, Dock.Left);
            DockPanel.SetDock(actions, Dock.Right);
            titleStack.MinWidth = 0;
            titleStack.Margin = new Thickness(0, 0, 10, 0);
            actions.HorizontalAlignment = HorizontalAlignment.Right;
            actions.VerticalAlignment = VerticalAlignment.Center;

            var subtitle = titleStack.Children.OfType<TextBlock>().LastOrDefault();
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

                var actionsWidth = Math.Max(actions.ActualWidth, actions.DesiredSize.Width);
                titleStack.MaxWidth = Math.Max(220, header.ActualWidth - actionsWidth - 10);

                var compact = header.ActualWidth < 980;
                if (reviewBadge != null)
                    reviewBadge.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;

                foreach (var button in actions.Children.OfType<Button>())
                    button.Padding = compact ? new Thickness(5, 2, 5, 2) : new Thickness(8, 3, 8, 3);
            }

            header.SizeChanged += (_, __) => ApplyHeaderBreakpoint();
            actions.SizeChanged += (_, __) => ApplyHeaderBreakpoint();
            ApplyHeaderBreakpoint();
        }

        private void TuneRecognitionFooter(Grid root)
        {
            var footerBorder = root.Children
                .OfType<Border>()
                .FirstOrDefault(border => Grid.GetRow(border) == 2 && border.Child is DockPanel);
            if (!(footerBorder?.Child is DockPanel footer))
                return;

            var status = footer.Children.OfType<TextBlock>().FirstOrDefault(text => ReferenceEquals(text, Status));
            var reviewHint = footer.Children.OfType<TextBlock>().FirstOrDefault(text => !ReferenceEquals(text, Status));
            if (status == null || reviewHint == null)
                return;

            footer.LastChildFill = false;
            DockPanel.SetDock(status, Dock.Left);
            DockPanel.SetDock(reviewHint, Dock.Right);
            status.MinWidth = 0;
            status.TextTrimming = TextTrimming.CharacterEllipsis;
            status.ToolTip = status.Text;
            reviewHint.HorizontalAlignment = HorizontalAlignment.Right;

            void ApplyFooterBreakpoint()
            {
                if (footer.ActualWidth <= 0)
                    return;

                var hintWidth = Math.Max(reviewHint.ActualWidth, reviewHint.DesiredSize.Width);
                status.MaxWidth = Math.Max(160, footer.ActualWidth - hintWidth - 28);
                reviewHint.Visibility = footer.ActualWidth < 720 ? Visibility.Collapsed : Visibility.Visible;
            }

            footer.SizeChanged += (_, __) => ApplyFooterBreakpoint();
            reviewHint.SizeChanged += (_, __) => ApplyFooterBreakpoint();
            ApplyFooterBreakpoint();
        }
    }
}
