using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneServiceXmlPersistabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectZoneServiceXmlPersistabilitySmoke.Run();
    }
}
