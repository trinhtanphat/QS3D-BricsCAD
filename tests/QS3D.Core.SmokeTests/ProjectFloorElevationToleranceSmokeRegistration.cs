using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorElevationToleranceSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFloorElevationToleranceSmoke.Run();
    }
}
