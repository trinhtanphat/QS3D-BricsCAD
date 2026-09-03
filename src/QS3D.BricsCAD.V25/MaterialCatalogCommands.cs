using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class MaterialCatalogCommands
    {
        private static PublishedManager? _pending;
        private static PublishedManager? _published;

        private sealed class PublishedManager
        {
            private readonly WeakReference<Document> _document;

            public PublishedManager(MaterialCatalogWindow window, Document document)
            {
                Window = window ?? throw new ArgumentNullException(nameof(window));
                if (document == null) throw new ArgumentNullException(nameof(document));

                var database = document.Database;
                if (database == null || database.UnmanagedObject == IntPtr.Zero)
                    throw new InvalidOperationException("Material Catalog requires a live native BricsCAD database.");

                NativeDatabaseIdentity = database.UnmanagedObject;
                _document = new WeakReference<Document>(document);
            }

            public MaterialCatalogWindow Window { get; }
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

        [CommandMethod("QS3DMATERIALS", CommandFlags.Modal)]
        public void ShowMaterialCatalog()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            MaterialCatalogWindow? window = null;
            PublishedManager? candidate = null;
            try
            {
                if (!ExistingProjectMutationContext.TryGet(document, out var project))
                    throw new InvalidOperationException("Material Catalog cần QS3D project hiện hữu. Hãy chạy QS3DINIT hoặc mở/nạp project trước.");

                var pending = _pending;
                if (pending != null)
                {
                    if (pending.Matches(document) && pending.MatchesManagedWrapper(document))
                    {
                        if (pending.Window.IsLoaded)
                        {
                            try { pending.Window.Activate(); } catch { }
                        }

                        try { PaletteCoordinator.SetStatus("Material Catalog đang được mở cho bản vẽ hiện hành."); } catch { }
                        return;
                    }

                    throw new InvalidOperationException(
                        "Material Catalog khác đang trong quá trình mở; không tạo instance thứ hai trước khi host hoàn tất publication.");
                }

                var previous = _published;
                if (previous != null)
                {
                    if (previous.Window.IsLoaded)
                    {
                        if (previous.Matches(document) && previous.MatchesManagedWrapper(document))
                        {
                            try { previous.Window.Activate(); } catch { }
                            try { PaletteCoordinator.SetStatus("Material Catalog đã mở cho bản vẽ hiện hành."); } catch { }
                            return;
                        }

                        try { previous.Window.Close(); }
                        catch (Exception closeError)
                        {
                            throw new InvalidOperationException(
                                "Không thể đóng Material Catalog trước; không mở instance thứ hai.",
                                closeError);
                        }

                        if (ReferenceEquals(_published, previous))
                            throw new InvalidOperationException(
                                "Material Catalog trước vẫn đang mở; hãy hoàn tất cleanup/close trước khi mở manager hiện hành.");
                    }
                    else if (ReferenceEquals(_published, previous))
                    {
                        _published = null;
                    }
                }

                window = new MaterialCatalogWindow(document, project);
                candidate = new PublishedManager(window, document);
                var reserved = candidate;
                _pending = reserved;

                window.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_pending, reserved)) _pending = null;
                    if (ReferenceEquals(_published, reserved)) _published = null;
                };

                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded)
                    throw new InvalidOperationException("Material Catalog host show returned without a loaded window.");
                if (!ReferenceEquals(_pending, reserved))
                    throw new InvalidOperationException("Material Catalog pending publication ownership changed during host show.");

                _pending = null;
                _published = reserved;
                candidate = null;
                window = null;
                try { PaletteCoordinator.SetStatus("Material Catalog: built-in + custom + apply theo semantic selection • khóa theo bản vẽ đang mở."); } catch { }
            }
            catch (Exception)
            {
                if (candidate != null && ReferenceEquals(_pending, candidate))
                    _pending = null;

                if (window != null)
                {
                    try { window.Close(); } catch { }
                }

                const string message = "QS3DMATERIALS không thể mở Material Catalog an toàn; trạng thái hiện tại được giữ nguyên.";
                try { PaletteCoordinator.SetStatus(message); } catch { }
                try { document.Editor.WriteMessage("\n" + message); } catch { }
            }
        }
    }
}
