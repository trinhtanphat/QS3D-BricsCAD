using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class BulgeArcMidpointOverflowSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BulgeArcMidpointOverflowSmoke.Run();
    }
}
