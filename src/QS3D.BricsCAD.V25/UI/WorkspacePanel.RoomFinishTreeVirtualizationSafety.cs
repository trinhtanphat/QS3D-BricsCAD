using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private const string RoomFinishTreeIdentity = "RoomFinishTree";

        /// <summary>
        /// The HT_PHÒNG finish tree is a small static navigation tree. Its complete item set,
        /// explicit identity and local virtualization contract must exist while InitializeComponent
        /// is assigning the root Content, before the first ItemsHost Measure can occur.
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == ContentControl.ContentProperty && e.NewValue is DependencyObject content)
                ApplyRoomFinishTreeVirtualizationSafetyPreLayout(content);
        }

        private static void ApplyRoomFinishTreeVirtualizationSafetyPreLayout(DependencyObject root)
        {
            var tree = FindSingleRoomFinishTree(root);
            if (tree == null) return;

            // The owner is identified exactly once, before first layout. A future second structural
            // match is a source-contract error instead of silently selecting the first TreeView.
            if (string.IsNullOrEmpty(tree.Name))
                tree.Name = RoomFinishTreeIdentity;
            else if (!string.Equals(tree.Name, RoomFinishTreeIdentity, StringComparison.Ordinal))
                throw new InvalidOperationException("Room finish TreeView has an unexpected pre-layout identity: " + tree.Name);

            // Local values win over Theme.xaml's global Recycling policy. Never write any of these
            // attached properties from Loaded/SystemIdle code after the ItemsHost has measured.
            if (tree.ReadLocalValue(VirtualizingPanel.VirtualizationModeProperty) == DependencyProperty.UnsetValue)
                VirtualizingPanel.SetVirtualizationMode(tree, VirtualizationMode.Standard);

            VirtualizingPanel.SetIsVirtualizing(tree, false);
            ScrollViewer.SetCanContentScroll(tree, false);

            // Materialize the final static item set before first Measure. Loaded/SystemIdle
            // presentation code is intentionally forbidden from mutating this TreeView.
            EnsureRoomFinishStaticItemsPreLayout(tree);
        }

        private static TreeView? FindSingleRoomFinishTree(DependencyObject root)
        {
            TreeView? match = null;
            FindRoomFinishTrees(root, ref match);
            return match;
        }

        private static void FindRoomFinishTrees(DependencyObject root, ref TreeView? match)
        {
            if (root is TreeView candidate && IsRoomFinishTree(candidate))
            {
                if (match != null && !ReferenceEquals(match, candidate))
                    throw new InvalidOperationException("Workspace contains more than one Room finish TreeView owner before first layout.");
                match = candidate;
            }

            foreach (var child in LogicalTreeHelper.GetChildren(root))
            {
                if (child is DependencyObject dependencyChild)
                    FindRoomFinishTrees(dependencyChild, ref match);
            }
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
