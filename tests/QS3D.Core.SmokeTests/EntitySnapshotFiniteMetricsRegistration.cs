using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class EntitySnapshotFiniteMetricsRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => EntitySnapshotFiniteMetricsSmoke.Run();
    }
}
