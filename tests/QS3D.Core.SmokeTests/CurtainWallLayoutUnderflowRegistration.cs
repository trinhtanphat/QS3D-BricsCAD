using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests;

internal static class CurtainWallLayoutUnderflowRegistration
{
    [ModuleInitializer]
    internal static void Register()
    {
        CurtainWallLayoutUnderflowSmoke.Run();
    }
}
