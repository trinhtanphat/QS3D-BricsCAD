using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationMatrixDiagnosticsSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityCalculationMatrixDiagnosticsSmoke.Run();
    }
}
