using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallRegeneratorRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainWallRegeneratorSmoke.Run();
    }
}
