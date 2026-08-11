using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RightPanel
    {
        private void OnDrawingListPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindRightPanelContainer<ListViewItem>(DrawingList, e.OriginalSource as DependencyObject);
            if (item == null)
            {
                DrawingList.UnselectAll();
                return;
            }
            item.IsSelected = true;
            item.Focus();
        }

        private void OnLayerListPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindRightPanelContainer<ListViewItem>(LayerList, e.OriginalSource as DependencyObject);
            if (item == null) return;

            // Preserve an existing Ctrl/Shift multi-selection when the user right-clicks one
            // of its rows. Right-clicking a new row intentionally makes that row the target.
            if (!item.IsSelected)
            {
                LayerList.UnselectAll();
                item.IsSelected = true;
            }
            item.Focus();
        }

        private static T? FindRightPanelContainer<T>(ItemsControl owner, DependencyObject? source)
            where T : DependencyObject
        {
            if (owner == null || source == null) return null;
            var current = source;
            while (current != null && !ReferenceEquals(current, owner))
            {
                if (current is T typed) return typed;
                current = RightPanelParentOf(current);
            }
            return null;
        }

        private static DependencyObject? RightPanelParentOf(DependencyObject child)
        {
            if (child is ContentElement content)
                return ContentOperations.GetParent(content) ?? (content as FrameworkContentElement)?.Parent;
            return VisualTreeHelper.GetParent(child);
        }
    }
}
