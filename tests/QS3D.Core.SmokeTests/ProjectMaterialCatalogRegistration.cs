using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialCatalogRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectMaterialCatalogSmoke.Run();
    }
}
