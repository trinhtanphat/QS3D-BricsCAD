using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Keeps the static Workspace navigation tree out of WPF recycling virtualization and moves it
    /// into its final Project Browser host before WPF can perform the first host layout.
    ///
    /// ModelTree is not a normal data-bound virtualized list: the registry augmenter reorders the
    /// already-created TreeViewItem containers, while Project Browser hosts the same TreeView instance
    /// inside a TabControl. Recycling virtualization keeps IContainItemStorage ownership state across
    /// those transitions and can make a later BricsCAD host WM_SIZE layout enter
    /// VirtualizingStackPanel.SetVirtualizationState/GetOwners with inconsistent owners.
    ///
    /// The containment is intentionally local to ModelTree. Data-heavy Family/Property/Inspection
    /// lists and Project Browser result lists continue to inherit the Theme.xaml Recycling contract.
    ///
    /// IMPORTANT: the attached properties and the one-time ModelTree reparent are completed only from
    /// the Workspace constructor, immediately after InitializeComponent and before first host layout.
    /// Dependency-property local values stay with the same TreeView instance in the TabControl.
    /// Loaded-time EnsureProjectBrowserSurface calls remain idempotent because _browserTabs is already
    /// established; they must never become the first mutation/reparent path again.
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
            panel.EnsureProjectBrowserSurface();
        }
    }
}
