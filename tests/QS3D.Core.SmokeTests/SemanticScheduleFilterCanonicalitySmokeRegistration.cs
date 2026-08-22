using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleFilterCanonicalitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticScheduleFilterCanonicalitySmoke.Run();
    }
}
