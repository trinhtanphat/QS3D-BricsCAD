using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishScheduleGroupKeyCollisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RoomFinishScheduleGroupKeyCollisionSmoke.Run();
    }
}
