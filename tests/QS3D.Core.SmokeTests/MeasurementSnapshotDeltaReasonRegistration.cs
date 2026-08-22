using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementSnapshotDeltaReasonRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MeasurementSnapshotDeltaReasonSmoke.Run();
    }
}
