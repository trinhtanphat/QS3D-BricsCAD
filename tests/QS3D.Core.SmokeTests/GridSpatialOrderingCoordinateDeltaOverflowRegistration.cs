using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class GridSpatialOrderingCoordinateDeltaOverflowRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GridSpatialOrderingCoordinateDeltaOverflowSmoke.Run();
        }
    }
}
