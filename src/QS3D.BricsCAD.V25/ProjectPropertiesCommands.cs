using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectPropertiesCommands
    {
        private static ProjectPropertiesWindow? _pending;
        private static ProjectPropertiesWindow? _published;

        [CommandMethod("QS3DPROJECTPROPERTIES", CommandFlags.Modal)]
        public void ShowProjectProperties()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            ProjectPropertiesWindow? candidate = null;
            try
            {
                var pending = _pending;
                if (pending != null)
                    CloseOwnerBeforeReplacement(pending, "pending");

                var published = _published;
                if (published != null)
                {
                    if (published.IsLoaded)
                    {
                        try { published.Activate(); } catch { }
                        try { PaletteCoordinator.SetStatus("Thuộc tính dự án đã mở."); } catch { }
                        return;
                    }

                    CloseOwnerBeforeReplacement(published, "published");
                }

                var window = new ProjectPropertiesWindow();
                candidate = window;
                window.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_pending, window)) _pending = null;
                    if (ReferenceEquals(_published, window)) _published = null;
                };

                _pending = window;
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded)
                    throw new InvalidOperationException("Project Properties host show returned without a loaded window.");
                if (!ReferenceEquals(_pending, window))
                    throw new InvalidOperationException("Project Properties publication ownership changed unexpectedly.");

                _pending = null;
                _published = window;
                candidate = null;
                try { PaletteCoordinator.SetStatus("Thuộc tính dự án: surface BLT3D riêng, read-only placeholder; không mở Project Tools."); } catch { }
            }
            catch (Exception ex)
            {
                if (candidate != null && ReferenceEquals(_pending, candidate))
                {
                    try { candidate.Close(); } catch { }
                }

                var message = "QS3DPROJECTPROPERTIES không thể mở Thuộc tính dự án (" + ex.GetType().Name + ").";
                try { PaletteCoordinator.SetStatus(message); } catch { }
                try { document.Editor.WriteMessage("\n" + message); } catch { }
            }
        }

        private static void CloseOwnerBeforeReplacement(ProjectPropertiesWindow window, string state)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            if (!window.IsLoaded && string.Equals(state, "published", StringComparison.Ordinal))
            {
                if (ReferenceEquals(_published, window)) _published = null;
                return;
            }

            try
            {
                window.Close();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Project Properties " + state + " cleanup failed; replacement was refused.",
                    ex);
            }

            if (window.IsLoaded || ReferenceEquals(_pending, window) || ReferenceEquals(_published, window))
                throw new InvalidOperationException(
                    "Project Properties " + state + " owner did not reach terminal close; replacement was refused.");
        }
    }
}
