using System.Windows;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private bool _workspaceDocumentAffinityAttached;

        static WorkspacePanel()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnWorkspacePanelLoaded));
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnWorkspacePanelUnloaded));
        }

        private static void OnWorkspacePanelLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel)
                panel.AttachWorkspaceDocumentAffinity();
        }

        private static void OnWorkspacePanelUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel)
                panel.DetachWorkspaceDocumentAffinity();
        }

        private void AttachWorkspaceDocumentAffinity()
        {
            if (_workspaceDocumentAffinityAttached) return;
            Application.DocumentManager.DocumentActivated += OnWorkspaceDocumentActivated;
            _workspaceDocumentAffinityAttached = true;
        }

        private void DetachWorkspaceDocumentAffinity()
        {
            if (!_workspaceDocumentAffinityAttached) return;
            try
            {
                Application.DocumentManager.DocumentActivated -= OnWorkspaceDocumentActivated;
            }
            finally
            {
                _workspaceDocumentAffinityAttached = false;
            }
        }

        private void OnWorkspaceDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            InvalidateWorkspaceDocumentState();
        }

        private void InvalidateWorkspaceDocumentState()
        {
            // DocumentLifecycleCoordinator intentionally performs project/selection/UI hydration at
            // ApplicationIdle. Clear the shared modeless Workspace synchronously at the native MDI
            // activation boundary so rows and handles from document A cannot be interpreted against
            // newly-active document B during that activation-to-idle window.
            ClearProject("Workspace is reconciling the active drawing.");
        }
    }
}
