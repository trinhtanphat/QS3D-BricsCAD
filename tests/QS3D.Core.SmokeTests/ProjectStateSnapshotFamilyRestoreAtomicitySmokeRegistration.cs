using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotFamilyRestoreAtomicitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectStateSnapshotFamilyRestoreAtomicitySmoke.Run();
    }
}
