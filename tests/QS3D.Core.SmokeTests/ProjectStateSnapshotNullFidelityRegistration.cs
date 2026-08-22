using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotNullFidelityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectStateSnapshotNullFidelitySmoke.Run();
    }
}
