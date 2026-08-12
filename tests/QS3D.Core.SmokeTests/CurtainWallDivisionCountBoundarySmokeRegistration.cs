using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallDivisionCountBoundarySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainWallDivisionCountBoundarySmoke.Run();
    }
}
