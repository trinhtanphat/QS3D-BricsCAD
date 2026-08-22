using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class GridSpatialOrderingAxisScaleRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GridSpatialOrderingAxisScaleSmoke.Run();
        }
    }
}
