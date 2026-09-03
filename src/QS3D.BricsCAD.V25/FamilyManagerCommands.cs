using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class FamilyManagerCommands
    {
        private static PublishedManager? _pending;
        private static PublishedManager? _published;

        private sealed class PublishedManager
        {
            private readonly WeakReference<Document> _document;

            public PublishedManager(FamilyManagerWindow window, Document document)
            {
                Window = window ?? throw new ArgumentNullException(nameof(window));
                if (document == null) throw new ArgumentNullException(nameof(document));

                var database = document.Database;
                if (database == null || database.UnmanagedObject == IntPtr.Zero)
                    throw new InvalidOperationException("Family Manager requires a live native BricsCAD database.");

                NativeDatabaseIdentity = database.UnmanagedObject;
                _document = new WeakReference<Document>(document);
            }

            public FamilyManagerWindow Window { get; }
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

        [CommandMethod("QS3DFAMILIES", CommandFlags.Modal)]
        public void ShowFamilyManager()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            PublishedManager? candidate = null;
            try
            {
                ExistingProjectMutationContext.TryGet(document, out _);

                var pending = _pending;
                if (pending != null)
                    CloseOwnerBeforeReplacement(pending, "pending");

                var previous = _published;
                if (previous != null)
                {
                    if (previous.Window.IsLoaded &&
                        previous.Matches(document) &&
                        previous.MatchesManagedWrapper(document))
                    {
                        try { previous.Window.Activate(); } catch { }
                        try { PaletteCoordinator.SetStatus("Family Manager đã mở cho bản vẽ hiện hành."); } catch { }
                        return;
                    }

                    CloseOwnerBeforeReplacement(previous, "published");
                }

                var window = new FamilyManagerWindow(document);
                var owner = new PublishedManager(window, document);
                candidate = owner;
                window.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_pending, owner)) _pending = null;
                    if (ReferenceEquals(_published, owner)) _published = null;
                };

                _pending = owner;
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded)
                    throw new InvalidOperationException("Family Manager did not remain loaded after host publication.");
                if (!ReferenceEquals(_pending, owner))
                    throw new InvalidOperationException("Family Manager publication ownership changed unexpectedly.");

                _pending = null;
                _published = owner;
                candidate = null;
                try { PaletteCoordinator.SetStatus("Family Manager: CRUD • properties • inheritance-safe semantic assignment • khóa theo bản vẽ."); } catch { }
            }
            catch (Exception ex)
            {
                if (candidate != null && ReferenceEquals(_pending, candidate))
                {
                    try { candidate.Window.Close(); } catch { }
                }

                var message = "QS3DFAMILIES không thể mở Family Manager (" + ex.GetType().Name + ").";
                try { PaletteCoordinator.SetStatus(message); } catch { }
                try { document.Editor.WriteMessage("\n" + message); } catch { }
            }
        }

        private static void CloseOwnerBeforeReplacement(PublishedManager owner, string state)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));

            if (!owner.Window.IsLoaded && string.Equals(state, "published", StringComparison.Ordinal))
            {
                if (ReferenceEquals(_published, owner)) _published = null;
                return;
            }

            try
            {
                owner.Window.Close();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Family Manager " + state + " cleanup failed; replacement was refused.",
                    ex);
            }

            if (owner.Window.IsLoaded || ReferenceEquals(_pending, owner) || ReferenceEquals(_published, owner))
                throw new InvalidOperationException(
                    "Family Manager " + state + " owner did not reach terminal close; replacement was refused.");
        }
    }
}
