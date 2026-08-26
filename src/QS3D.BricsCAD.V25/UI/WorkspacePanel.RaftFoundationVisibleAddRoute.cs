using System;
using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private const string RaftVisibleAddLabel = "+ Add";

        // BLT3D relabels the shared Add button after the generic Workspace handlers are wired.
        // An explicit type initializer is required here: a side-effect-only static field/property
        // initializer may be emitted as beforefieldinit and is not guaranteed to run before the
        // first routed click. Register the narrow bridge deterministically before WorkspacePanel use.
        static WorkspacePanel()
        {
            RegisterRaftVisibleAddRoute();
        }

        private static void RegisterRaftVisibleAddRoute()
        {
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(OnRaftVisibleAddButtonClick),
                true);
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
