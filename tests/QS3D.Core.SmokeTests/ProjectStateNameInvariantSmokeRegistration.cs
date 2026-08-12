using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateNameInvariantSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectStateNameInvariantSmoke.Run();
    }
}
