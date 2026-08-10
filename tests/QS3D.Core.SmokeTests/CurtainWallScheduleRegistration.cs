using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallScheduleRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainWallScheduleSmoke.Run();
    }
}
