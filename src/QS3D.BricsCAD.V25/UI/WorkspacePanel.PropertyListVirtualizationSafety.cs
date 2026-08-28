using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Keeps the grouped Workspace property list out of recycling virtualization before the
    /// CollectionView receives its GroupDescriptions and before BricsCAD performs first layout.
    ///
    /// WPF's grouped ListView creates nested item owners. In the BricsCAD V25 host, recycling
    /// that owner graph can reach VirtualizingStackPanel.SetVirtualizationState during WM_SIZE
    /// with inconsistent IContainItemStorage ownership and terminate the process. This exception
    /// is intentionally limited to PropertyList; the ungrouped family and inspection lists retain
    /// the shared Theme.xaml recycling policy.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static void ApplyPropertyListVirtualizationSafety(WorkspacePanel panel)
        {
            if (panel.PropertyList == null)
                return;

            VirtualizingPanel.SetVirtualizationMode(panel.PropertyList, VirtualizationMode.Standard);
            VirtualizingPanel.SetIsVirtualizing(panel.PropertyList, false);
            ScrollViewer.SetCanContentScroll(panel.PropertyList, false);
        }
    }
}
