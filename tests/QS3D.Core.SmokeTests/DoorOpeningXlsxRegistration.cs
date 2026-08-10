using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningXlsxRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DoorOpeningXlsxSmoke.Run();
    }
}
