using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    // Execution hook for MeasurementTraceKnownCountIntegritySmoke.
    // The smoke itself deliberately exposes Run() without a module initializer and
    // SmokeTestRegistration.RunAll() does not invoke it, so this bootstrap is required
    // to ensure the #2728 Count-contract regression actually executes in CI.
    internal static class MeasurementTraceKnownCountIntegritySmokeBootstrap
    {
        [ModuleInitializer]
        internal static void Initialize() => MeasurementTraceKnownCountIntegritySmoke.Run();
    }
}
