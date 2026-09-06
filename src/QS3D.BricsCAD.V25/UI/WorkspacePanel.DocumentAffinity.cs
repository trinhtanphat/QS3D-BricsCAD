using System;
using System.Windows;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Owns the native-document affinity fence for the shared modeless Workspace.
    ///
    /// DocumentLifecycleCoordinator deliberately defers the heavier project/selection/UI reconcile
    /// to ApplicationIdle. Clear document-bound Workspace presentation synchronously when MDI
    /// ownership changes so handles, Family rows and project actions from DWG A cannot remain
    /// actionable while DWG B is active.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool DocumentAffinityRegistrationReady = RegisterWorkspaceDocumentAffinity();
        private bool _workspaceDocumentAffinityAttached;

        private static bool RegisterWorkspaceDocumentAffinity()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnWorkspaceAffinityLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnWorkspaceAffinityUnloaded),
                true);
            return true;
        }

        private static void OnWorkspaceAffinityLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel)) return;
            panel.AttachWorkspaceDocumentAffinity();

            // Loaded may recur after the PaletteSet was detached while another DWG became active.
            // Never re-expose stale rows from that detached interval. Rehydrate only project UI;
            // selection inspection remains empty until the active document publishes fresh data.
            panel.InvalidateWorkspaceDocumentState();
            panel.RefreshProject();
        }

        private static void OnWorkspaceAffinityUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel)
                panel.DetachWorkspaceDocumentAffinity();
        }

        private void AttachWorkspaceDocumentAffinity()
        {
            if (_workspaceDocumentAffinityAttached) return;
            Application.DocumentManager.DocumentActivated += OnWorkspaceDocumentActivated;
            try
            {
                Application.DocumentManager.DocumentToBeDestroyed += OnWorkspaceDocumentToBeDestroyed;
                _workspaceDocumentAffinityAttached = true;
            }
            catch
            {
                try { Application.DocumentManager.DocumentActivated -= OnWorkspaceDocumentActivated; }
                catch { }
                throw;
            }
        }

        private void DetachWorkspaceDocumentAffinity()
        {
            if (!_workspaceDocumentAffinityAttached) return;
            _workspaceDocumentAffinityAttached = false;
            try { Application.DocumentManager.DocumentActivated -= OnWorkspaceDocumentActivated; }
            catch { }
            try { Application.DocumentManager.DocumentToBeDestroyed -= OnWorkspaceDocumentToBeDestroyed; }
            catch { }
        }

        private void OnWorkspaceDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            // Synchronous by design: queuing behind lifecycle ApplicationIdle would recreate the
            // exact A-state/B-document action window this fence owns.
            InvalidateWorkspaceDocumentState();
        }

        private void OnWorkspaceDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if (ReferenceEquals(Application.DocumentManager.MdiActiveDocument, e.Document))
                InvalidateWorkspaceDocumentState();
        }

        private void InvalidateWorkspaceDocumentState()
        {
            // ClearProject is presentation-only and already suppresses Workspace callbacks while it
            // replaces the inspection, Family/project view model and active Zone/Floor presentation.
            ClearProject("Đang đồng bộ Workspace với bản vẽ active.");
        }
    }
}
