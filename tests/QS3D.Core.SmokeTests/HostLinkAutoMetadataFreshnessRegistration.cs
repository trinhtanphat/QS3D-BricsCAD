using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkAutoMetadataFreshnessRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => HostLinkAutoMetadataFreshnessSmoke.Run();
    }
}
