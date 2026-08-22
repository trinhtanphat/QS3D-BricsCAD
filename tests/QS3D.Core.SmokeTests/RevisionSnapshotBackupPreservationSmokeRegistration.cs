using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotBackupPreservationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RevisionSnapshotBackupPreservationSmoke.Run();
    }
}
