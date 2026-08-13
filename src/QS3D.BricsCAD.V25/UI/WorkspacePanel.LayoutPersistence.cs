using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using QS3D.BricsCAD.V25.Services;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private bool _layoutPersistenceAttached;
        private Grid? _layoutMainGrid;
        private Grid? _layoutFamilyGrid;
        private Grid? _layoutRoomGrid;

        private void AttachLayoutPersistence()
        {
            if (_layoutPersistenceAttached) return;
            var root = WorkspaceContentRoot;
            if (root == null) return;

            var main = root.Children
                .OfType<Grid>()
                .FirstOrDefault(x => Grid.GetRow(x) == 1 && x.ColumnDefinitions.Count >= 5);
            if (main == null) return;

            var family = main.Children
                .OfType<Grid>()
                .FirstOrDefault(x => Grid.GetColumn(x) == 2 && x.RowDefinitions.Count >= 3);
            var room = main.Children
                .OfType<Grid>()
                .FirstOrDefault(x => Grid.GetColumn(x) == 4 && x.RowDefinitions.Count >= 3);
            if (family == null || room == null) return;

            _layoutMainGrid = main;
            _layoutFamilyGrid = family;
            _layoutRoomGrid = room;

            var layout = UserUiLayoutStore.Get();
            main.ColumnDefinitions[0].Width = new GridLength(layout.ModelColumnWidth, GridUnitType.Pixel);
            main.ColumnDefinitions[2].Width = new GridLength(layout.FamilyColumnWidth, GridUnitType.Pixel);
            family.RowDefinitions[0].Height = new GridLength(layout.FamilyTopHeight, GridUnitType.Pixel);
            room.RowDefinitions[0].Height = new GridLength(layout.RoomTopHeight, GridUnitType.Pixel);

            foreach (var splitter in main.Children.OfType<GridSplitter>()) splitter.DragCompleted += OnLayoutSplitterDragCompleted;
            foreach (var splitter in family.Children.OfType<GridSplitter>()) splitter.DragCompleted += OnLayoutSplitterDragCompleted;
            foreach (var splitter in room.Children.OfType<GridSplitter>()) splitter.DragCompleted += OnLayoutSplitterDragCompleted;
            _layoutPersistenceAttached = true;
        }

        private void OnLayoutSplitterDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (_layoutMainGrid == null || _layoutFamilyGrid == null || _layoutRoomGrid == null) return;

            var modelWidth = _layoutMainGrid.ColumnDefinitions[0].ActualWidth;
            var familyWidth = _layoutMainGrid.ColumnDefinitions[2].ActualWidth;
            var familyTop = _layoutFamilyGrid.RowDefinitions[0].ActualHeight;
            var roomTop = _layoutRoomGrid.RowDefinitions[0].ActualHeight;
            var splitterGrid = (sender as FrameworkElement)?.Parent as Grid;
            var narrowResponsiveLayout = _layoutMainGrid.ActualWidth > 0 && _layoutMainGrid.ActualWidth < 680;

            UserUiLayoutStore.Update(layout =>
            {
                if (!narrowResponsiveLayout)
                {
                    layout.ModelColumnWidth = modelWidth;
                    layout.FamilyColumnWidth = familyWidth;
                    layout.FamilyTopHeight = familyTop;
                    layout.RoomTopHeight = roomTop;
                    return;
                }

                // At narrow widths CompactShell temporarily reflows the desktop 3-column grid
                // into two tiers. Those transient column widths must never replace the user's
                // saved desktop layout. Only persist a vertical split that the user actually
                // dragged inside the Family/Properties or Room/Selection pane.
                if (ReferenceEquals(splitterGrid, _layoutFamilyGrid))
                    layout.FamilyTopHeight = familyTop;
                else if (ReferenceEquals(splitterGrid, _layoutRoomGrid))
                    layout.RoomTopHeight = roomTop;
            });
        }
    }
}
