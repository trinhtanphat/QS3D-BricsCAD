using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTraceKnownCountIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MeasurementTraceKnownCountIntegritySmoke.Run();
    }
}
