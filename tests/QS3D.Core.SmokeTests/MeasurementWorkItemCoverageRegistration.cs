using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemCoverageRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MeasurementWorkItemCoverageSmoke.Run();
    }
}
