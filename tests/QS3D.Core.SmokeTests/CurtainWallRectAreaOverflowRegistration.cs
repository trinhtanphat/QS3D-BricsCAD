using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallRectAreaOverflowRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainWallRectAreaOverflowSmoke.Run();
    }
}
