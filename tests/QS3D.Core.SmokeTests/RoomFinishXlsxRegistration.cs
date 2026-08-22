using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RoomFinishXlsxSmoke.Run();
    }
}
