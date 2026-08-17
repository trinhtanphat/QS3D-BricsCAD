using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfNumericCanonicalityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BcfNumericCanonicalitySmoke.Run();
    }
}
