using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationSettingsCardinalitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityCalculationSettingsCardinalitySmoke.Run();
    }
}
