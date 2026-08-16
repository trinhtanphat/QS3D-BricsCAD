using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class FeatureFlagsCanonicalIdentifierRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => FeatureFlagsCanonicalIdentifierSmoke.Run();
    }
}
