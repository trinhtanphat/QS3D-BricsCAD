using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementSnapshotContractRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MeasurementSnapshotContractSmoke.Run();
    }
}
