using System;
using System.Security.Cryptography;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseAuthenticityBeforeSemanticsSmoke
    {
        internal static void Run()
        {
            using var rsa = RSA.Create();
            rsa.KeySize = 2048;
            var publicKey = rsa.ExportParameters(false);
            var verifier = new LicenseVerifier();
            var nowUtc = new DateTime(2030, 1, 2, 0, 0, 0, DateTimeKind.Utc);

            var tamperedProduct = CreateSigned(
                rsa,
                "QS3D",
                new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2030, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                "tampered-product");
            tamperedProduct.ProductId = "OTHER";
            RequireStatus(
                LicenseStatus.InvalidSignature,
                verifier.Verify(tamperedProduct, publicKey, "QS3D", nowUtc),
                "Tampering ProductId must be classified as InvalidSignature before ProductMismatch.");

            var tamperedExpiry = CreateSigned(
                rsa,
                "QS3D",
                new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2030, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                "tampered-expiry");
            tamperedExpiry.ExpiresUtc = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            RequireStatus(
                LicenseStatus.InvalidSignature,
                verifier.Verify(tamperedExpiry, publicKey, "QS3D", nowUtc),
                "Tampering ExpiresUtc must be classified as InvalidSignature before Expired.");

            var productMismatch = CreateSigned(
                rsa,
                "OTHER",
                new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2030, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                "signed-mismatch");
            RequireStatus(
                LicenseStatus.ProductMismatch,
                verifier.Verify(productMismatch, publicKey, "QS3D", nowUtc),
                "A validly signed license for another product must still return ProductMismatch.");

            var notYetValid = CreateSigned(
                rsa,
                "QS3D",
                new DateTime(2030, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2030, 1, 4, 0, 0, 0, DateTimeKind.Utc),
                "signed-future");
            RequireStatus(
                LicenseStatus.NotYetValid,
                verifier.Verify(notYetValid, publicKey, "QS3D", nowUtc),
                "A validly signed future license must still return NotYetValid.");

            var expired = CreateSigned(
                rsa,
                "QS3D",
                new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2030, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                "signed-expired");
            RequireStatus(
                LicenseStatus.Expired,
                verifier.Verify(expired, publicKey, "QS3D", nowUtc),
                "A validly signed expired license must still return Expired.");

            var valid = CreateSigned(
                rsa,
                "QS3D",
                new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2030, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                "signed-valid");
            RequireStatus(
                LicenseStatus.Valid,
                verifier.Verify(valid, publicKey, "QS3D", nowUtc),
                "A validly signed in-window license must remain Valid.");
        }

        private static LicenseDocument CreateSigned(
            RSA rsa,
            string productId,
            DateTime notBeforeUtc,
            DateTime expiresUtc,
            string nonce)
        {
            var license = new LicenseDocument
            {
                LicenseId = "smoke-license",
                CustomerId = "smoke-customer",
                ProductId = productId,
                NotBeforeUtc = notBeforeUtc,
                ExpiresUtc = expiresUtc,
                Nonce = nonce
            };
            license.Signature = rsa.SignData(
                license.CanonicalPayload(),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            return license;
        }

        private static void RequireStatus(
            LicenseStatus expected,
            LicenseVerificationResult actual,
            string message)
        {
            if (actual.Status != expected)
                throw new InvalidOperationException(message + " Expected " + expected + ", got " + actual.Status + ".");
        }
    }
}
