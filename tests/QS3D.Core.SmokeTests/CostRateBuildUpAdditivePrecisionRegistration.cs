using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class CostRateBuildUpAdditivePrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CostRateBuildUpAdditivePrecisionSmoke.Run();
    }
}
