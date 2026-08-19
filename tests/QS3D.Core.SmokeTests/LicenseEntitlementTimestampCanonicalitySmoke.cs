using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseEntitlementTimestampCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsLeadingZeroPersistedTicksWithValidSeal();
            CanonicalPersistedTicksStillRoundTrip();
        }

        private static void RejectsLeadingZeroPersistedTicksWithValidSeal()
        {
            const string Prefix = "persisted-utc-ticks:";
            var source = LicenseEntitlementSnapshot.Create(
                "QS3D",
                "1",
                "machine",
                "payload",
                new DateTime(2026, 8, 18, 16, 0, 0, DateTimeKind.Utc));
            var lines = source.Serialize().Split('\n');
            Require(lines.Length == 7, "canonical snapshot line count changed unexpectedly");
            Require(lines[4].StartsWith(Prefix, StringComparison.Ordinal), "persisted-ticks line was not found");

            var canonicalTicks = lines[4].Substring(Prefix.Length);
            Require(canonicalTicks.Length > 0 && canonicalTicks[0] != '0', "canonical tick fixture unexpectedly starts with zero");
            lines[4] = Prefix + "0" + canonicalTicks;
            var resealedPayload = string.Join("\n", lines, 0, 6);
            lines[6] = "sha256:" + ComputeSha256Hex(resealedPayload);
            var nonCanonical = string.Join("\n", lines);

            Require(
                !LicenseEntitlementSnapshot.TryDeserialize(nonCanonical, out _),
                "leading-zero persisted ticks were accepted as canonical persistence after resealing");
        }

        private static void CanonicalPersistedTicksStillRoundTrip()
        {
            var source = LicenseEntitlementSnapshot.Create(
                "QS3D",
                "1",
                "machine",
                "payload",
                new DateTime(2026, 8, 18, 16, 5, 0, DateTimeKind.Utc));
            var serialized = source.Serialize();

            Require(LicenseEntitlementSnapshot.TryDeserialize(serialized, out var restored), "canonical timestamp snapshot did not deserialize");
            Require(string.Equals(serialized, restored.Serialize(), StringComparison.Ordinal), "canonical timestamp serialization was not stable");
        }

        private static string ComputeSha256Hex(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var digest = sha256.ComputeHash(new UTF8Encoding(false, true).GetBytes(value));
                var builder = new StringBuilder(digest.Length * 2);
                for (var i = 0; i < digest.Length; i++)
                    builder.Append(digest[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("LicenseEntitlementTimestampCanonicalitySmoke: " + message + ".");
        }
    }
}
