using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActiveDeleteCanonicalSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFamilyActiveDeleteCanonicalSmoke.Run();
    }
}
