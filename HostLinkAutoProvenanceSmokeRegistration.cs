using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkAutoProvenanceSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => HostLinkAutoProvenanceSmoke.Run();
    }
}
