using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceEmptyMetadataSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectBrowserWorkspaceEmptyMetadataSmoke.Run();
    }
}
