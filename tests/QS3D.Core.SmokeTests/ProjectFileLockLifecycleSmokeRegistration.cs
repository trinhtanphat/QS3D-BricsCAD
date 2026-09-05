using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFileLockLifecycleSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFileLockLifecycleSmoke.Run();
    }
}
