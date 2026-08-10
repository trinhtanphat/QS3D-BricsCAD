using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyServiceRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFamilyServiceSmoke.Run();
    }
}
