using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseVerificationResultStatusIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AcceptsEveryDeclaredStatus();
            RejectsUndefinedStatus((LicenseStatus)(-1), "negative");
            RejectsUndefinedStatus((LicenseStatus)int.MaxValue, "positive");
        }

        private static void AcceptsEveryDeclaredStatus()
        {
            foreach (LicenseStatus status in Enum.GetValues(typeof(LicenseStatus)))
            {
                var result = new LicenseVerificationResult(status, CreateLicense());
                if (result.Status != status)
                    throw new InvalidOperationException(
                        "LicenseVerificationResultStatusIntegritySmoke declared status changed: expected=" + status + ", actual=" + result.Status + ".");
            }
        }

        private static void RejectsUndefinedStatus(LicenseStatus status, string label)
        {
            try
            {
                _ = new LicenseVerificationResult(status, CreateLicense());
            }
            catch (ArgumentOutOfRangeException ex) when (string.Equals(ex.ParamName, "status", StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException(
                "LicenseVerificationResultStatusIntegritySmoke expected ArgumentOutOfRangeException for " + label + " undefined status " + (int)status + ".");
        }

        private static LicenseDocument CreateLicense() => new LicenseDocument
        {
            LicenseId = "LIC-STATUS",
            CustomerId = "CUSTOMER",
            ProductId = "QS3D",
            NotBeforeUtc = new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc),
            ExpiresUtc = new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc),
            Nonce = "NONCE"
        };
    }
}
