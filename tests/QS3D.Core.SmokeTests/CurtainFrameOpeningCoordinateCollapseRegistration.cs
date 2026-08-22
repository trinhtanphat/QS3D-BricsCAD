using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests;

internal static class CurtainFrameOpeningCoordinateCollapseRegistration
{
    [ModuleInitializer]
    internal static void Register()
    {
        CurtainFrameOpeningCoordinateCollapseSmoke.Run();
    }
}
