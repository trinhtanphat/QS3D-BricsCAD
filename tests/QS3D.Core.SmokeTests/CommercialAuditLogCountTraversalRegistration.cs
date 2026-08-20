using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialAuditLogCountTraversalRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CommercialAuditLogCountTraversalSmoke.Run();
    }
}
