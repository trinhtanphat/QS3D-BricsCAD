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
            if (!(Content is Grid root)) return;

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

            UserUiLayoutStore.Update(layout =>
            {
                layout.ModelColumnWidth = modelWidth;
                layout.FamilyColumnWidth = familyWidth;
                layout.FamilyTopHeight = familyTop;
                layout.RoomTopHeight = roomTop;
            });
        }
    }
}
