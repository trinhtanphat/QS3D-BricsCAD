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
            public PublishedManager(ZoneManagerWindow window, Document document)
            {
                Window = window ?? throw new ArgumentNullException(nameof(window));
                if (document == null) throw new ArgumentNullException(nameof(document));

                var database = document.Database;
                if (database == null || database.UnmanagedObject == IntPtr.Zero)
                    throw new InvalidOperationException("Zone Manager requires a live native BricsCAD database.");

                NativeDatabaseIdentity = database.UnmanagedObject;
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
                        if (previous.Matches(document))
                        {
                            try { previous.Window.Activate(); } catch { }
                            try { PaletteCoordinator.SetStatus("Zone Manager đã mở cho bản vẽ hiện hành."); } catch { }
                            return;
                        }

                        try { previous.Window.Close(); }
                        catch (Exception closeError)
                        {
                            throw new InvalidOperationException(
                                "Không thể đóng Zone Manager của document trước; không mở instance thứ hai.",
                                closeError);
                        }

                        // Window.Close() can return without terminal Closed when any Closing
                        // subscriber vetoes. Static ownership is released only by Closed.
                        if (ReferenceEquals(_published, previous))
                            throw new InvalidOperationException(
                                "Zone Manager của document trước vẫn đang mở; hãy hoàn tất cleanup/close trước khi mở manager cho document khác.");
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
