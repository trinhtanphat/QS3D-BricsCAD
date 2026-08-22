using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserQueryDefinitionBoundsSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectBrowserQueryDefinitionBoundsSmoke.Run();
    }
}
