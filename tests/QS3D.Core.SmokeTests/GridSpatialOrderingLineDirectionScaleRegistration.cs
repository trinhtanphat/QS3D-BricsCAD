using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class GridSpatialOrderingLineDirectionScaleRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GridSpatialOrderingLineDirectionScaleSmoke.Run();
        }
    }
}
