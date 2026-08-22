using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationDeductionGateSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityCalculationDeductionGateSmoke.Run();
    }
}
