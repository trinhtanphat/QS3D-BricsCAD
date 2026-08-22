using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomDanglingPreviousFamilyRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => AutoRoomDanglingPreviousFamilySmoke.Run();
    }
}
