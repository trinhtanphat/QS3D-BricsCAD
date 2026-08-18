using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class QuantityBltPresetCommands
    {
        [CommandMethod("QS3DSETUPBLT", CommandFlags.Modal)]
        public void ShowBltQuantityPreset()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            try
            {
                var window = new QuantitySettingsWindow(new QuantitySettingsStore());
                window.LoadBltPresetOnOpen();
                QuantitySettingsColorPickerEnhancer.Attach(window);

                // Keep the compatibility entry point on the same BricsCAD-owned
                // modal host/message loop as the native QS3DSETUP command.
                Application.ShowModalWindow(window);
            }
            catch (Exception ex)
            {
                WriteFailure(document, "QS3DSETUPBLT", ex);
            }
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
