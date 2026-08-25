using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GridIntersectionMarkerPlannerSmoke.Run();
            GridIntersectionPlannerSmoke.Run();
        }
    }
}
