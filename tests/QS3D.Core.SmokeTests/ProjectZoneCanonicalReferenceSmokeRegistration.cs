using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneCanonicalReferenceSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectZoneCanonicalReferenceSmoke.Run();
        }
    }
}
