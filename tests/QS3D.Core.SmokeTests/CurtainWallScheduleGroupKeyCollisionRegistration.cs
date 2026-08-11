using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallScheduleGroupKeyCollisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainWallScheduleGroupKeyCollisionSmoke.Run();
    }
}
