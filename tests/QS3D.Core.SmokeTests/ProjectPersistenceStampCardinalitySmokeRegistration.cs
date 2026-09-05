using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampCardinalitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectPersistenceStampCardinalitySmoke.Run();
    }
}