using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneServiceRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectZoneServiceSmoke.Run();
    }
}
