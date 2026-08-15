using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemMappingTokenXmlPersistabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MeasurementWorkItemMappingTokenXmlPersistabilitySmoke.Run();
    }
}
