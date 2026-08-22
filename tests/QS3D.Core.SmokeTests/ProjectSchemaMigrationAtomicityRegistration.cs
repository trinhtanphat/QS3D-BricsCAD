using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSchemaMigrationAtomicityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectSchemaMigrationAtomicitySmoke.Run();
    }
}
