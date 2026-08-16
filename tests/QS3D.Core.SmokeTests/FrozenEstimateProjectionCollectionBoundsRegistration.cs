using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class FrozenEstimateProjectionCollectionBoundsRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            FrozenEstimateProjectionCollectionBoundsSmoke.Run();
        }
    }
}