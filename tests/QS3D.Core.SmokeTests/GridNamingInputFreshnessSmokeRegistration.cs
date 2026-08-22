using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingInputFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => GridNamingInputFreshnessSmoke.Run();
    }
}
