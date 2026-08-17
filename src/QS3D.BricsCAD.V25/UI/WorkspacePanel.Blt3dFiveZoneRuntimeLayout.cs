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
    /// The left PaletteSet remains one native BricsCAD palette, but it exposes two distinct
    /// QS3D regions vertically: Model/Zone/Floor above and Family/Properties below. This keeps
    /// the real BricsCAD viewport wide enough on 1366-class screens while making Properties
    /// visibly QS3D-owned instead of relying on the host Properties palette.
    ///
    /// Run at SystemIdle so this pass wins after the older CompactShell / reference presentation
    /// compatibility passes that still rewrite the same five-column Grid during Loaded.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool Blt3dFiveZoneRuntimeLayoutRegistered = RegisterBlt3dFiveZoneRuntimeLayout();

        // Force CLR type initialization before the first WorkspacePanel instance is constructed.
        // Without an explicit static constructor this partial class is marked beforefieldinit, so
        // the class-handler registrations below are allowed to run too late for the first Loaded
        // event. The explicit constructor makes both this registration and the runtime-repair
        // registration in the sibling partial deterministic for every first palette instance.
        static WorkspacePanel()
        {
        }

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
            var familyPropertiesPane = workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(child =>
                    Grid.GetColumn(child) == 2 &&
                    IsVisualDescendant(child, FamilyList) &&
                    IsVisualDescendant(child, PropertyList));
            var verticalSplitter = workspace.Children
                .OfType<GridSplitter>()
                .FirstOrDefault(child => Grid.GetColumn(child) == 1);

            if (modelPane == null || familyPropertiesPane == null || verticalSplitter == null)
                return;

            root.MinWidth = 0;
            WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            WorkspaceOverflow.ScrollToHorizontalOffset(0);

            // Collapse the old horizontal dashboard columns. The host palette now needs only one
            // compact left rail, with QS3D Properties visibly below the model browser.
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
                Height = new GridLength(45, GridUnitType.Star),
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
                Height = new GridLength(55, GridUnitType.Star),
                MinHeight = 220
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

            // Give Properties the larger share of the lower QS3D region. Family/Type remains
            // available and functional rather than being retired only to gain visual parity.
            if (familyPropertiesPane.RowDefinitions.Count >= 3)
            {
                familyPropertiesPane.RowDefinitions[0].MinHeight = 75;
                familyPropertiesPane.RowDefinitions[0].MaxHeight = double.PositiveInfinity;
                familyPropertiesPane.RowDefinitions[0].Height = new GridLength(42, GridUnitType.Star);
                familyPropertiesPane.RowDefinitions[1].MinHeight = 4;
                familyPropertiesPane.RowDefinitions[1].MaxHeight = 4;
                familyPropertiesPane.RowDefinitions[1].Height = new GridLength(4);
                familyPropertiesPane.RowDefinitions[2].MinHeight = 120;
                familyPropertiesPane.RowDefinitions[2].MaxHeight = double.PositiveInfinity;
                familyPropertiesPane.RowDefinitions[2].Height = new GridLength(58, GridUnitType.Star);
            }

            FamilyList.MinHeight = 70;
            PropertyList.MinHeight = 120;
        }
    }
}
