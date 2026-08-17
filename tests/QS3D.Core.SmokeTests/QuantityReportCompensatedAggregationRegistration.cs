using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportCompensatedAggregationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityReportCompensatedAggregationSmoke.Run();
    }
}
