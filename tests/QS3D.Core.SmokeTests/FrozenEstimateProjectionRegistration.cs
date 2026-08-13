using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class FrozenEstimateProjectionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => FrozenEstimateProjectionSmoke.Run();
    }
}