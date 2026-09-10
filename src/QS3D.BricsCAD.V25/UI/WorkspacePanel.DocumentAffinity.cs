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
        private bool _workspaceDocumentActivatedMayBeSubscribed;
        private bool _workspaceDocumentDestroyMayBeSubscribed;
        private bool _workspaceDocumentAffinityDetachInProgress;

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

            try
            {
                if (!_workspaceDocumentActivatedMayBeSubscribed)
                {
                    _workspaceDocumentActivatedMayBeSubscribed = true;
                    Application.DocumentManager.DocumentActivated += OnWorkspaceDocumentActivated;
                }

                if (!_workspaceDocumentDestroyMayBeSubscribed)
                {
                    _workspaceDocumentDestroyMayBeSubscribed = true;
                    Application.DocumentManager.DocumentToBeDestroyed += OnWorkspaceDocumentToBeDestroyed;
                }

                _workspaceDocumentAffinityAttached =
                    _workspaceDocumentActivatedMayBeSubscribed &&
                    _workspaceDocumentDestroyMayBeSubscribed;
            }
            catch
            {
                // Either native add may have registered before throwing. Retain exact per-event
                // ownership until best-effort removal actually succeeds.
                RetryWorkspaceDocumentAffinityDetach();
                throw;
            }
        }

        private void DetachWorkspaceDocumentAffinity()
        {
            RetryWorkspaceDocumentAffinityDetach();
        }

        private void RetryWorkspaceDocumentAffinityDetach()
        {
            if (_workspaceDocumentAffinityDetachInProgress) return;
            if (!_workspaceDocumentActivatedMayBeSubscribed && !_workspaceDocumentDestroyMayBeSubscribed)
            {
                _workspaceDocumentAffinityAttached = false;
                return;
            }

            _workspaceDocumentAffinityDetachInProgress = true;
            try
            {
                if (_workspaceDocumentActivatedMayBeSubscribed)
                {
                    try
                    {
                        Application.DocumentManager.DocumentActivated -= OnWorkspaceDocumentActivated;
                        _workspaceDocumentActivatedMayBeSubscribed = false;
                    }
                    catch
                    {
                        // Keep ownership true so a later Unloaded/stale callback can retry.
                    }
                }

                if (_workspaceDocumentDestroyMayBeSubscribed)
                {
                    try
                    {
                        Application.DocumentManager.DocumentToBeDestroyed -= OnWorkspaceDocumentToBeDestroyed;
                        _workspaceDocumentDestroyMayBeSubscribed = false;
                    }
                    catch
                    {
                        // Keep ownership true so a later cleanup can retry without duplicate add.
                    }
                }
            }
            finally
            {
                _workspaceDocumentAffinityAttached =
                    _workspaceDocumentActivatedMayBeSubscribed &&
                    _workspaceDocumentDestroyMayBeSubscribed;
                _workspaceDocumentAffinityDetachInProgress = false;
            }
        }

        private void OnWorkspaceDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (!_workspaceDocumentAffinityAttached)
            {
                RetryWorkspaceDocumentAffinityDetach();
                return;
            }

            // Synchronous by design: queuing behind lifecycle ApplicationIdle would recreate the
            // exact A-state/B-document action window this fence owns.
            TryInvalidateWorkspaceDocumentStateFromNativeCallback();
        }

        private void OnWorkspaceDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if (!_workspaceDocumentAffinityAttached)
            {
                RetryWorkspaceDocumentAffinityDetach();
                return;
            }

            try
            {
                if (ReferenceEquals(Application.DocumentManager.MdiActiveDocument, e.Document))
                    TryInvalidateWorkspaceDocumentStateFromNativeCallback();
            }
            catch (Exception)
            {
                // Native document wrappers can become unavailable during teardown. Fail closed by
                // clearing document-bound Workspace presentation; never let a host callback escape.
                TryInvalidateWorkspaceDocumentStateFromNativeCallback();
            }
        }

        private void TryInvalidateWorkspaceDocumentStateFromNativeCallback()
        {
            try
            {
                InvalidateWorkspaceDocumentState();
            }
            catch (Exception)
            {
                // A callback racing Unloaded/disposal is cleanup-only. Retain normal subscriptions
                // for a still-loaded Workspace so an isolated presentation error does not silently
                // disable future document-affinity fencing.
                if (!IsLoaded)
                    RetryWorkspaceDocumentAffinityDetach();
            }
        }

        private void InvalidateWorkspaceDocumentState()
        {
            // ClearProject is presentation-only and already suppresses Workspace callbacks while it
            // replaces the inspection, Family/project view model and active Zone/Floor presentation.
            ClearProject("Đang đồng bộ Workspace với bản vẽ active.");
        }
    }
}
