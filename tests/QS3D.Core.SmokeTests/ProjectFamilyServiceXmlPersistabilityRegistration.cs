using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyServiceXmlPersistabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFamilyServiceXmlPersistabilitySmoke.Run();
    }
}
