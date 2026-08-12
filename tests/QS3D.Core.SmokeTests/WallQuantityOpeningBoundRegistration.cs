using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class WallQuantityOpeningBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => WallQuantityOpeningBoundSmoke.Run();
    }
}
