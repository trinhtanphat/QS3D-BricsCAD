using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfZipNumericCanonicalityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BcfZipNumericCanonicalitySmoke.Run();
    }
}
