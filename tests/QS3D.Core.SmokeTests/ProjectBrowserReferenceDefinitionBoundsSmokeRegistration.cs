using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserReferenceDefinitionBoundsSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectBrowserReferenceDefinitionBoundsSmoke.Run();
    }
}
