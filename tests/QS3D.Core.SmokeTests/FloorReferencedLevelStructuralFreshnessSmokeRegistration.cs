using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorReferencedLevelStructuralFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => FloorReferencedLevelStructuralFreshnessSmoke.Run();
    }
}
