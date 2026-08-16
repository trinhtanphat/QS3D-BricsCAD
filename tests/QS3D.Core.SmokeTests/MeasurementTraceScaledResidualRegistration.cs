using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTraceScaledResidualRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MeasurementTraceScaledResidualSmoke.Run();
    }
}
