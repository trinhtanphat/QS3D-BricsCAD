using Bricscad.ApplicationServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class StartCenterCommands
    {
        [CommandMethod("QS3DSTART", CommandFlags.Modal)]
        public void ShowStartCenter()
        {
            try
            {
                StartCenterPaletteCoordinator.Show();
            }
            catch (System.Exception ex)
            {
                try
                {
                    Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                        "\nQS3DSTART error: " + ex.Message);
                }
                catch (System.Exception)
                {
                    // Never let optional Start Center diagnostics escape the command failure boundary.
                }
            }
        }
    }
}
