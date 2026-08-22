using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorGeneratedIdentitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => FloorGeneratedIdentitySmoke.Run();
    }
}
