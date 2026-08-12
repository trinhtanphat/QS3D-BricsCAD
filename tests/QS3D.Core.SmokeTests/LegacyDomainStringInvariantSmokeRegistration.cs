using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class LegacyDomainStringInvariantSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => LegacyDomainStringInvariantSmoke.Run();
    }
}
