using System;
using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private const string RaftVisibleAddLabel = "+ Add";

        // BLT3D relabels the shared Add button after the generic Workspace handlers are wired.
        // Register one narrow class-level bridge for the rendered label so Móng Bè keeps the
        // owner-required direct Add route instead of falling through to the generic mode chooser.
        private static bool RaftVisibleAddRouteRegistered { get; } = RegisterRaftVisibleAddRoute();

        private static bool RegisterRaftVisibleAddRoute()
        {
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(OnRaftVisibleAddButtonClick),
                true);
            return true;
        }

        private static void OnRaftVisibleAddButtonClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) ||
                !string.Equals(button.Content as string, RaftVisibleAddLabel, StringComparison.Ordinal)) return;

            var panel = FindRaftWorkspacePanel(button);
            if (panel == null || !panel.IsRaftSubtypeFilter()) return;

            e.Handled = true;
            panel.CreateFamilyFromWorkspaceSubtype(false);
        }
    }
}
