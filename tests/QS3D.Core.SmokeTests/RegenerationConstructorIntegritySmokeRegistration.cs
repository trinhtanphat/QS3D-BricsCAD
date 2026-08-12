using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationConstructorIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RegenerationConstructorIntegritySmoke.Run();
    }
}
