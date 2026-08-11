using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    internal static class DocumentLifecycleCoordinator
    {
        private static bool _started;
        private static readonly Dictionary<Document, DatabaseIOEventHandler> SaveCompleteHandlers = new Dictionary<Document, DatabaseIOEventHandler>();
        private static readonly Dictionary<Document, DocumentBeginCloseEventHandler> BeginCloseHandlers = new Dictionary<Document, DocumentBeginCloseEventHandler>();

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
                AttachProjectPersistence(docs.MdiActiveDocument);
                SelectionSyncCoordinator.Attach(docs.MdiActiveDocument);
                _started = true;
            }
            catch
            {
                try { docs.DocumentCreated -= OnDocumentCreated; } catch { }
                try { docs.DocumentActivated -= OnDocumentActivated; } catch { }
                try { docs.DocumentToBeDestroyed -= OnDocumentToBeDestroyed; } catch { }
                try { docs.DocumentDestroyed -= OnDocumentDestroyed; } catch { }
                foreach (var document in SaveCompleteHandlers.Keys.ToArray()) DetachProjectPersistence(document);
                SelectionSyncCoordinator.Stop();
                _started = false;
                throw;
            }
        }

        public static void Stop()
        {
            if (!_started) return;
            var docs = Application.DocumentManager;
            docs.DocumentCreated -= OnDocumentCreated;
            docs.DocumentActivated -= OnDocumentActivated;
            docs.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
            docs.DocumentDestroyed -= OnDocumentDestroyed;
            foreach (var document in SaveCompleteHandlers.Keys.ToArray()) DetachProjectPersistence(document);
            SelectionSyncCoordinator.Stop();
            _started = false;
        }

        private static void OnDocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            AttachProjectPersistence(e.Document);
            SelectionSyncCoordinator.Attach(e.Document);
            EnsureProject(e.Document, false);
        }

        private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            AttachProjectPersistence(e.Document);
            SelectionSyncCoordinator.Attach(e.Document);
            EnsureProject(e.Document, true);
            SelectionSyncCoordinator.Refresh(e.Document);
        }

        private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            var document = e.Document;
            DetachProjectPersistence(document);
            SelectionSyncCoordinator.Detach(document);
            ProjectContextCoordinator.Forget(document);
        }

        private static void OnDocumentDestroyed(object sender, DocumentDestroyedEventArgs e)
        {
            var docs = Application.DocumentManager;
            if (docs.Count == 0)
            {
                SelectionSyncCoordinator.Refresh(null);
                PaletteCoordinator.ResetForNoDocument();
                return;
            }

            var active = docs.MdiActiveDocument;
            if (active == null) return;
            AttachProjectPersistence(active);
            SelectionSyncCoordinator.Attach(active);
            EnsureProject(active, true);
            SelectionSyncCoordinator.Refresh(active);
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
                Report(document, "QS3D sidecar save failed after DWG save: " + saveError.Message + recovery);
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
                Report(document, "QS3D close cancelled because the sidecar could not be saved: " + saveError.Message + recovery);
                MessageBox.Show(
                    "The drawing was kept open because QS3D could not save its sidecar.\n\n" + saveError.Message + recovery,
                    "QS3D — Project save failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string TryWriteRecovery(Document document, Exception saveError)
        {
            try
            {
                var path = ProjectContextCoordinator.SaveRecoveryCopy(document, saveError);
                return " Recovery copy: " + path;
            }
            catch (Exception recoveryError)
            {
                return " Recovery copy also failed: " + recoveryError.Message;
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
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
        }

        private static void EnsureProject(Document? document, bool refreshUi)
        {
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out _))
                {
                    if (refreshUi)
                        PaletteCoordinator.ResetForUnavailableProject(
                            "No QS3D project is available for this drawing. Use an authoring command to create one.");
                    return;
                }

                if (refreshUi) PaletteCoordinator.RefreshAll();
            }
            catch (Exception ex)
            {
                var message = "QS3D project load error: " + ex.Message;
                try { document.Editor.WriteMessage("\n" + message); }
                catch { }
                try { PaletteCoordinator.ResetForUnavailableProject(message); }
                catch { }
            }
        }
    }
}
