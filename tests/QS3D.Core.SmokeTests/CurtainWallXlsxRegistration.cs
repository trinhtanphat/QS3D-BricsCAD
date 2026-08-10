using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallXlsxRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainWallXlsxSmoke.Run();
    }
}
