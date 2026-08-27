using System;
using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Keeps the static Workspace navigation tree out of WPF recycling virtualization.
    ///
    /// ModelTree is not a normal data-bound virtualized list: Project Browser reparents the same
    /// TreeView instance into a TabControl during Workspace Loaded, while the registry augmenter
    /// reorders/reparents the already-created TreeViewItem containers. Recycling virtualization
    /// keeps IContainItemStorage ownership state across those transitions and can make the next
    /// BricsCAD host WM_SIZE layout enter VirtualizingStackPanel.SetVirtualizationState/GetOwners
    /// with inconsistent owners.
    ///
    /// The containment is intentionally local to ModelTree. Data-heavy Family/Property/Inspection
    /// lists and Project Browser result lists continue to inherit the Theme.xaml Recycling contract.
    /// </summary>
    public partial class WorkspacePanel
    {
        internal static readonly bool ModelTreeVirtualizationSafetyRegistered =
            RegisterModelTreeVirtualizationSafety();

        private static bool RegisterModelTreeVirtualizationSafety()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnModelTreeVirtualizationSafetyLoaded),
                true);
            return true;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            ApplyModelTreeVirtualizationSafety(this);
        }

        private static void OnModelTreeVirtualizationSafetyLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel)
                ApplyModelTreeVirtualizationSafety(panel);
        }

        private static void ApplyModelTreeVirtualizationSafety(WorkspacePanel panel)
        {
            if (panel.ModelTree == null)
                return;

            // Apply as soon as the Workspace XAML has initialized so the first measure/layout pass
            // already sees the local non-virtualizing contract. Loaded remains a defensive fallback
            // for later reparenting/reload paths.
            VirtualizingPanel.SetIsVirtualizing(panel.ModelTree, false);
            ScrollViewer.SetCanContentScroll(panel.ModelTree, false);
        }
    }
}
