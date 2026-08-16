using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class FeatureFlagsCanonicalIdentityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => FeatureFlagsCanonicalIdentitySmoke.Run();
    }
}
