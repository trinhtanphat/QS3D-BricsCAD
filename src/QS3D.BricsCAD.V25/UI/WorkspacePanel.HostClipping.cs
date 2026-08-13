using System.Windows;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Host-boundary guard for BricsCAD PaletteSet embedding. The Workspace intentionally
    /// keeps a wider logical content surface for horizontal overflow, so descendant rendering
    /// must be clipped explicitly to the actual WPF host and ScrollViewer viewport.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static bool HostClippingClassHandlerRegistered { get; } = RegisterHostClippingClassHandler();

        private static bool RegisterHostClippingClassHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnHostClippingLoaded),
                true);
            return true;
        }

        private static void OnHostClippingLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel)
                panel.ApplyHostClippingBoundary();
        }

        private void ApplyHostClippingBoundary()
        {
            ClipToBounds = true;
            WorkspaceOverflow.ClipToBounds = true;
        }
    }
}
