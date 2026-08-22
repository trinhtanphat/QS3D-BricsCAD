using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceContainerOrderSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectBrowserWorkspaceContainerOrderSmoke.Run();
    }
}
