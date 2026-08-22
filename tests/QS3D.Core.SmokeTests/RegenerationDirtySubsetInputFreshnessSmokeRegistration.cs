using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationDirtySubsetInputFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RegenerationDirtySubsetInputFreshnessSmoke.Run();
    }
}
