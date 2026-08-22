using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationSettingsCloneValidationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityCalculationSettingsCloneValidationSmoke.Run();
    }
}
