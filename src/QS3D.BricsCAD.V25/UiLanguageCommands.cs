using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class UiLanguageCommands
    {
        [CommandMethod("QS3DLANGUAGE", CommandFlags.Modal)]
        public void ShowLanguageSettings()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            try
            {
                Application.ShowModalWindow(new UiLanguageWindow());
            }
            catch (Exception error)
            {
                try
                {
                    document?.Editor.WriteMessage(
                        "\nQS3DLANGUAGE error: "
                        + error.GetType().Name
                        + ": "
                        + (error.Message ?? string.Empty));
                }
                catch
                {
                    // Do not turn a language-window failure into a second command error.
                }
            }
        }

        [CommandMethod("QS3DNGONNGU", CommandFlags.Modal)]
        public void ShowLanguageSettingsVietnameseAlias()
        {
            ShowLanguageSettings();
        }
    }
}
