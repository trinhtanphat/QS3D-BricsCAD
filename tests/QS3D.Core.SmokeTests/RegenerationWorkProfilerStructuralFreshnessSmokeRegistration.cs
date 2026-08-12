using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfilerStructuralFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RegenerationWorkProfilerStructuralFreshnessSmoke.Run();
    }
}
