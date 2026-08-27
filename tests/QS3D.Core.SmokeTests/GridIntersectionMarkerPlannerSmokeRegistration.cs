using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionMarkerPlannerSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => GridIntersectionMarkerPlannerSmoke.Run();
    }
}
