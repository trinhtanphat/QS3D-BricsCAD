using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageScheduleGroupKeyCollisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MaterialUsageScheduleGroupKeyCollisionSmoke.Run();
    }
}
