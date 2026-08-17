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
    /// Workspace keeps Model/Zone/Floor + Family/Type on the left. Ordinary ShowWorkspace also
    /// keeps its historical embedded Properties editor. Coordinated BIM mode reparents that exact
    /// editor into a distinct native QS3D Properties PaletteSet, so native BricsCAD Properties can
    /// never be mistaken for the plugin-owned property surface.
    ///
    /// Run at SystemIdle so this pass wins after the older CompactShell / reference presentation
    /// compatibility passes that still rewrite the same five-column Grid during Loaded.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool Blt3dFiveZoneRuntimeLayoutRegistered = RegisterBlt3dFiveZoneRuntimeLayout();
        private GridSplitter? _blt3dRuntimeVerticalSplitter;

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

            // Properties can move between this grid and the dedicated native palette. FamilyList
            // remains in the owner grid in both modes, so it is the stable idempotent rediscovery key.
            var familyPane = workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(child => IsVisualDescendant(child, FamilyList));

            // The first pass moves the original column-1 splitter into row 1/column 0. Keep the
            // exact splitter instance for later settle passes; rediscover only after recreation.
            var verticalSplitter = _blt3dRuntimeVerticalSplitter;
            if (verticalSplitter == null || !ReferenceEquals(verticalSplitter.Parent, workspace))
            {
                verticalSplitter = workspace.Children
                    .OfType<GridSplitter>()
                    .FirstOrDefault(child => Grid.GetColumn(child) == 1);
                _blt3dRuntimeVerticalSplitter = verticalSplitter;
            }

            if (modelPane == null || familyPane == null || verticalSplitter == null)
                return;

            root.MinWidth = 0;
            WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            WorkspaceOverflow.ScrollToHorizontalOffset(0);

            for (var index = 0; index < workspace.ColumnDefinitions.Count; index++)
            {
                var column = workspace.ColumnDefinitions[index];
                column.MinWidth = 0;
                column.MaxWidth = index == 0 ? double.PositiveInfinity : 0;
                column.Width = index == 0
                    ? new GridLength(1, GridUnitType.Star)
                    : new GridLength(0);
            }

            workspace.RowDefinitions.Clear();
            workspace.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(_dedicatedPropertiesPaletteActive ? 62 : 45, GridUnitType.Star),
                MinHeight = 130
            });
            workspace.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(4),
                MinHeight = 4,
                MaxHeight = 4
            });
            workspace.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(_dedicatedPropertiesPaletteActive ? 38 : 55, GridUnitType.Star),
                MinHeight = 120
            });

            foreach (UIElement child in workspace.Children)
            {
                child.Visibility = ReferenceEquals(child, modelPane) ||
                                   ReferenceEquals(child, familyPane) ||
                                   ReferenceEquals(child, verticalSplitter)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            Grid.SetColumn(modelPane, 0);
            Grid.SetRow(modelPane, 0);
            Grid.SetColumnSpan(modelPane, 1);

            Grid.SetColumn(verticalSplitter, 0);
            Grid.SetRow(verticalSplitter, 1);
            Grid.SetColumnSpan(verticalSplitter, 1);
            verticalSplitter.Width = double.NaN;
            verticalSplitter.Height = 4;
            verticalSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            verticalSplitter.VerticalAlignment = VerticalAlignment.Stretch;
            verticalSplitter.ResizeDirection = GridResizeDirection.Rows;
            verticalSplitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;

            Grid.SetColumn(familyPane, 0);
            Grid.SetRow(familyPane, 2);
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
                    // Preserve the ordinary isolated Workspace behavior from #2396: the same real
                    // property editor returns to row 2 when the dedicated BIM palette is inactive.
                    familyPane.RowDefinitions[0].MinHeight = 75;
                    familyPane.RowDefinitions[0].MaxHeight = double.PositiveInfinity;
                    familyPane.RowDefinitions[0].Height = new GridLength(42, GridUnitType.Star);
                    familyPane.RowDefinitions[1].MinHeight = 4;
                    familyPane.RowDefinitions[1].MaxHeight = 4;
                    familyPane.RowDefinitions[1].Height = new GridLength(4);
                    familyPane.RowDefinitions[2].MinHeight = 120;
                    familyPane.RowDefinitions[2].MaxHeight = double.PositiveInfinity;
                    familyPane.RowDefinitions[2].Height = new GridLength(58, GridUnitType.Star);

                    foreach (var splitter in familyPane.Children.OfType<GridSplitter>()
                                 .Where(child => Grid.GetRow(child) == 1))
                        splitter.Visibility = Visibility.Visible;

                    FamilyList.MinHeight = 70;
                    PropertyList.MinHeight = 120;
                }
            }
        }
    }
}
