using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphRebuildInputFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DependencyGraphRebuildInputFreshnessSmoke.Run();
    }
}
