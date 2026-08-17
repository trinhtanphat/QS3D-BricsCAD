using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorMutationTargetBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFloorMutationTargetBoundSmoke.Run();
    }
}
