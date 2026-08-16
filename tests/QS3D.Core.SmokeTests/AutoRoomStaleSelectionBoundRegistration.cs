using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomStaleSelectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => AutoRoomStaleSelectionBoundSmoke.Run();
    }
}
