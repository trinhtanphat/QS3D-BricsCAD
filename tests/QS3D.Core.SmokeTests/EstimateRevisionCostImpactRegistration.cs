using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimateRevisionCostImpactRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => EstimateRevisionCostImpactSmoke.Run();
    }
}
