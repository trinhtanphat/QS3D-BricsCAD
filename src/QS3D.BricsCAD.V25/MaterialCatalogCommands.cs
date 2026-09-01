using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class MaterialCatalogCommands
    {
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
            try
            {
                if (!ExistingProjectMutationContext.TryGet(document, out var project))
                    throw new InvalidOperationException("Material Catalog cần QS3D project hiện hữu. Hãy chạy QS3DINIT hoặc mở/nạp project trước.");

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
                var published = new PublishedManager(window, document);
                window.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_published, published)) _published = null;
                };

                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded)
                    throw new InvalidOperationException("Material Catalog host show returned without a loaded window.");

                _published = published;
                window = null;
                try { PaletteCoordinator.SetStatus("Material Catalog: built-in + custom + apply theo semantic selection • khóa theo bản vẽ đang mở."); } catch { }
            }
            catch (Exception ex)
            {
                if (window != null)
                {
                    try { window.Close(); } catch { }
                }

                var message = "QS3DMATERIALS lỗi: " + ex.Message;
                try { PaletteCoordinator.SetStatus(message); } catch { }
                try { document.Editor.WriteMessage("\n" + message); } catch { }
            }
        }
    }
}