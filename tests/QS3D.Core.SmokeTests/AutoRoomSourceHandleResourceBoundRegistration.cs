using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomSourceHandleResourceBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => AutoRoomSourceHandleResourceBoundSmoke.Run();
    }
}
