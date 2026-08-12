using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorMutationInputFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => FloorMutationInputFreshnessSmoke.Run();
    }
}
