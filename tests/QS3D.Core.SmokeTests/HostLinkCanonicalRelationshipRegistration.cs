using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkCanonicalRelationshipRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => HostLinkCanonicalRelationshipSmoke.Run();
    }
}
