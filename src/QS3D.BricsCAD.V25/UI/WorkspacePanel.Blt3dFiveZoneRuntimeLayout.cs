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
    /// The Workspace PaletteSet owns Model/Zone/Floor plus Family browsing only. QS3D Properties
    /// is intentionally hosted by its own native BricsCAD PaletteSet so it can be docked/resized
    /// independently instead of being fused into the Family browser. Together with Management and
    /// Quantity on the right, these palettes surround the real native BricsCAD modelspace.
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

            // Reassertion must be idempotent. The first pass deliberately moves this pane from
            // column 2 to column 0, so rediscover it by its owned controls rather than by the
            // original compatibility-layout column. PropertyList remains in the visual tree only
            // as a compatibility host; its region is collapsed because the live property editor is
            // now rendered in the separately dockable Qs3dPropertiesPanel PaletteSet.
            var familyPropertiesPane = workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(child =>
                    IsVisualDescendant(child, FamilyList) &&
                    IsVisualDescendant(child, PropertyList));

            // Likewise, the first pass moves the original column-1 splitter into row 1/column 0.
            // Keep the exact splitter instance for later settle passes; rediscover only when the
            // host has recreated/reparented the workspace tree.
            var verticalSplitter = _blt3dRuntimeVerticalSplitter;
            if (verticalSplitter == null || !ReferenceEquals(verticalSplitter.Parent, workspace))
            {
                verticalSplitter = workspace.Children
                    .OfType<GridSplitter>()
                    .FirstOrDefault(child => Grid.GetColumn(child) == 1);
                _blt3dRuntimeVerticalSplitter = verticalSplitter;
            }

            if (modelPane == null || familyPropertiesPane == null || verticalSplitter == null)
                return;

            var embeddedPropertyRegion = familyPropertiesPane.Children
                .OfType<Border>()
                .FirstOrDefault(child =>
                    Grid.GetRow(child) == 2 &&
                    IsVisualDescendant(child, PropertyList));
            var embeddedPropertySplitter = familyPropertiesPane.Children
                .OfType<GridSplitter>()
                .FirstOrDefault(child => Grid.GetRow(child) == 1);

            root.MinWidth = 0;
            WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            WorkspaceOverflow.ScrollToHorizontalOffset(0);

            // Collapse the old horizontal dashboard columns. The host palette now needs one compact
            // left rail for Model + Family; dedicated QS3D Properties is a separate native palette.
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
                Height = new GridLength(52, GridUnitType.Star),
                MinHeight = 150
            });
            workspace.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(4),
                MinHeight = 4,
                MaxHeight = 4
            });
            workspace.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(48, GridUnitType.Star),
                MinHeight = 150
            });

            foreach (UIElement child in workspace.Children)
            {
                child.Visibility = ReferenceEquals(child, modelPane) ||
                                   ReferenceEquals(child, familyPropertiesPane) ||
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

            Grid.SetColumn(familyPropertiesPane, 0);
            Grid.SetRow(familyPropertiesPane, 2);
            Grid.SetColumnSpan(familyPropertiesPane, 1);

            // #2399: the embedded Workspace property editor must not masquerade as an independent
            // region. Keep Family fully functional here and collapse the compatibility property row;
            // the same live WorkspaceViewModel is rendered by Qs3dPropertiesPanel in another
            // PaletteSet, so edits/selection remain real and synchronized rather than mocked.
            if (familyPropertiesPane.RowDefinitions.Count >= 3)
            {
                familyPropertiesPane.RowDefinitions[0].MinHeight = 120;
                familyPropertiesPane.RowDefinitions[0].MaxHeight = double.PositiveInfinity;
                familyPropertiesPane.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                familyPropertiesPane.RowDefinitions[1].MinHeight = 0;
                familyPropertiesPane.RowDefinitions[1].MaxHeight = 0;
                familyPropertiesPane.RowDefinitions[1].Height = new GridLength(0);
                familyPropertiesPane.RowDefinitions[2].MinHeight = 0;
                familyPropertiesPane.RowDefinitions[2].MaxHeight = 0;
                familyPropertiesPane.RowDefinitions[2].Height = new GridLength(0);
            }

            if (embeddedPropertySplitter != null)
                embeddedPropertySplitter.Visibility = Visibility.Collapsed;
            if (embeddedPropertyRegion != null)
                embeddedPropertyRegion.Visibility = Visibility.Collapsed;

            FamilyList.Visibility = Visibility.Visible;
            FamilyList.MinHeight = 120;
            PropertyList.Visibility = Visibility.Collapsed;
            PropertyList.MinHeight = 0;
        }
    }
}
