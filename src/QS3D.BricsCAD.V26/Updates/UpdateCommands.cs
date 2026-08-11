using Bricscad.ApplicationServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25.Updates
{
    public sealed class UpdateCommands
    {
        [CommandMethod("QS3DUPDATE", CommandFlags.Modal)]
        public void ShowUpdateCenter()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            document?.Editor.WriteMessage(
                "\nQS3D V26: one-click update is intentionally disabled until a V26-specific signed release channel is qualified. " +
                "Do not install a V25 update package into the V26 host.");
        }
    }
}