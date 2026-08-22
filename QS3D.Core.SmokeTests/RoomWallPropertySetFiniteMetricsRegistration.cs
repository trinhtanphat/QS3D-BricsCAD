using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomWallPropertySetFiniteMetricsRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RoomWallPropertySetFiniteMetricsSmoke.Run();
    }
}
