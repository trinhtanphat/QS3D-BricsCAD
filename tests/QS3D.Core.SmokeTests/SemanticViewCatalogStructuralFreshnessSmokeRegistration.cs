using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewCatalogStructuralFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticViewCatalogStructuralFreshnessSmoke.Run();
    }
}
