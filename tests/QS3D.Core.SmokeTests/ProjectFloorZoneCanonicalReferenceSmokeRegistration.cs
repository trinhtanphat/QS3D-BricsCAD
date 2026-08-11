using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorZoneCanonicalReferenceSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFloorZoneCanonicalReferenceSmoke.Run();
    }
}
