using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectPropertiesCommands
    {
        private static ProjectPropertiesWindow? _published;

        [CommandMethod("QS3DPROJECTPROPERTIES", CommandFlags.Modal)]
        public void ShowProjectProperties()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            ProjectPropertiesWindow? window = null;
            try
            {
                var previous = _published;
                if (previous != null)
                {
                    if (previous.IsLoaded)
                    {
                        try { previous.Activate(); } catch { }
                        try { PaletteCoordinator.SetStatus("Thuộc tính dự án đã mở."); } catch { }
                        return;
                    }

                    if (ReferenceEquals(_published, previous))
                        _published = null;
                }

                window = new ProjectPropertiesWindow();
                var published = window;
                window.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_published, published)) _published = null;
                };

                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded)
                    throw new InvalidOperationException("Project Properties host show returned without a loaded window.");

                _published = published;
                window = null;
                try { PaletteCoordinator.SetStatus("Thuộc tính dự án: surface BLT3D riêng, read-only placeholder; không mở Project Tools."); } catch { }
            }
            catch (Exception ex)
            {
                if (window != null)
                {
                    try { window.Close(); } catch { }
                }

                var message = "QS3DPROJECTPROPERTIES lỗi: " + ex.Message;
                try { PaletteCoordinator.SetStatus(message); } catch { }
                try { document.Editor.WriteMessage("\n" + message); } catch { }
            }
        }
    }
}
