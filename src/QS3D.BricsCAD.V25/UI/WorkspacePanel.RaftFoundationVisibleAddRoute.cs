using System;
using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private const string RaftVisibleAddLabel = "+ Add";

        // WorkspacePanel already has one explicit type initializer in CompactShell.cs. That
        // prevents beforefieldinit for the partial type, so this field initializer is guaranteed
        // to run as part of WorkspacePanel type initialization before the first live instance use.
        // Keep registration here narrow and let the single existing cctor remain authoritative.
        private static readonly bool _raftVisibleAddRouteRegistered = RegisterRaftVisibleAddRoute();

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
