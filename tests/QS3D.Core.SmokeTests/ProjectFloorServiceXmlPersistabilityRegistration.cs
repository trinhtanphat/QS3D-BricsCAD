using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorServiceXmlPersistabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFloorServiceXmlPersistabilitySmoke.Run();
    }
}
