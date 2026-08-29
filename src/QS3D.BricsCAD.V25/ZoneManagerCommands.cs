using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ZoneManagerCommands
    {
        private static PublishedManager? _published;

        private sealed class PublishedManager
        {
            private readonly WeakReference<Document> _document;

            public PublishedManager(ZoneManagerWindow window, Document document)
            {
                Window = window ?? throw new ArgumentNullException(nameof(window));
                if (document == null) throw new ArgumentNullException(nameof(document));

                var database = document.Database;
                if (database == null || database.UnmanagedObject == IntPtr.Zero)
                    throw new InvalidOperationException("Zone Manager requires a live native BricsCAD database.");

                NativeDatabaseIdentity = database.UnmanagedObject;
                _document = new WeakReference<Document>(document);
            }

            public ZoneManagerWindow Window { get; }
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

            public bool MatchesManagedWrapper(Document document)
            {
                return document != null &&
                       _document.TryGetTarget(out var ownedDocument) &&
                       ReferenceEquals(ownedDocument, document);
            }
        }

        [CommandMethod("QS3DZONES", CommandFlags.Modal)]
        public void ShowZoneManager()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            ZoneManagerWindow? candidate = null;
            try
            {
                ExistingProjectMutationContext.TryGet(document, out _);

                var previous = _published;
                if (previous != null)
                {
                    if (previous.Window.IsLoaded)
                    {
                        if (previous.Matches(document) && previous.MatchesManagedWrapper(document))
                        {
                            try { previous.Window.Activate(); } catch { }
                            try { PaletteCoordinator.SetStatus("Zone Manager đã mở cho bản vẽ hiện hành."); } catch { }
                            return;
                        }

                        try { previous.Window.Close(); }
                        catch (Exception closeError)
                        {
                            throw new InvalidOperationException(
                                "Không thể đóng Zone Manager trước; không mở instance thứ hai.",
                                closeError);
                        }

                        // A native-database match with managed-wrapper drift must replace the
                        // old wrapper-bound window. Close() can also be vetoed. In either case,
                        // construction is forbidden until terminal Closed releases publication.
                        if (ReferenceEquals(_published, previous))
                            throw new InvalidOperationException(
                                "Zone Manager trước vẫn đang mở; hãy hoàn tất cleanup/close trước khi mở manager hiện hành.");
                    }
                    else if (ReferenceEquals(_published, previous))
                    {
                        // Defensive stale-reference repair. A normally closed manager clears
                        // ownership synchronously in its instance-safe Closed handler below.
                        _published = null;
                    }
                }

                candidate = new ZoneManagerWindow(document);
                var publishedWindow = candidate;
                var published = new PublishedManager(publishedWindow, document);
                publishedWindow.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_published, published)) _published = null;
                };

                Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);
                _published = published;
                candidate = null;
                try { PaletteCoordinator.SetStatus("Đã mở Zone Manager cho bản vẽ hiện hành."); } catch { }
            }
            catch (Exception ex)
            {
                if (candidate != null)
                {
                    try { candidate.Close(); } catch { }
                }

                var message = "QS3DZONES lỗi: " + ex.Message;
                try { PaletteCoordinator.SetStatus(message); } catch { }
                try { document.Editor.WriteMessage("\n" + message); } catch { }
            }
        }
    }
}
