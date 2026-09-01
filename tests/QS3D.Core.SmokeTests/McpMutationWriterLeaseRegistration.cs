using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class McpMutationWriterLeaseRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => McpMutationWriterLeaseSmoke.Run();
    }
}
