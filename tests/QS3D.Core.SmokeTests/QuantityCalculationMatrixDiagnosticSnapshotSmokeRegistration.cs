using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityCalculationMatrixDiagnosticSnapshotSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityCalculationMatrixDiagnosticSnapshotSmoke.Run();
    }
}
