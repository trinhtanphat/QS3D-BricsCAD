using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialCatalogDeterministicOrderingRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectMaterialCatalogDeterministicOrderingSmoke.Run();
    }
}
