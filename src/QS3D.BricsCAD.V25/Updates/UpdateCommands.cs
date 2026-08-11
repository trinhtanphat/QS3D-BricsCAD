using System;
using Bricscad.ApplicationServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25.Updates
{
    public sealed class UpdateCommands
    {
        [CommandMethod("QS3DUPDATE", CommandFlags.Modal)]
        public void ShowUpdateCenter()
        {
            try
            {
                UpdateCenterWindowHost.Show();
            }
            catch (Exception ex)
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\nQS3DUPDATE error: " + ex.Message);
            }
        }
    }
}