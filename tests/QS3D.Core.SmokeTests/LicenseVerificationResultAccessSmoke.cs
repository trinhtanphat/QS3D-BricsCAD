using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseVerificationResultAccessSmoke
    {
        internal static void Run()
        {
            var publicConstructors = typeof(LicenseVerificationResult).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance);

            if (publicConstructors.Length != 0)
                throw new InvalidOperationException(
                    "LicenseVerificationResult must not expose a public constructor that can fabricate a Valid status without LicenseVerifier.Verify().");
        }
    }

    internal static class LicenseVerificationResultAccessRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LicenseVerificationResultAccessSmoke.Run();
        }
    }
}
