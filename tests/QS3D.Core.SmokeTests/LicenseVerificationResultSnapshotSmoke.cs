using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseVerificationResultSnapshotSmoke
    {
        public static void Run()
        {
            VerifiedPayloadIsDetachedFromInputAndReturnedCopies();
        }

        private static void VerifiedPayloadIsDetachedFromInputAndReturnedCopies()
        {
            var now = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc);
            var license = new LicenseDocument
            {
                LicenseId = "LIC-001",
                CustomerId = "CUSTOMER-001",
                ProductId = "QS3D",
                NotBeforeUtc = now.AddDays(-1),
                ExpiresUtc = now.AddDays(30),
                Nonce = "NONCE-001"
            };
            license.Features.Add("Core");
            license.Features.Add("Rebar");

            RSAParameters publicKey;
            using (var rsa = RSA.Create())
            {
                rsa.KeySize = 2048;
                license.Signature = rsa.SignData(
                    license.CanonicalPayload(),
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                publicKey = rsa.ExportParameters(false);
            }

            var expectedSignature = (byte[])license.Signature.Clone();
            var result = new LicenseVerifier().Verify(license, publicKey, "QS3D", now);
            if (!result.IsValid || result.Status != LicenseStatus.Valid)
                throw new InvalidOperationException("Signed baseline license did not verify as valid.");

            license.LicenseId = "MUTATED-LICENSE";
            license.ProductId = "OTHER-PRODUCT";
            license.Features.Clear();
            license.Features.Add("Mutated");
            license.Signature[0] ^= 0x5A;

            var firstRead = result.License;
            AssertOriginalSnapshot(firstRead, expectedSignature, "after original input mutation");

            firstRead.LicenseId = "RETURNED-COPY-MUTATION";
            firstRead.ProductId = "RETURNED-COPY-PRODUCT";
            firstRead.Features.Clear();
            firstRead.Features.Add("ReturnedCopyMutation");
            firstRead.Signature[0] ^= 0x33;

            var secondRead = result.License;
            AssertOriginalSnapshot(secondRead, expectedSignature, "after returned copy mutation");
            if (ReferenceEquals(firstRead, secondRead))
                throw new InvalidOperationException("License verification result returned the same mutable LicenseDocument instance twice.");
            if (ReferenceEquals(firstRead.Signature, secondRead.Signature))
                throw new InvalidOperationException("License verification result reused the same mutable signature byte array across reads.");
        }

        private static void AssertOriginalSnapshot(LicenseDocument snapshot, byte[] expectedSignature, string label)
        {
            if (!string.Equals(snapshot.LicenseId, "LIC-001", StringComparison.Ordinal) ||
                !string.Equals(snapshot.CustomerId, "CUSTOMER-001", StringComparison.Ordinal) ||
                !string.Equals(snapshot.ProductId, "QS3D", StringComparison.Ordinal) ||
                !string.Equals(snapshot.Nonce, "NONCE-001", StringComparison.Ordinal))
                throw new InvalidOperationException("License verification snapshot identity changed " + label + ".");

            if (snapshot.Features.Count != 2 ||
                !snapshot.Features.Contains("Core") ||
                !snapshot.Features.Contains("Rebar"))
                throw new InvalidOperationException("License verification snapshot features changed " + label + ".");

            if (!snapshot.Signature.SequenceEqual(expectedSignature))
                throw new InvalidOperationException("License verification snapshot signature changed " + label + ".");
        }
    }

    internal static class LicenseVerificationResultSnapshotSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LicenseVerificationResultSnapshotSmoke.Run();
        }
    }
}
