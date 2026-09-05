using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbBackupFallbackRecoveryReasonSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbBackupFallbackRecoveryReasonSmoke.Run();
    }
}
