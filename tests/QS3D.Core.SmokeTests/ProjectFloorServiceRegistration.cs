using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorServiceRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFloorServiceSmoke.Run();
    }
}
