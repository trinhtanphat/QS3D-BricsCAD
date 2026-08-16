using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseVerifierProductInputCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsMalformedExpectedProductIds();
            PreservesCanonicalMismatchClassification();
            PreservesCanonicalMatchVerificationFlow();
        }

        private static void RejectsMalformedExpectedProductIds()
        {
            RejectExpectedProduct(" QS3D ", "padded");
            RejectExpectedProduct("QS\u0001D", "control character");
            RejectExpectedProduct(new string('P', 129), "overlength");
            RejectExpectedProduct("QS3D\uD800", "malformed Unicode");
        }

        private static void PreservesCanonicalMismatchClassification()
        {
            using (var rsa = RSA.Create())
            {
                rsa.KeySize = 2048;
                var license = CreateLicense("OTHER-PRODUCT");
                license.Signature = rsa.SignData(
                    license.CanonicalPayload(),
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                var result = new LicenseVerifier().Verify(
                    license,
                    rsa.ExportParameters(false),
                    "QS3D",
                    VerificationTime());
                if (result.Status != LicenseStatus.ProductMismatch)
                    throw new InvalidOperationException(
                        "LicenseVerifierProductInputCanonicalitySmoke canonical mismatch expected ProductMismatch, actual=" + result.Status + ".");
            }
        }

        private static void PreservesCanonicalMatchVerificationFlow()
        {
            var result = new LicenseVerifier().Verify(
                CreateLicense(),
                default(RSAParameters),
                "QS3D",
                VerificationTime());
            if (result.Status != LicenseStatus.InvalidSignature)
                throw new InvalidOperationException(
                    "LicenseVerifierProductInputCanonicalitySmoke canonical match should continue to signature verification, actual=" + result.Status + ".");
        }

        private static void RejectExpectedProduct(string expectedProductId, string label)
        {
            try
            {
                new LicenseVerifier().Verify(
                    CreateLicense(),
                    default(RSAParameters),
                    expectedProductId,
                    VerificationTime());
            }
            catch (ArgumentException ex) when (string.Equals(ex.ParamName, "expectedProductId", StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException(
                "LicenseVerifierProductInputCanonicalitySmoke expected ArgumentException for " + label + " expectedProductId.");
        }

        private static LicenseDocument CreateLicense(string productId = "QS3D")
        {
            var now = VerificationTime();
            return new LicenseDocument
            {
                LicenseId = "LIC-PRODUCT-INPUT",
                CustomerId = "CUSTOMER",
                ProductId = productId,
                NotBeforeUtc = now.AddDays(-1),
                ExpiresUtc = now.AddDays(1),
                Nonce = "NONCE"
            };
        }

        private static DateTime VerificationTime() =>
            new DateTime(2026, 8, 12, 3, 0, 0, DateTimeKind.Utc);
    }
}
