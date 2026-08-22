using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewFilterCanonicalitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticViewFilterCanonicalitySmoke.Run();
    }
}
