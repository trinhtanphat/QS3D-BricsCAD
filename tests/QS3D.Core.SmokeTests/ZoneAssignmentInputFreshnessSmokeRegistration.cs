using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ZoneAssignmentInputFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ZoneAssignmentInputFreshnessSmoke.Run();
    }
}
