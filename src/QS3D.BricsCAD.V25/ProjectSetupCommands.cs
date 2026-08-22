using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Public command surface for the read-only Project Information work area.
    /// Project Properties is intentionally owned by ProjectPropertiesCommands.
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
