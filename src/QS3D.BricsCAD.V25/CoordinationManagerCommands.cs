using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CoordinationManagerCommands
    {
        private static CoordinationManagerWindow? _window;

        [CommandMethod("QS3DCOORDINATIONMANAGER", CommandFlags.Modal)]
        public void ShowCoordinationManager()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            CoordinationManagerWindow? candidate = null;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Coordination Manager cần một QS3D project hiện hữu; thao tác đọc không tạo project thay thế.");

                var previous = _window;
                _window = null;
                if (previous != null)
                {
                    try { previous.Close(); } catch { }
                }

                candidate = new CoordinationManagerWindow(document, project.ProjectId, project.DrawingFingerprint);
                CoordinationManagerReviewUi.Attach(candidate, document, project.ProjectId, project.DrawingFingerprint);

                var published = candidate;
                published.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_window, published)) _window = null;
                };

                Application.ShowModelessWindow(IntPtr.Zero, published, true);
                _window = published;
                candidate = null;
                try { PaletteCoordinator.SetStatus("Đã mở Coordination Manager cho project hiện hành."); } catch { }
            }
            catch (Exception ex)
            {
                if (candidate != null)
                {
                    try { candidate.Close(); } catch { }
                    if (ReferenceEquals(_window, candidate)) _window = null;
                }

                try { document.Editor.WriteMessage("\nQS3D Coordination Manager: " + ex.Message); } catch { }
                try { PaletteCoordinator.SetStatus("Coordination Manager: " + ex.Message); } catch { }
            }
        }
    }
}
