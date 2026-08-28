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
    ///
    /// IMPORTANT: these attached properties are written only from the Workspace constructor,
    /// immediately after InitializeComponent and before first host layout. Dependency-property local
    /// values stay with the same TreeView instance when Project Browser reparents it, so Loaded-time
    /// reassertion is both unnecessary and unsafe: WPF can already have measured an ItemsHost and a
    /// late virtualization write can re-enter VirtualizingStackPanel.SetVirtualizationState.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static void ApplyModelTreeVirtualizationSafety(WorkspacePanel panel)
        {
            if (panel.ModelTree == null)
                return;

            VirtualizingPanel.SetVirtualizationMode(panel.ModelTree, VirtualizationMode.Standard);
            VirtualizingPanel.SetIsVirtualizing(panel.ModelTree, false);
            ScrollViewer.SetCanContentScroll(panel.ModelTree, false);
        }
    }
}
