using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests;

internal static class CurtainWallDetailNumericCollapseRegistration
{
    [ModuleInitializer]
    internal static void Register() => CurtainWallDetailNumericCollapseSmoke.Run();
}
