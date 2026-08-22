using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceXmlQueryRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectBrowserWorkspaceXmlQuerySmoke.Run();
    }
}