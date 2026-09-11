using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CommercialQsCommands
    {
        private static PublishedWindow? _pending;
        private static PublishedWindow? _published;

        private sealed class PublishedWindow
        {
            private readonly WeakReference<Document> _document;

            public PublishedWindow(CommercialQsWindow window, Document document, IntPtr nativeDatabaseIdentity)
            {
                Window = window ?? throw new ArgumentNullException(nameof(window));
                _document = new WeakReference<Document>(document ?? throw new ArgumentNullException(nameof(document)));
                NativeDatabaseIdentity = nativeDatabaseIdentity;
            }

            public CommercialQsWindow Window { get; }

            public IntPtr NativeDatabaseIdentity { get; }

            public bool Matches(Document document, IntPtr nativeDatabaseIdentity)
            {
                return _document.TryGetTarget(out var ownedDocument)
                    && ReferenceEquals(ownedDocument, document)
                    && nativeDatabaseIdentity != IntPtr.Zero
                    && nativeDatabaseIdentity == NativeDatabaseIdentity;
            }
        }

        [CommandMethod("QS3DCOMMERCIAL", CommandFlags.Modal)]
        public void ShowCommercialQsWorkspace()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            var nativeDatabaseIdentity = IntPtr.Zero;
            PublishedWindow? owner = null;
            try
            {
                nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;

                var pending = _pending;
                if (pending != null && !TryCloseOwner(pending))
                {
                    ReportIfActive(document, nativeDatabaseIdentity, "QS3DCOMMERCIAL: a previous unpublished Commercial QS window has not reached terminal Closed.");
                    return;
                }

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;

                var published = _published;
                if (published != null)
                {
                    if (published.Window.IsLoaded && published.Matches(document, nativeDatabaseIdentity))
                    {
                        try { published.Window.Activate(); } catch { }
                        ReportIfActive(document, nativeDatabaseIdentity, "Commercial QS workspace activated.");
                        return;
                    }

                    if (!TryCloseOwner(published))
                    {
                        ReportIfActive(document, nativeDatabaseIdentity, "QS3DCOMMERCIAL: the existing Commercial QS window belongs to another drawing generation and could not close safely.");
                        return;
                    }

                    if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                }

                var window = new CommercialQsWindow(document);
                owner = new PublishedWindow(window, document, nativeDatabaseIdentity);
                var releaseOwner = owner;
                window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);
                _pending = owner;

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    TryCloseOwner(owner);
                    return;
                }

                Application.ShowModelessWindow(IntPtr.Zero, window, true);

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    TryCloseOwner(owner);
                    return;
                }

                if (!window.IsLoaded)
                {
                    ReleaseOwnedWindow(owner);
                    return;
                }

                if (!ReferenceEquals(_pending, owner))
                    return;

                _pending = null;
                _published = owner;
                owner = null;
                ReportIfActive(document, nativeDatabaseIdentity, "Commercial QS workspace opened: Variation • IPC • Final Account • Tender • CVR • XLSX.");
            }
            catch (Exception ex)
            {
                if (owner != null)
                    TryCloseOwner(owner);

                ReportIfActive(document, nativeDatabaseIdentity, "QS3DCOMMERCIAL failed (" + ex.GetType().Name + ").");
            }
        }

        private static bool TryCloseOwner(PublishedWindow owner)
        {
            if (!owner.Window.IsLoaded)
            {
                ReleaseOwnedWindow(owner);
                return true;
            }

            try { owner.Window.Close(); }
            catch { return false; }

            if (owner.Window.IsLoaded) return false;
            ReleaseOwnedWindow(owner);
            return true;
        }

        private static void ReleaseOwnedWindow(PublishedWindow owner)
        {
            if (ReferenceEquals(_pending, owner))
                _pending = null;
            if (ReferenceEquals(_published, owner))
                _published = null;
        }

        private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (nativeDatabaseIdentity == IntPtr.Zero) return false;
            try
            {
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                    return false;
                var database = document.Database;
                return database != null
                    && database.UnmanagedObject != IntPtr.Zero
                    && database.UnmanagedObject == nativeDatabaseIdentity;
            }
            catch
            {
                return false;
            }
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Commercial QS requires a BricsCAD document database.");
            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Commercial QS requires a live native BricsCAD database.");
            return identity;
        }

        private static void ReportIfActive(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try { document.Editor.WriteMessage("\n" + message); } catch { }
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}