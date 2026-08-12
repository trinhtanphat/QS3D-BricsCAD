using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailNullBackingIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => AuditTrailNullBackingIntegritySmoke.Run();
    }
}
