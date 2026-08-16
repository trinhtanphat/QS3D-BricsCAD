using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class WallQuantityOpeningCompensationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => WallQuantityOpeningCompensationSmoke.Run();
    }
}
