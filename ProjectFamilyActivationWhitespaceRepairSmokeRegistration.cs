using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyActivationWhitespaceRepairSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFamilyActivationWhitespaceRepairSmoke.Run();
    }
}
