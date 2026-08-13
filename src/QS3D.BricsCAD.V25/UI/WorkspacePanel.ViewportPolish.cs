using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Final viewport pass for docked/short Workspace palettes. The existing compact shell
    /// owns the width breakpoint; this pass runs immediately afterwards so the same layout
    /// also remains usable when BricsCAD gives the palette little vertical space.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static bool ViewportPolishRegistered { get; } = RegisterViewportPolish();
        private bool _viewportPolishApplied;

        private static bool RegisterViewportPolish()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnViewportPolishLoaded),
                true);
            return true;
        }

        private static void OnViewportPolishLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel))
                return;

            // CompactShell also handles Loaded. Defer this pass until those width rules have
            // been applied, otherwise its historical fixed minimums can win and clip the footer.
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(panel.ApplyViewportPolish));
        }

        private void ApplyViewportPolish()
        {
            if (_viewportPolishApplied)
                return;

            _viewportPolishApplied = true;

            // Horizontal overflow is solved by the responsive width breakpoint. Keep a vertical
            // safety valve for unusually short docked palettes instead of silently clipping rows.
            WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            WorkspaceOverflow.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            WorkspaceOverflow.PanningMode = PanningMode.VerticalOnly;

            WorkspaceContentRoot.MinWidth = 0;
            WorkspaceContentRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
            WorkspaceContentRoot.VerticalAlignment = VerticalAlignment.Stretch;

            var workspace = WorkspaceContentRoot.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate =>
                    Grid.GetRow(candidate) == 1 &&
                    candidate.ColumnDefinitions.Count == 5);
            if (workspace == null)
                return;

            var familyAndProperties = workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate => candidate.RowDefinitions.Count == 3 && Grid.GetColumn(candidate) == 2);
            var roomAndSelection = workspace.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate => candidate.RowDefinitions.Count == 3 && Grid.GetColumn(candidate) == 4);
            if (familyAndProperties == null || roomAndSelection == null)
                return;

            void ApplyHeightBreakpoint()
            {
                ApplyWorkspaceHeightBreakpoint(workspace, familyAndProperties, roomAndSelection);
            }

            workspace.SizeChanged += (_, __) => ApplyHeightBreakpoint();
            ApplyHeightBreakpoint();
        }

        private void ApplyWorkspaceHeightBreakpoint(
            Grid workspace,
            Grid familyAndProperties,
            Grid roomAndSelection)
        {
            if (workspace.RowDefinitions.Count < 3 ||
                familyAndProperties.RowDefinitions.Count < 3 ||
                roomAndSelection.RowDefinitions.Count < 3)
                return;

            var width = workspace.ActualWidth;
            var narrow = width > 0 && width < 680;
            if (!narrow)
            {
                FamilyList.MinHeight = 82;
                PropertyList.MinHeight = 118;
                InspectionList.MinHeight = 96;
                return;
            }

            var height = workspace.ActualHeight;
            var shortViewport = height > 0 && height < 430;

            // The earlier responsive pass used 245 + 4 + 190 px minimum body rows. Together
            // with the 40 px header and 30 px footer that exceeded the ~480 px palette height
            // visible in the report. Keep both tiers useful while allowing their own lists to
            // scroll; the outer vertical scrollbar is only a last-resort fallback.
            workspace.RowDefinitions[0].MinHeight = shortViewport ? 185 : 210;
            workspace.RowDefinitions[2].MinHeight = shortViewport ? 145 : 170;

            familyAndProperties.RowDefinitions[0].Height = new GridLength(shortViewport ? 125 : 145);
            familyAndProperties.RowDefinitions[0].MinHeight = shortViewport ? 100 : 110;
            roomAndSelection.RowDefinitions[0].Height = new GridLength(shortViewport ? 85 : 100);
            roomAndSelection.RowDefinitions[0].MinHeight = shortViewport ? 70 : 80;

            FamilyList.MinHeight = shortViewport ? 58 : 68;
            PropertyList.MinHeight = shortViewport ? 76 : 88;
            InspectionList.MinHeight = shortViewport ? 66 : 78;
        }
    }
}
