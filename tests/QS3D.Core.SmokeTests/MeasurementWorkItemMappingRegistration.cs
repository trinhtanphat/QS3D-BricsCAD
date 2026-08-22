using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemMappingRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MeasurementWorkItemMappingSmoke.Run();
    }
}
