using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyHealthRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DependencyHealthSmoke.Run();
    }
}
