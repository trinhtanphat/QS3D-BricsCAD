using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActivationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFamilyActivationSmoke.Run();
    }
}
