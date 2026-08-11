using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceBooleanCanonicalitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectBrowserWorkspaceBooleanCanonicalitySmoke.Run();
    }
}
