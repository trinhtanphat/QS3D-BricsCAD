using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkFamilyPropertyCanonicalityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BulkFamilyPropertyCanonicalitySmoke.Run();
    }
}
