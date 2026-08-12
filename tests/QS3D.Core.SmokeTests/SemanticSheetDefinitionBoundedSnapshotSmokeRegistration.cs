using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetDefinitionBoundedSnapshotSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticSheetDefinitionBoundedSnapshotSmoke.Run();
    }
}
