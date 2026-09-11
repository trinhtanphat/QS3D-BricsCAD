using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectToolsCommands
    {
        private static PublishedManager? _published;

        private sealed class PublishedManager
        {
            private readonly WeakReference<Document> _document;

            public PublishedManager(ProjectToolsWindow window, Document document)
            {
                Window = window ?? throw new ArgumentNullException(nameof(window));
                if (document == null) throw new ArgumentNullException(nameof(document));

                var database = document.Database;
                if (database == null || database.UnmanagedObject == IntPtr.Zero)
                    throw new InvalidOperationException("Project Tools requires a live native BricsCAD database.");

                NativeDatabaseIdentity = database.UnmanagedObject;
                _document = new WeakReference<Document>(document);
            }

            public ProjectToolsWindow Window { get; }
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

        [CommandMethod("QS3DPROJECTTOOLS", CommandFlags.Modal)]
        public void ShowProjectTools()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            ProjectToolsWindow? window = null;
            var nativeDatabaseIdentity = IntPtr.Zero;
            try
            {
                nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;

                var previous = _published;
                if (previous != null)
                {
                    if (previous.Window.IsLoaded)
                    {
                        if (previous.Matches(document) && previous.MatchesManagedWrapper(document))
                        {
                            try { previous.Window.Activate(); } catch { }
                            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                            try { PaletteCoordinator.SetStatus("Project Tools đã mở cho bản vẽ hiện hành."); } catch { }
                            return;
                        }

                        if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                        try { previous.Window.Close(); }
                        catch (Exception closeError)
                        {
                            throw new InvalidOperationException(
                                "Không thể đóng Project Tools trước; không mở instance thứ hai.",
                                closeError);
                        }

                        if (ReferenceEquals(_published, previous))
                            throw new InvalidOperationException(
                                "Project Tools trước vẫn đang mở; hãy hoàn tất cleanup/close trước khi mở manager hiện hành.");
                    }
                    else if (ReferenceEquals(_published, previous))
                    {
                        _published = null;
                    }
                }

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                window = new ProjectToolsWindow(document);
                var published = new PublishedManager(window, document);
                window.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_published, published)) _published = null;
                };

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    CloseWindowOnAffinityDrift(window);
                    window = null;
                    return;
                }
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                    CloseWindowOnAffinityDrift(window);
                    window = null;
                    return;
                }
                if (!window.IsLoaded)
                    throw new InvalidOperationException("Project Tools host show returned without a loaded window.");

                _published = published;
                window = null;
                if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                {
                try { PaletteCoordinator.SetStatus("Project Tools: tầng • vật liệu • template • module • health • khóa theo bản vẽ."); } catch { }
                }
            }
            catch (Exception ex)
            {
                if (window != null)
                {
                    try { window.Close(); } catch { }
                }

                if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;
                var message = "QS3DPROJECTTOOLS lỗi: " + ex.Message;
                try { PaletteCoordinator.SetStatus(message); } catch { }
                try { document.Editor.WriteMessage("\n" + message); } catch { }
            }
        }
        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var database = document.Database;
            if (database == null || database.UnmanagedObject == IntPtr.Zero)
                throw new InvalidOperationException("Project Tools requires a live native BricsCAD database.");
            return database.UnmanagedObject;
        }

        private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (document == null || nativeDatabaseIdentity == IntPtr.Zero) return false;
            try
            {
                var activeDocument = Application.DocumentManager.MdiActiveDocument;
                if (!ReferenceEquals(activeDocument, document)) return false;
                var database = activeDocument.Database;
                return database != null && database.UnmanagedObject != IntPtr.Zero &&
                       database.UnmanagedObject == nativeDatabaseIdentity;
            }
            catch { return false; }
        }

        private static void CloseWindowOnAffinityDrift(ProjectToolsWindow window)
        {
            if (window == null) return;
            try { window.Close(); } catch { }
        }

    }
}