using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbBackupFallbackProjectIdentitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbBackupFallbackProjectIdentitySmoke.Run();
    }
}
