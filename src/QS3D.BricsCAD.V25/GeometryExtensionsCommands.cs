using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GeometryExtensionsCommands
    {
        private static GeometryExtensionsWindow? _published;

        [CommandMethod("QS3DGEOMETRYEXT", CommandFlags.Modal)]
        public void ShowGeometryExtensions()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            GeometryExtensionsWindow? window = null;
            try
            {
                var previous = _published;
                if (previous != null)
                {
                    if (previous.IsLoaded)
                    {
                        try { previous.Activate(); } catch { }
                        try { PaletteCoordinator.SetStatus("Geometry Extensions đã mở."); } catch { }
                        return;
                    }

                    if (ReferenceEquals(_published, previous))
                        _published = null;
                }

                window = new GeometryExtensionsWindow();
                var published = window;
                window.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_published, published)) _published = null;
                };

                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded)
                    throw new InvalidOperationException("Geometry Extensions host show returned without a loaded window.");

                _published = published;
                window = null;
                try { PaletteCoordinator.SetStatus("Đã mở Geometry Extensions."); } catch { }
            }
            catch (Exception ex)
            {
                if (window != null)
                {
                    try { window.Close(); } catch { }
                }

                var message = "QS3DGEOMETRYEXT lỗi: " + ex.Message;
                try { PaletteCoordinator.SetStatus(message); } catch { }
                try { document.Editor.WriteMessage("\n" + message); } catch { }
            }
        }
    }
}
