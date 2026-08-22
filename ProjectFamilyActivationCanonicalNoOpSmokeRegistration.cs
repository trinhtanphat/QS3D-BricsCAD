using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActivationCanonicalNoOpSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFamilyActivationCanonicalNoOpSmoke.Run();
    }
}
