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
            AcceptsEveryNonValidDeclaredStatus();
            RejectsPublicValidStatus();
            RejectsUndefinedStatus((LicenseStatus)(-1), "negative");
            RejectsUndefinedStatus((LicenseStatus)int.MaxValue, "positive");
        }

        private static void AcceptsEveryNonValidDeclaredStatus()
        {
            foreach (LicenseStatus status in Enum.GetValues(typeof(LicenseStatus)))
            {
                if (status == LicenseStatus.Valid) continue;

                var result = new LicenseVerificationResult(status, CreateLicense());
                if (result.Status != status)
                    throw new InvalidOperationException(
                        "LicenseVerificationResultStatusIntegritySmoke declared status changed: expected=" + status + ", actual=" + result.Status + ".");
                if (result.IsValid)
                    throw new InvalidOperationException(
                        "LicenseVerificationResultStatusIntegritySmoke non-valid public result unexpectedly reported IsValid=true for " + status + ".");
            }
        }

        private static void RejectsPublicValidStatus()
        {
            try
            {
                _ = new LicenseVerificationResult(LicenseStatus.Valid, CreateLicense());
            }
            catch (ArgumentException ex) when (string.Equals(ex.ParamName, "status", StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException(
                "LicenseVerificationResultStatusIntegritySmoke expected public construction of LicenseStatus.Valid to fail closed.");
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
