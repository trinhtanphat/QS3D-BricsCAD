using System;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseEntitlementSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RoundTripsCanonicalSnapshot();
            RejectsNonCanonicalIdentities();
            RejectsTamperedPayload();
            RejectsNonCanonicalSealText();
            RejectsMalformedAndOversizedPersistence();
            RejectsInvalidUtf16BeforeCanonicalization();
            NormalizesExplicitLocalTimestamp();
            RejectsAmbiguousTimestamp();
        }

        private static void RoundTripsCanonicalSnapshot()
        {
            var instant = new DateTime(2026, 8, 16, 18, 20, 0, DateTimeKind.Utc);
            var source = LicenseEntitlementSnapshot.Create("QS3D", "0.1-preview", "machine-01", "signed-entitlement-payload", instant);
            var serialized = source.Serialize();

            Require(LicenseEntitlementSnapshot.TryDeserialize(serialized, out var restored), "canonical snapshot did not deserialize");
            Equal("QS3D", restored.Product, "product changed during round-trip");
            Equal("0.1-preview", restored.ProductVersion, "version changed during round-trip");
            Equal("machine-01", restored.MachineId, "machine id changed during round-trip");
            Equal("signed-entitlement-payload", restored.EntitlementPayload, "payload changed during round-trip");
            Require(restored.PersistedAtUtc == instant && restored.PersistedAtUtc.Kind == DateTimeKind.Utc, "UTC timestamp changed during round-trip");
            Equal(serialized, restored.Serialize(), "canonical serialization was not stable");
        }

        private static void RejectsNonCanonicalIdentities()
        {
            var instant = new DateTime(2026, 8, 16, 18, 20, 0, DateTimeKind.Utc);
            RequireThrows<ArgumentException>(
                () => LicenseEntitlementSnapshot.Create(" QS3D", "1", "machine", "payload", instant),
                "padded product identity was silently normalized");
            RequireThrows<ArgumentException>(
                () => LicenseEntitlementSnapshot.Create("QS3D", "1 ", "machine", "payload", instant),
                "padded version identity was silently normalized");
            RequireThrows<ArgumentException>(
                () => LicenseEntitlementSnapshot.Create("QS3D", "1", " machine ", "payload", instant),
                "padded machine identity was silently normalized");
        }

        private static void RejectsTamperedPayload()
        {
            var source = LicenseEntitlementSnapshot.Create("QS3D", "1", "machine", "signed-A", DateTime.UtcNow);
            var serialized = source.Serialize();
            var tampered = serialized.Replace("c2lnbmVkLUE=", "c2lnbmVkLUI=");

            Require(!string.Equals(serialized, tampered, StringComparison.Ordinal), "tamper fixture did not alter serialized payload");
            Require(!LicenseEntitlementSnapshot.TryDeserialize(tampered, out _), "tampered payload passed the integrity seal");
        }

        private static void RejectsNonCanonicalSealText()
        {
            var source = LicenseEntitlementSnapshot.Create("QS3D", "1", "machine", "payload", new DateTime(2026, 8, 17, 3, 0, 0, DateTimeKind.Utc));
            var serialized = source.Serialize();
            const string Prefix = "sha256:";
            var sealIndex = serialized.LastIndexOf(Prefix, StringComparison.Ordinal);
            Require(sealIndex >= 0, "canonical seal prefix was not found");
            var sealStart = sealIndex + Prefix.Length;
            var upperSeal = serialized.Substring(sealStart).ToUpperInvariant();
            var nonCanonical = serialized.Substring(0, sealStart) + upperSeal;

            Require(!string.Equals(serialized, nonCanonical, StringComparison.Ordinal), "uppercase seal fixture did not alter canonical text");
            Require(!LicenseEntitlementSnapshot.TryDeserialize(nonCanonical, out _), "uppercase SHA-256 seal text was accepted as canonical persistence");
        }

        private static void RejectsMalformedAndOversizedPersistence()
        {
            Require(!LicenseEntitlementSnapshot.TryDeserialize("QS3D-LICENSE-ENTITLEMENT/1\n", out _), "truncated snapshot was accepted");
            Require(!LicenseEntitlementSnapshot.TryDeserialize(new string('x', 96 * 1024 + 1), out _), "oversized serialized snapshot was accepted");

            var oversizedPayload = new string('p', 48 * 1024 + 1);
            RequireThrows<ArgumentException>(() => LicenseEntitlementSnapshot.Create("QS3D", "1", "machine", oversizedPayload, DateTime.UtcNow), "oversized payload was accepted");
        }

        private static void RejectsInvalidUtf16BeforeCanonicalization()
        {
            const string InvalidUtf16 = "bad-\uD800-value";
            RequireThrows<ArgumentException>(
                () => LicenseEntitlementSnapshot.Create(InvalidUtf16, "1", "machine", "payload", DateTime.UtcNow),
                "invalid UTF-16 product identity did not fail with argument validation");
            RequireThrows<EncoderFallbackException>(
                () => LicenseEntitlementSnapshot.Create("QS3D", "1", "machine", InvalidUtf16, DateTime.UtcNow),
                "invalid UTF-16 entitlement payload was replacement-encoded");
        }

        private static void NormalizesExplicitLocalTimestamp()
        {
            var local = new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Local);
            var snapshot = LicenseEntitlementSnapshot.Create("QS3D", "1", "machine", "payload", local);
            Require(snapshot.PersistedAtUtc.Kind == DateTimeKind.Utc, "explicit local timestamp was not normalized to UTC");
            Require(snapshot.PersistedAtUtc == local.ToUniversalTime(), "normalized UTC timestamp is incorrect");
        }

        private static void RejectsAmbiguousTimestamp()
        {
            RequireThrows<ArgumentException>(
                () => LicenseEntitlementSnapshot.Create("QS3D", "1", "machine", "payload", new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Unspecified)),
                "unspecified timestamp was accepted");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("LicenseEntitlementSnapshotSmoke: " + message + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("LicenseEntitlementSnapshotSmoke: " + message + ".");
        }

        private static void RequireThrows<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("LicenseEntitlementSnapshotSmoke: " + message + ".");
        }
    }
}
