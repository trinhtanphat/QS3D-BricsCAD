using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseVerifierSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => LicenseVerifierSmoke.Run();
    }
}
