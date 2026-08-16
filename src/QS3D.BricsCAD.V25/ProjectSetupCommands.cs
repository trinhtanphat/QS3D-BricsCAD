using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// BLT3D-familiar project-information entry point. The legacy full Project Tools command and
    /// the independently landed Project Properties command remain available on their own routes.
    /// </summary>
    public sealed class ProjectSetupCommands
    {
        [CommandMethod("QS3DPROJECTINFO", CommandFlags.Modal)]
        public void ShowProjectInformation()
        {
            ProjectSetupPaletteCoordinator.ShowProjectInformation();
        }
    }
}
