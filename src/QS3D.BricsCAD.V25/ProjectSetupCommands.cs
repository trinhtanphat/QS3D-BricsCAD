using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Public command surface for the embedded Project Setup work-area experience.
    /// Both placeholder commands intentionally share one non-mutating BricsCAD-hosted surface.
    /// </summary>
    public sealed class ProjectSetupCommands
    {
        [CommandMethod("QS3DPROJECTINFO", CommandFlags.Modal)]
        public void ShowProjectInformation()
        {
            ProjectSetupPaletteCoordinator.ShowProjectInformation();
        }

        [CommandMethod("QS3DPROJECTPROPERTIES", CommandFlags.Modal)]
        public void ShowProjectProperties()
        {
            // Properties is a distinct public route, but issue #2103 intentionally specifies the
            // same embedded placeholder until the real project-properties workflow is implemented.
            ProjectSetupPaletteCoordinator.ShowProjectInformation();
        }
    }
}
