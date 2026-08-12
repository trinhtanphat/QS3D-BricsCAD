using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEmptyPropertyPresenceSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BulkEmptyPropertyPresenceSmoke.Run();
    }
}
