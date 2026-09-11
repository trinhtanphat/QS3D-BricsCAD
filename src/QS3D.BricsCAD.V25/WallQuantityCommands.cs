using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class WallQuantityCommands
    {
        private static PublishedWindow? _pending;
        private static PublishedWindow? _published;

        private sealed class PublishedWindow
        {
            private readonly WeakReference<Document> _document;

            public PublishedWindow(WallQuantityWindow window, Document document, IntPtr nativeDatabaseIdentity)
            {
                Window = window ?? throw new ArgumentNullException(nameof(window));
                if (document == null) throw new ArgumentNullException(nameof(document));
                if (nativeDatabaseIdentity == IntPtr.Zero) throw new ArgumentException("Native database identity is required.", nameof(nativeDatabaseIdentity));
                _document = new WeakReference<Document>(document);
                NativeDatabaseIdentity = nativeDatabaseIdentity;
            }

            public WallQuantityWindow Window { get; }
            public IntPtr NativeDatabaseIdentity { get; }

            public bool Matches(Document document, IntPtr nativeDatabaseIdentity)
            {
                return nativeDatabaseIdentity != IntPtr.Zero &&
                       nativeDatabaseIdentity == NativeDatabaseIdentity &&
                       _document.TryGetTarget(out var ownedDocument) &&
                       ReferenceEquals(ownedDocument, document);
            }
        }

        [CommandMethod("QS3DWALLQTY", CommandFlags.Modal)]
        public void ShowWallQuantity()
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
                if (pending != null && !TryCloseOwner(pending)) return;
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;

                var published = _published;
                if (published != null)
                {
                    if (published.Window.IsLoaded && published.Matches(document, nativeDatabaseIdentity))
                    {
                        try { published.Window.Activate(); } catch { }
                        return;
                    }
                    if (!TryCloseOwner(published)) return;
                    if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                }

                var window = new WallQuantityWindow(document);
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
                    throw new InvalidOperationException("Wall Quantity did not remain loaded after host publication.");
                if (!ReferenceEquals(_pending, owner))
                    throw new InvalidOperationException("Wall Quantity publication ownership changed unexpectedly.");

                _pending = null;
                _published = owner;
                owner = null;
                try { PaletteCoordinator.SetStatus("Wall Quantity ready for the active drawing."); } catch { }
            }
            catch (Exception ex)
            {
                if (owner != null) TryCloseOwner(owner);
                if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    var message = "QS3DWALLQTY failed (" + ex.GetType().Name + ").";
                    try { PaletteCoordinator.SetStatus(message); } catch { }
                    try { document.Editor.WriteMessage("\n" + message); } catch { }
                }
            }
        }

        private static bool TryCloseOwner(PublishedWindow owner)
        {
            if (owner == null) return true;
            if (!owner.Window.IsLoaded)
            {
                ReleaseOwnedWindow(owner);
                return true;
            }
            try { owner.Window.Close(); } catch { return false; }
            if (owner.Window.IsLoaded) return false;
            ReleaseOwnedWindow(owner);
            return true;
        }

        private static void ReleaseOwnedWindow(PublishedWindow owner)
        {
            if (ReferenceEquals(_pending, owner)) _pending = null;
            if (ReferenceEquals(_published, owner)) _published = null;
        }

        private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (document == null || nativeDatabaseIdentity == IntPtr.Zero) return false;
            try
            {
                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)) return false;
                var database = document.Database;
                return database != null && database.UnmanagedObject == nativeDatabaseIdentity;
            }
            catch { return false; }
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null) throw new InvalidOperationException("Wall Quantity requires a BricsCAD database.");
            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero) throw new InvalidOperationException("Wall Quantity requires a live native database.");
            return identity;
        }
    }
}
