using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningScheduleRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DoorOpeningScheduleSmoke.Run();
    }
}
