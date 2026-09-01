using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Keeps the grouped Workspace property list out of recycling virtualization before
    /// CollectionView grouping and before BricsCAD performs the first host layout.
    ///
    /// PropertyList is a small grouped ListView. In BricsCAD V25, allowing the shared Recycling
    /// policy to create nested grouped item owners during the first WM_SIZE/layout can enter
    /// VirtualizingStackPanel.SetVirtualizationState/GetOwners with inconsistent ownership.
    /// Keep this containment local so normal data-heavy lists retain the shared Theme policy.
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
