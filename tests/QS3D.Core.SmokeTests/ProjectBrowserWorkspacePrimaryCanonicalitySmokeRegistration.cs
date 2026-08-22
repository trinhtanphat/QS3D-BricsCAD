using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspacePrimaryCanonicalitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectBrowserWorkspacePrimaryCanonicalitySmoke.Run();
    }
}
