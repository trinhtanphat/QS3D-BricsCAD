using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampScalarDirtyRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectPersistenceStampScalarDirtySmoke.Run();
    }
}
