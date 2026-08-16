using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests;

internal static class CurtainWallOpeningFrameCoordinateCollapseRegistration
{
    [ModuleInitializer]
    internal static void Register()
    {
        CurtainWallOpeningFrameCoordinateCollapseSmoke.Run();
    }
}
