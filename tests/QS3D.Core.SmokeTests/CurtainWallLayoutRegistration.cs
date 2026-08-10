using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallLayoutRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainWallLayoutSmoke.Run();
    }
}
