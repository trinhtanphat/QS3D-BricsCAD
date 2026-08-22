using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTraceInspectorRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MeasurementTraceInspectorSmoke.Run();
    }
}
