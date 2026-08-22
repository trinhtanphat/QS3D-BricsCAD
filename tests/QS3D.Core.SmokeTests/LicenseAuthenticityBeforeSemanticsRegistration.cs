using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseAuthenticityBeforeSemanticsRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => LicenseAuthenticityBeforeSemanticsSmoke.Run();
    }
}
