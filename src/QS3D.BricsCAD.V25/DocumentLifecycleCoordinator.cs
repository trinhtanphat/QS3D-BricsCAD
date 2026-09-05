using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    internal static class DocumentLifecycleCoordinator
    {
        private static bool _started;
        private static readonly Dictionary<Document, DatabaseIOEventHandler> SaveCompleteHandlers = new Dictionary<Document, DatabaseIOEventHandler>();
        private static readonly Dictionary<Document, DocumentBeginCloseEventHandler> BeginCloseHandlers = new Dictionary<Document, DocumentBeginCloseEventHandler>();
        private static readonly Dictionary<Document, bool> PendingReconciliation = new Dictionary<Document, bool>();
        private static readonly Dictionary<Document, FailedProjectReconcile> FailedProjectReconciliations = new Dictionary<Document, FailedProjectReconcile>();
        private static DispatcherOperation? _lifecycleIdleOperation;
        private static bool _pendingNoDocumentReset;

        public static void Start()
        {
            if (_started) return;
            var docs = Application.DocumentManager;
            try
            {
                docs.DocumentCreated += OnDocumentCreated;
                docs.DocumentActivated += OnDocumentActivated;
                docs.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
                docs.DocumentDestroyed += OnDocumentDestroyed;

                // Keep save/close and Undo observers attached before NETLOAD returns so a
                // higher-priority input/command cannot outrun the lifecycle hooks.
                AttachCriticalServices(docs.MdiActiveDocument);
                _started = true;

                // Project/selection/UI reconciliation is the potentially expensive part.
                // Queue it so NETLOAD can return before sidecar/palette work runs.
                ScheduleReconcile(docs.MdiActiveDocument, false);
            }
            catch
            {
                try { docs.DocumentCreated -= OnDocumentCreated; } catch { }
                try { docs.DocumentActivated -= OnDocumentActivated; } catch { }
                try { docs.DocumentToBeDestroyed -= OnDocumentToBeDestroyed; } catch { }
                try { docs.DocumentDestroyed -= OnDocumentDestroyed; } catch { }
                StopPendingLifecycleWork();
                foreach (var document in SaveCompleteHandlers.Keys.ToArray()) DetachProjectPersistence(document);
                SourceReconcileUndoCoordinator.Stop();
                CurtainWallUndoCoordinator.Stop();
                SelectionSyncCoordinator.Stop();
                _started = false;
                throw;
            }
        }

        public static void Stop()
        {
            if (!_started) return;
            _started = false;
            var docs = Application.DocumentManager;
            try { docs.DocumentCreated -= OnDocumentCreated; } catch { }
            try { docs.DocumentActivated -= OnDocumentActivated; } catch { }
            try { docs.DocumentToBeDestroyed -= OnDocumentToBeDestroyed; } catch { }
            try { docs.DocumentDestroyed -= OnDocumentDestroyed; } catch { }
            StopPendingLifecycleWork();
            try
            {
                foreach (var document in SaveCompleteHandlers.Keys.ToArray()) DetachProjectPersistence(document);
            }
            catch
            {
                // Continue teardown even if native document bookkeeping is already unavailable.
            }
            try { SourceReconcileUndoCoordinator.Stop(); }
            catch { }
            try { CurtainWallUndoCoordinator.Stop(); }
            catch { }
            try { SelectionSyncCoordinator.Stop(); }
            catch { }
        }

        private static void OnDocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            try
            {
                AttachCriticalServices(e.Document);
                ScheduleReconcile(e.Document, false);
            }
            catch (Exception ex)
            {
                ReportLifecycleError(e.Document, ex);
            }
        }

        private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            try
            {
                AttachCriticalServices(e.Document);
                ScheduleReconcile(e.Document, true);
            }
            catch (Exception ex)
            {
                ReportLifecycleError(e.Document, ex);
            }
        }

        private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            var document = e.Document;
            var teardownErrors = new List<Exception>();

            // Teardown is intentionally synchronous: native handlers must be gone before
            // BricsCAD destroys the document. Every action is independently fail-soft so
            // one coordinator failure cannot suppress later native/document cleanup.
            try { CancelPendingReconcile(document); } catch (Exception ex) { teardownErrors.Add(ex); }
            try { FailedProjectReconciliations.Remove(document); } catch (Exception ex) { teardownErrors.Add(ex); }
            try { DetachProjectPersistence(document); } catch (Exception ex) { teardownErrors.Add(ex); }
            try { SourceReconcileUndoCoordinator.Detach(document); } catch (Exception ex) { teardownErrors.Add(ex); }
            try { CurtainWallUndoCoordinator.Detach(document); } catch (Exception ex) { teardownErrors.Add(ex); }
            try { SelectionSyncCoordinator.Detach(document); } catch (Exception ex) { teardownErrors.Add(ex); }
            try { ProjectContextCoordinator.Forget(document); } catch (Exception ex) { teardownErrors.Add(ex); }

            ReportDocumentDestroyTeardownErrors(document, teardownErrors);
        }

        private static void ReportDocumentDestroyTeardownErrors(Document document, List<Exception> errors)
        {
            if (errors.Count == 0) return;
            Report(
                document,
                "QS3D document destroy teardown completed with " + errors.Count +
                " cleanup error(s). Internal details were hidden.");
        }

        private static void OnDocumentDestroyed(object sender, DocumentDestroyedEventArgs e)
        {
            var docs = Application.DocumentManager;
            if (docs.Count == 0)
            {
                PendingReconciliation.Clear();
                _pendingNoDocumentReset = true;
                ScheduleLifecycleIdleDrain();
                return;
            }

            var active = docs.MdiActiveDocument;
            if (active == null) return;
            try
            {
                AttachCriticalServices(active);
                ScheduleReconcile(active, true);
            }
            catch (Exception ex)
            {
                ReportLifecycleError(active, ex);
            }
        }

        private static void AttachCriticalServices(Document? document)
        {
            if (document == null) return;
            AttachProjectPersistence(document);
            SourceReconcileUndoCoordinator.Attach(document);
            CurtainWallUndoCoordinator.Attach(document);
        }

        private static void ScheduleReconcile(Document? document, bool refreshUi)
        {
            if (!_started || document == null) return;
            if (PendingReconciliation.TryGetValue(document, out var pendingRefresh))
                PendingReconciliation[document] = pendingRefresh || refreshUi;
            else
                PendingReconciliation.Add(document, refreshUi);
            _pendingNoDocumentReset = false;
            ScheduleLifecycleIdleDrain();
        }

        private static void CancelPendingReconcile(Document? document)
        {
            if (document == null) return;
            PendingReconciliation.Remove(document);
            if (PendingReconciliation.Count == 0 && !_pendingNoDocumentReset)
                CancelLifecycleIdleDrain();
        }

        private static void ScheduleLifecycleIdleDrain()
        {
            if (!_started || _lifecycleIdleOperation != null) return;
            _lifecycleIdleOperation = Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(OnLifecycleIdle));
        }

        private static void CancelLifecycleIdleDrain()
        {
            var operation = _lifecycleIdleOperation;
            _lifecycleIdleOperation = null;
            if (operation == null) return;
            try { operation.Abort(); } catch { }
        }

        private static void StopPendingLifecycleWork()
        {
            CancelLifecycleIdleDrain();
            PendingReconciliation.Clear();
            FailedProjectReconciliations.Clear();
            _pendingNoDocumentReset = false;
        }

        private static void OnLifecycleIdle()
        {
            _lifecycleIdleOperation = null;
            if (!_started) return;

            var pending = PendingReconciliation.ToArray();
            PendingReconciliation.Clear();
            var resetForNoDocument = _pendingNoDocumentReset;
            _pendingNoDocumentReset = false;

            if (resetForNoDocument && Application.DocumentManager.Count == 0)
            {
                try { SelectionSyncCoordinator.Refresh(null); } catch { }
                try { PaletteCoordinator.ResetForNoDocument(); } catch { }
            }

            foreach (var pair in pending)
            {
                if (!_started) break;
                ReconcileDocument(pair.Key, pair.Value);
            }

            if (_started && (PendingReconciliation.Count > 0 || _pendingNoDocumentReset))
                ScheduleLifecycleIdleDrain();
        }

        private static void ReconcileDocument(Document document, bool refreshUi)
        {
            try
            {
                var refreshActiveUi = refreshUi && IsActiveDocument(document);
                SelectionSyncCoordinator.Attach(document);
                EnsureProject(document, refreshActiveUi);
                if (refreshActiveUi) SelectionSyncCoordinator.Refresh(document);
            }
            catch (Exception ex)
            {
                ReportLifecycleError(document, ex);
            }
        }

        private static bool IsActiveDocument(Document document)
        {
            try
            {
                return ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document);
            }
            catch
            {
                return false;
            }
        }

        private static void AttachProjectPersistence(Document? document)
        {
            if (document == null || SaveCompleteHandlers.ContainsKey(document)) return;
            DatabaseIOEventHandler saveComplete = (sender, args) => OnDrawingSaveComplete(document, args);
            DocumentBeginCloseEventHandler beginClose = (sender, args) => OnBeginDocumentClose(document, args);
            document.Database.SaveComplete += saveComplete;
            try { document.BeginDocumentClose += beginClose; }
            catch
            {
                try { document.Database.SaveComplete -= saveComplete; }
                catch { }
                throw;
            }
            SaveCompleteHandlers[document] = saveComplete;
            BeginCloseHandlers[document] = beginClose;
        }

        private static void DetachProjectPersistence(Document? document)
        {
            if (document == null || !SaveCompleteHandlers.TryGetValue(document, out var saveComplete)) return;
            try { document.Database.SaveComplete -= saveComplete; }
            catch { }
            if (BeginCloseHandlers.TryGetValue(document, out var beginClose))
            {
                try { document.BeginDocumentClose -= beginClose; }
                catch { }
            }
            SaveCompleteHandlers.Remove(document);
            BeginCloseHandlers.Remove(document);
        }

        private static void OnDrawingSaveComplete(Document document, DatabaseIOEventArgs args)
        {
            try
            {
                if (!IsNamedDrawing(document))
                {
                    if (ProjectContextCoordinator.HasPendingChanges(document))
                        Report(document, "QS3D project is still pending because the saved DWG path is not available. Run SAVEAS, then QS3DSAVE if needed.");
                    return;
                }

                if (!ProjectContextCoordinator.TrySavePending(document, out var path)) return;
                Report(document, "QS3D sidecar saved after DWG save: " + path);
            }
            catch (Exception saveError)
            {
                var recovery = TryWriteRecovery(document, saveError);
                Report(document, "DWG save completed, but the QS3D sidecar could not be saved." + recovery);
            }
        }

        private static void OnBeginDocumentClose(Document document, DocumentBeginCloseEventArgs e)
        {
            try
            {
                if (!ProjectContextCoordinator.HasPendingChanges(document)) return;
                var choice = MessageBox.Show(
                    "QS3D has semantic project changes that are not stored in the .qsdb sidecar.\n\n" +
                    "Yes: save the QS3D sidecar and continue closing.\n" +
                    "No: discard the pending QS3D changes and continue closing.\n" +
                    "Cancel: keep the drawing open.",
                    "QS3D — Unsaved project changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (choice == MessageBoxResult.No) return;
                if (choice != MessageBoxResult.Yes)
                {
                    e.Veto();
                    return;
                }

                if (!IsNamedDrawing(document))
                {
                    e.Veto();
                    MessageBox.Show(
                        "Save the DWG with SAVEAS first. QS3D will write the matching .qsdb only after the drawing has a stable path.",
                        "QS3D — Save DWG first",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var path = ProjectContextCoordinator.Save(document);
                Report(document, "QS3D sidecar saved before close: " + path);
            }
            catch (Exception saveError)
            {
                e.Veto();
                var recovery = TryWriteRecovery(document, saveError);
                var message = "The drawing was kept open because QS3D could not save its sidecar." + recovery;
                Report(document, message);
                MessageBox.Show(
                    message,
                    "QS3D — Project save failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string TryWriteRecovery(Document document, Exception saveError)
        {
            try
            {
                ProjectContextCoordinator.SaveRecoveryCopy(document, saveError);
                return " Recovery copy was written successfully.";
            }
            catch (Exception)
            {
                return " Recovery copy also failed; internal details were hidden.";
            }
        }

        private static bool IsNamedDrawing(Document document)
        {
            try { return !string.IsNullOrWhiteSpace(document.Name) && Path.IsPathRooted(document.Name); }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) { return false; }
        }

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); }
            catch { }
            if (!IsActiveDocument(document)) return;
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
        }

        private static void ReportLifecycleError(Document document, Exception error)
        {
            Report(document, "QS3D document lifecycle reconcile failed. Internal details were hidden.");
        }

        private static void EnsureProject(Document? document, bool refreshUi)
        {
            if (document == null) return;
            if (TryUseStableFailedProjectReconcile(document, refreshUi)) return;

            ProjectSidecarRevisionStamp? attemptedRevision = null;
            TryCaptureProjectRevision(document, out attemptedRevision);
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out _))
                {
                    FailedProjectReconciliations.Remove(document);
                    if (refreshUi)
                        PaletteCoordinator.ResetForUnavailableProject(
                            "No QS3D project is available for this drawing. Use an authoring command to create one.");
                    return;
                }

                FailedProjectReconciliations.Remove(document);
                if (refreshUi) PaletteCoordinator.RefreshAll();
            }
            catch (InvalidDataException)
            {
                const string message = "QS3D project load failed. Internal details were hidden.";
                RememberStableProjectLoadFailure(document, attemptedRevision, message);
                if (refreshUi)
                {
                    try { PaletteCoordinator.ResetForUnavailableProject(message); }
                    catch { }
                }
            }
            catch (Exception)
            {
                FailedProjectReconciliations.Remove(document);
                const string message = "QS3D project load failed. Internal details were hidden.";
                if (refreshUi)
                {
                    try { PaletteCoordinator.ResetForUnavailableProject(message); }
                    catch { }
                }
            }
        }

        private static bool TryUseStableFailedProjectReconcile(Document document, bool refreshUi)
        {
            if (!FailedProjectReconciliations.TryGetValue(document, out var failed)) return false;

            try
            {
                if (ProjectContextCoordinator.TryGetCached(document, out _))
                {
                    FailedProjectReconciliations.Remove(document);
                    return false;
                }
            }
            catch
            {
                FailedProjectReconciliations.Remove(document);
                return false;
            }

            if (!TryCaptureProjectRevision(document, out var current) || current == null || !failed.Revision.Equals(current))
            {
                FailedProjectReconciliations.Remove(document);
                return false;
            }

            if (refreshUi)
            {
                try { PaletteCoordinator.ResetForUnavailableProject(failed.Message); }
                catch { }
            }
            return true;
        }

        private static void RememberStableProjectLoadFailure(
            Document document,
            ProjectSidecarRevisionStamp? attemptedRevision,
            string message)
        {
            if (attemptedRevision == null || !attemptedRevision.HasAnyFile)
            {
                FailedProjectReconciliations.Remove(document);
                return;
            }

            if (!TryCaptureProjectRevision(document, out var current) || current == null || !attemptedRevision.Equals(current))
            {
                FailedProjectReconciliations.Remove(document);
                return;
            }

            FailedProjectReconciliations[document] = new FailedProjectReconcile(current, message);
        }

        private static bool TryCaptureProjectRevision(Document document, out ProjectSidecarRevisionStamp? revision)
        {
            revision = null;
            if (!IsNamedDrawing(document)) return false;
            try
            {
                revision = ProjectSidecarRevisionStamp.Capture(ProjectContextCoordinator.GetProjectPath(document));
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException || ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                revision = null;
                return false;
            }
        }

        private sealed class FailedProjectReconcile
        {
            public FailedProjectReconcile(ProjectSidecarRevisionStamp revision, string message)
            {
                Revision = revision ?? throw new ArgumentNullException(nameof(revision));
                Message = message ?? string.Empty;
            }

            public ProjectSidecarRevisionStamp Revision { get; }
            public string Message { get; }
        }
    }
}
