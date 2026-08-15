using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ZoneDefinitionXmlPersistabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ZoneDefinitionXmlPersistabilitySmoke.Run();
    }
}
