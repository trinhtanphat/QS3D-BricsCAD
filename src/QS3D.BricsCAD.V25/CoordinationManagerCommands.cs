using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CoordinationManagerCommands
    {
        private static PublishedManager? _published;
        private static PublishedManager? _publicationInFlight;
        private static PublishedManager? _cleanupInFlight;
        private static bool _nativePublicationCallActive;

        private sealed class PublishedManager
        {
            public PublishedManager(CoordinationManagerWindow window, Document document)
            {
                Window = window ?? throw new ArgumentNullException(nameof(window));
                if (document == null) throw new ArgumentNullException(nameof(document));

                var database = document.Database;
                if (database == null || database.UnmanagedObject == IntPtr.Zero)
                    throw new InvalidOperationException("Coordination Manager requires a live native BricsCAD database.");

                NativeDatabaseIdentity = database.UnmanagedObject;
            }

            public CoordinationManagerWindow Window { get; }
            public IntPtr NativeDatabaseIdentity { get; }

            public bool Matches(Document document)
            {
                if (document == null) return false;
                try
                {
                    var database = document.Database;
                    return database != null &&
                           database.UnmanagedObject != IntPtr.Zero &&
                           database.UnmanagedObject == NativeDatabaseIdentity;
                }
                catch
                {
                    return false;
                }
            }
        }

        [CommandMethod("QS3DCOORDINATIONMANAGER", CommandFlags.Modal)]
        public void ShowCoordinationManager()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            if (_nativePublicationCallActive || _cleanupInFlight != null)
            {
                ReportBlocked(document, "Coordination Manager đang hoàn tất publication/cleanup; không mở instance thứ hai.");
                return;
            }

            if (!PrepareUnpublishedCandidate())
            {
                ReportBlocked(document, "Coordination Manager trước chưa đạt terminal Closed; không mở instance thứ hai.");
                return;
            }

            CoordinationManagerWindow? candidate = null;
            PublishedManager? published = null;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Coordination Manager requires an existing QS3D project.");

                var previous = _published;
                if (previous != null)
                {
                    if (previous.Window.IsLoaded)
                    {
                        if (previous.Matches(document))
                        {
                            try { previous.Window.Activate(); } catch { }
                            try { PaletteCoordinator.SetStatus("Coordination Manager đã mở cho project hiện hành."); } catch { }
                            return;
                        }

                        if (!TryCloseManager(previous))
                            throw new InvalidOperationException("Previous Coordination Manager did not reach terminal Closed.");
                    }
                    else
                    {
                        ReleaseTerminalManager(previous);
                    }
                }

                candidate = new CoordinationManagerWindow(document, project.ProjectId, project.DrawingFingerprint);
                var publishedWindow = candidate;
                published = new PublishedManager(publishedWindow, document);
                var exactPublished = published;
                publishedWindow.Closed += (_, __) => ReleaseClosedManager(exactPublished);

                _publicationInFlight = published;
                _nativePublicationCallActive = true;
                try
                {
                    CoordinationManagerReviewUi.Attach(
                        publishedWindow,
                        document,
                        project.ProjectId,
                        project.DrawingFingerprint);
                    Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);

                    if (!publishedWindow.IsLoaded)
                    {
                        ReleaseTerminalManager(published);
                        candidate = null;
                        try { PaletteCoordinator.SetStatus("Coordination Manager không được publish; candidate đã đóng an toàn."); } catch { }
                        return;
                    }

                    if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                        throw new InvalidOperationException("Active document changed during Coordination Manager publication.");
                    if (!published.Matches(document))
                        throw new InvalidOperationException("Coordination Manager document identity changed during publication.");
                    if (!ReferenceEquals(_publicationInFlight, published))
                        throw new InvalidOperationException("Coordination Manager publication ownership changed unexpectedly.");

                    _published = published;
                    _publicationInFlight = null;
                    candidate = null;
                }
                finally
                {
                    _nativePublicationCallActive = false;
                }

                try { PaletteCoordinator.SetStatus("Đã mở Coordination Manager cho project hiện hành."); } catch { }
            }
            catch (Exception)
            {
                if (published != null && ReferenceEquals(_publicationInFlight, published))
                {
                    TryCloseManager(published);
                }
                else if (candidate != null)
                {
                    try { candidate.Close(); } catch { }
                }

                const string message = "Coordination Manager không thể mở an toàn; publication đã bị từ chối.";
                try { document.Editor.WriteMessage("\nQS3D Coordination Manager: " + message); } catch { }
                try { PaletteCoordinator.SetStatus(message); } catch { }
            }
        }

        private static bool PrepareUnpublishedCandidate()
        {
            var pending = _publicationInFlight;
            if (pending == null) return true;
            return TryCloseManager(pending);
        }

        private static bool TryCloseManager(PublishedManager manager)
        {
            if (manager == null) return true;
            if (_cleanupInFlight != null) return false;

            _cleanupInFlight = manager;
            try
            {
                if (manager.Window.IsLoaded)
                {
                    try
                    {
                        manager.Window.Close();
                    }
                    catch
                    {
                        if (manager.Window.IsLoaded)
                            return false;
                    }
                }

                if (manager.Window.IsLoaded)
                    return false;

                ReleaseTerminalManager(manager);
                return !ReferenceEquals(_published, manager) &&
                       !ReferenceEquals(_publicationInFlight, manager);
            }
            finally
            {
                if (ReferenceEquals(_cleanupInFlight, manager))
                    _cleanupInFlight = null;
            }
        }

        private static void ReleaseClosedManager(PublishedManager manager)
        {
            // Closed can fire synchronously while ShowModelessWindow/Close is still on the
            // native stack. Only published ownership is released here; in-flight/cleanup
            // reservations stay owned by their exact outer stack until it unwinds.
            if (ReferenceEquals(_published, manager))
                _published = null;
        }

        private static void ReleaseTerminalManager(PublishedManager manager)
        {
            if (manager.Window.IsLoaded) return;
            if (ReferenceEquals(_published, manager))
                _published = null;
            if (ReferenceEquals(_publicationInFlight, manager))
                _publicationInFlight = null;
        }

        private static void ReportBlocked(Document document, string message)
        {
            try { document.Editor.WriteMessage("\nQS3D Coordination Manager: " + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}
