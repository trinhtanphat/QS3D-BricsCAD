using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceXmlCanonicalityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectBrowserWorkspaceXmlCanonicalitySmoke.Run();
    }
}
