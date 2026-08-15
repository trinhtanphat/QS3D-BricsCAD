using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementKeyXmlPersistabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectElementKeyXmlPersistabilitySmoke.Run();
    }
}
