using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Keeps the grouped Workspace property list out of recycling virtualization before the
    /// CollectionView receives its GroupDescriptions and before BricsCAD performs first layout.
    ///
    /// Grouping creates nested WPF item owners. In the V25 palette host, recycling those owners
    /// can make a later WM_SIZE layout enter VirtualizingStackPanel.GetOwners with inconsistent
    /// IContainItemStorage state. This containment is intentionally local to PropertyList; normal
    /// data-heavy lists continue to inherit the shared Theme.xaml recycling policy.
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
