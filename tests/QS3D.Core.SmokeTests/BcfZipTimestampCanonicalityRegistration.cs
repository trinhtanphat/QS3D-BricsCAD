using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfZipTimestampCanonicalityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BcfZipTimestampCanonicalitySmoke.Run();
    }
}
