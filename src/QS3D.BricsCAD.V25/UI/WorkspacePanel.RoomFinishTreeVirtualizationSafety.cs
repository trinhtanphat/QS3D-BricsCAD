using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        /// <summary>
        /// The HT_PHÒNG finish tree is a small static navigation tree. It must never inherit
        /// the global Recycling policy after its ItemsHost has already measured. Hooking the
        /// root Content assignment keeps this containment inside InitializeComponent, before
        /// the host can perform first layout, while leaving data-heavy TreeView/ListView/ListBox
        /// surfaces on Theme.xaml's normal Recycling policy.
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == ContentControl.ContentProperty && e.NewValue is DependencyObject content)
                ApplyRoomFinishTreeVirtualizationSafetyPreLayout(content);
        }

        private static void ApplyRoomFinishTreeVirtualizationSafetyPreLayout(DependencyObject root)
        {
            var tree = FindRoomFinishTree(root);
            if (tree == null) return;

            // Local values win over the implicit Theme.xaml TreeView Recycling setters. The
            // mode is established before first Measure and must never be rewritten by Loaded.
            if (tree.ReadLocalValue(VirtualizingPanel.VirtualizationModeProperty) == DependencyProperty.UnsetValue)
                VirtualizingPanel.SetVirtualizationMode(tree, VirtualizationMode.Standard);

            VirtualizingPanel.SetIsVirtualizing(tree, false);
            ScrollViewer.SetCanContentScroll(tree, false);

            // Materialize the final static item set before first Measure. The historical
            // double-SystemIdle presentation path checks for this item and therefore becomes
            // a no-op instead of mutating an already measured TreeView.
            EnsureRoomFinishStaticItemsPreLayout(tree);
        }

        private static TreeView? FindRoomFinishTree(DependencyObject root)
        {
            if (root is TreeView candidate && IsRoomFinishTree(candidate)) return candidate;

            foreach (var child in LogicalTreeHelper.GetChildren(root))
            {
                if (!(child is DependencyObject dependencyChild)) continue;
                var nested = FindRoomFinishTree(dependencyChild);
                if (nested != null) return nested;
            }

            return null;
        }

        private static bool IsRoomFinishTree(TreeView tree)
        {
            var hasFloorFinish = false;
            var hasWaterproofing = false;
            var hasWallFinish = false;
            var hasCeilingFinish = false;

            foreach (var item in tree.Items)
            {
                if (!(item is TreeViewItem treeItem) || !(treeItem.Tag is string tag)) continue;
                if (string.Equals(tag, ElementCategory.FloorFinish.ToString(), StringComparison.OrdinalIgnoreCase))
                    hasFloorFinish = true;
                else if (string.Equals(tag, ElementCategory.Waterproofing.ToString(), StringComparison.OrdinalIgnoreCase))
                    hasWaterproofing = true;
                else if (string.Equals(tag, ElementCategory.WallFinish.ToString(), StringComparison.OrdinalIgnoreCase))
                    hasWallFinish = true;
                else if (string.Equals(tag, ElementCategory.CeilingFinish.ToString(), StringComparison.OrdinalIgnoreCase))
                    hasCeilingFinish = true;
            }

            return hasFloorFinish && hasWaterproofing && hasWallFinish && hasCeilingFinish;
        }

        private static void EnsureRoomFinishStaticItemsPreLayout(TreeView tree)
        {
            foreach (var item in tree.Items)
            {
                if (item is TreeViewItem treeItem &&
                    string.Equals(treeItem.Header as string, "Trát Trần", StringComparison.OrdinalIgnoreCase))
                    return;
            }

            tree.Items.Add(new TreeViewItem
            {
                Header = "Trát Trần",
                Tag = ElementCategory.CeilingFinish.ToString()
            });
        }
    }
}