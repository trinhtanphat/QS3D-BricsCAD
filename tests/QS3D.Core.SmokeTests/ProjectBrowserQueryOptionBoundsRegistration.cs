using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserQueryOptionBoundsRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectBrowserQueryOptionBoundsSmoke.Run();
    }
}