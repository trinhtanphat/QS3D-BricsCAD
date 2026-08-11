using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorZoneMutationIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFloorZoneMutationIntegritySmoke.Run();
    }
}
