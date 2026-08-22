using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationSettingsSchemaValidationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityCalculationSettingsSchemaValidationSmoke.Run();
    }
}
