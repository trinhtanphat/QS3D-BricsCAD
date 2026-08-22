using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningScheduleGroupKeyCollisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DoorOpeningScheduleGroupKeyCollisionSmoke.Run();
    }
}
