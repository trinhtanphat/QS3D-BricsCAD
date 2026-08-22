using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class GridIntersectionIdentitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => GridIntersectionIdentitySmoke.Run();
    }
}
