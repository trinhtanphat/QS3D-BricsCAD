using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewDefinitionBoundedSnapshotSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticViewDefinitionBoundedSnapshotSmoke.Run();
    }
}
