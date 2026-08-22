using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionOwnershipSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => WallJunctionOwnershipSmoke.Run();
    }
}
