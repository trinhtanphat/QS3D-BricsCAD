using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class AutomaticRoomLifecycleRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AutomaticRoomLifecycleSmoke.Run();
        }
    }
}
