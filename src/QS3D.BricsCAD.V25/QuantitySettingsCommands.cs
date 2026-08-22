using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class QuantitySettingsCommands
    {
        [CommandMethod("QS3DSETUP", CommandFlags.Modal)]
        public void ShowQuantitySettings()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            try
            {
                var window = new QuantitySettingsWindow(new QuantitySettingsStore());
                QuantitySettingsColorPickerEnhancer.Attach(window);

                // Do not call WPF Window.ShowDialog() directly from a BricsCAD command.
                // BricsCAD owns the host HWND/message loop and exposes ShowModalWindow
                // specifically so WPF dialogs are parented and pumped by the CAD host.
                Application.ShowModalWindow(window);
            }
            catch (Exception ex)
            {
                WriteFailure(document, "QS3DSETUP", ex);
            }
        }

        [CommandMethod("QS3DQUANTITYSETTINGS", CommandFlags.Modal)]
        public void ShowQuantitySettingsLongName()
        {
            ShowQuantitySettings();
        }

        private static void WriteFailure(Document? document, string commandName, Exception error)
        {
            var detail = Describe(error);
            try
            {
                document?.Editor.WriteMessage("\n" + commandName + " error: " + detail);
            }
            catch
            {
                // Never turn a settings-window failure into a second command exception.
            }
        }

        private static string Describe(Exception error)
        {
            var parts = new System.Collections.Generic.List<string>();
            for (var current = error; current != null; current = current.InnerException)
            {
                var message = (current.Message ?? string.Empty).Trim();
                var part = current.GetType().Name + (message.Length == 0 ? string.Empty : ": " + message);
                if (!parts.Contains(part)) parts.Add(part);
            }
            return parts.Count == 0 ? "Unknown error." : string.Join(" -> ", parts);
        }
    }
}
