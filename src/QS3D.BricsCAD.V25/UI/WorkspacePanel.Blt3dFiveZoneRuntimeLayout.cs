using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Final runtime layout pass for the owner-approved BLT3D-style workspace.
    ///
    /// The QS3D Workspace palette stays left of the host-owned BricsCAD modelspace and presents
    /// two adjacent plugin columns: Model/Zone/Floor, then Family with the authoritative embedded
    /// Properties editor. The right-side Drawing/Layer palette is a separate native PaletteSet.
    /// A dedicated Properties PaletteSet remains available as an opt-in isolated surface, but the
    /// default BIM reference layout does not reparent the editor out of the Workspace.
    ///
    /// Run at SystemIdle so this pass wins after older CompactShell/reference compatibility passes
    /// that still rewrite the same five-column Grid during Loaded.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool Blt3dFiveZoneRuntimeLayoutRegistered = RegisterBlt3dFiveZoneRuntimeLayout();
        private GridSplitter? _blt3dRuntimeColumnSplitter;

        // WorkspacePanel.CompactShell.cs already owns the type's single explicit static constructor.
        // That constructor removes beforefieldinit for the complete partial type, so this field
        // initializer and the runtime-repair registration execute deterministically before the
        // first WorkspacePanel instance without declaring a duplicate static constructor here.
        private static bool RegisterBlt3dFiveZoneRuntimeLayout()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBlt3dFiveZoneRuntimeLayoutLoaded),
                true);
            return true;
        }

        private static void OnBlt3dFiveZoneRuntimeLayoutLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !Blt3dFiveZoneRuntimeLayoutRegistered)
                return;

            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.SystemIdle,
                new Action(panel.ApplyBlt3dFiveZoneRuntimeLayout));
        }

        private void ApplyBlt3dFiveZoneRuntimeLayout()
        {
            var root = WorkspaceContentRoot;
            if (root == null)
                return;

            var workspace = root.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate =>
                    Grid.GetRow(candidate) == 1 &&
                    candidate.ColumnDefinitions.Count == 5);
            if (workspace == null)
                return;

            var modelPane = workspace.Children
                .OfType<Border>()
                .FirstOrDefault(child => Grid.GetColumn(child) == 0);

            var familyPane = workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(child => IsVisualDescendant(child, FamilyList));

            var columnSplitter = _blt3dRuntimeColumnSplitter;
            if (columnSplitter == null || !ReferenceEquals(columnSplitter.Parent, workspace))
            {
                columnSplitter = workspace.Children
                    .OfType<GridSplitter>()
                    .FirstOrDefault(child => Grid.GetColumn(child) == 1);
                _blt3dRuntimeColumnSplitter = columnSplitter;
            }

            if (modelPane == null || familyPane == null || columnSplitter == null)
                return;

            root.MinWidth = 0;
            WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            WorkspaceOverflow.ScrollToHorizontalOffset(0);

            // Match the owner screenshot inside the QS3D palette: narrow model/navigation column,
            // slim splitter, then a wider Family/Properties column. Retired legacy columns stay off.
            var columns = workspace.ColumnDefinitions;
            columns[0].MinWidth = 160;
            columns[0].MaxWidth = double.PositiveInfinity;
            columns[0].Width = new GridLength(38, GridUnitType.Star);
            columns[1].MinWidth = 4;
            columns[1].MaxWidth = 4;
            columns[1].Width = new GridLength(4);
            columns[2].MinWidth = 250;
            columns[2].MaxWidth = double.PositiveInfinity;
            columns[2].Width = new GridLength(62, GridUnitType.Star);
            for (var index = 3; index < columns.Count; index++)
            {
                columns[index].MinWidth = 0;
                columns[index].MaxWidth = 0;
                columns[index].Width = new GridLength(0);
            }

            workspace.RowDefinitions.Clear();
            workspace.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(1, GridUnitType.Star),
                MinHeight = 250
            });

            foreach (UIElement child in workspace.Children)
            {
                child.Visibility = ReferenceEquals(child, modelPane) ||
                                   ReferenceEquals(child, familyPane) ||
                                   ReferenceEquals(child, columnSplitter)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            Grid.SetColumn(modelPane, 0);
            Grid.SetRow(modelPane, 0);
            Grid.SetColumnSpan(modelPane, 1);

            Grid.SetColumn(columnSplitter, 1);
            Grid.SetRow(columnSplitter, 0);
            Grid.SetColumnSpan(columnSplitter, 1);
            columnSplitter.Width = 4;
            columnSplitter.Height = double.NaN;
            columnSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            columnSplitter.VerticalAlignment = VerticalAlignment.Stretch;
            columnSplitter.ResizeDirection = GridResizeDirection.Columns;
            columnSplitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;

            Grid.SetColumn(familyPane, 2);
            Grid.SetRow(familyPane, 0);
            Grid.SetColumnSpan(familyPane, 1);

            if (familyPane.RowDefinitions.Count >= 3)
            {
                if (_dedicatedPropertiesPaletteActive)
                {
                    familyPane.RowDefinitions[0].MinHeight = 100;
                    familyPane.RowDefinitions[0].MaxHeight = double.PositiveInfinity;
                    familyPane.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                    familyPane.RowDefinitions[1].MinHeight = 0;
                    familyPane.RowDefinitions[1].MaxHeight = 0;
                    familyPane.RowDefinitions[1].Height = new GridLength(0);
                    familyPane.RowDefinitions[2].MinHeight = 0;
                    familyPane.RowDefinitions[2].MaxHeight = 0;
                    familyPane.RowDefinitions[2].Height = new GridLength(0);

                    foreach (var splitter in familyPane.Children.OfType<GridSplitter>()
                                 .Where(child => Grid.GetRow(child) == 1))
                        splitter.Visibility = Visibility.Collapsed;

                    FamilyList.MinHeight = 100;
                    PropertyList.MinHeight = 0;
                }
                else
                {
                    familyPane.RowDefinitions[0].MinHeight = 100;
                    familyPane.RowDefinitions[0].MaxHeight = double.PositiveInfinity;
                    familyPane.RowDefinitions[0].Height = new GridLength(56, GridUnitType.Star);
                    familyPane.RowDefinitions[1].MinHeight = 4;
                    familyPane.RowDefinitions[1].MaxHeight = 4;
                    familyPane.RowDefinitions[1].Height = new GridLength(4);
                    familyPane.RowDefinitions[2].MinHeight = 120;
                    familyPane.RowDefinitions[2].MaxHeight = double.PositiveInfinity;
                    familyPane.RowDefinitions[2].Height = new GridLength(44, GridUnitType.Star);

                    foreach (var splitter in familyPane.Children.OfType<GridSplitter>()
                                 .Where(child => Grid.GetRow(child) == 1))
                        splitter.Visibility = Visibility.Visible;

                    FamilyList.MinHeight = 100;
                    PropertyList.MinHeight = 120;
                }
            }
        }
    }
}