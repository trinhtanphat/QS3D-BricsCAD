using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.Licensing
{
    /// <summary>
    /// Canonical persistence envelope for an already-issued entitlement payload.
    /// The SHA-256 seal is an integrity checksum, not a replacement for server signature verification.
    /// </summary>
    public sealed class LicenseEntitlementSnapshot
    {
        private const string Header = "QS3D-LICENSE-ENTITLEMENT/1";
        private const int MaxSerializedChars = 96 * 1024;
        private const int MaxProductBytes = 128;
        private const int MaxVersionBytes = 128;
        private const int MaxMachineIdBytes = 256;
        private const int MaxPayloadBytes = 48 * 1024;
        private static readonly UTF8Encoding StrictUtf8 = CreateStrictUtf8();

        private LicenseEntitlementSnapshot(string product, string productVersion, string machineId, string entitlementPayload, DateTime persistedAtUtc)
        {
            Product = product;
            ProductVersion = productVersion;
            MachineId = machineId;
            EntitlementPayload = entitlementPayload;
            PersistedAtUtc = persistedAtUtc;
        }

        public string Product { get; }
        public string ProductVersion { get; }
        public string MachineId { get; }
        public string EntitlementPayload { get; }
        public DateTime PersistedAtUtc { get; }

        public static LicenseEntitlementSnapshot Create(
            string product,
            string productVersion,
            string machineId,
            string entitlementPayload,
            DateTime persistedAt)
        {
            var canonicalProduct = RequireCanonicalIdentity(product, nameof(product), MaxProductBytes);
            var canonicalVersion = RequireCanonicalIdentity(productVersion, nameof(productVersion), MaxVersionBytes);
            var canonicalMachineId = RequireCanonicalIdentity(machineId, nameof(machineId), MaxMachineIdBytes);
            var payload = RequirePayload(entitlementPayload);

            if (persistedAt.Kind == DateTimeKind.Unspecified)
                throw new ArgumentException("Persistence timestamp must have an explicit time zone.", nameof(persistedAt));

            var persistedAtUtc = persistedAt.Kind == DateTimeKind.Utc ? persistedAt : persistedAt.ToUniversalTime();
            return new LicenseEntitlementSnapshot(canonicalProduct, canonicalVersion, canonicalMachineId, payload, persistedAtUtc);
        }

        public string Serialize()
        {
            var canonical = BuildCanonical(Product, ProductVersion, MachineId, EntitlementPayload, PersistedAtUtc);
            return canonical + "\nsha256:" + ComputeSha256Hex(canonical);
        }

        public static bool TryDeserialize(string serialized, out LicenseEntitlementSnapshot snapshot)
        {
            snapshot = null!;
            if (string.IsNullOrEmpty(serialized) || serialized.Length > MaxSerializedChars)
                return false;
            if (serialized.IndexOf('\r') >= 0 || serialized.EndsWith("\n", StringComparison.Ordinal))
                return false;

            var lines = serialized.Split('\n');
            if (lines.Length != 7 || !string.Equals(lines[0], Header, StringComparison.Ordinal))
                return false;
            if (!TryReadValue(lines[1], "product:", MaxProductBytes, out var product) ||
                !TryReadValue(lines[2], "version:", MaxVersionBytes, out var version) ||
                !TryReadValue(lines[3], "machine:", MaxMachineIdBytes, out var machineId) ||
                !TryReadUtcTicks(lines[4], out var persistedAtUtc) ||
                !TryReadValue(lines[5], "payload:", MaxPayloadBytes, out var payload) ||
                !TryReadSeal(lines[6], out var actualSeal))
                return false;

            var canonical = string.Join("\n", lines, 0, 6);
            var expectedSeal = ComputeSha256(canonical);
            if (!FixedTimeEquals(expectedSeal, actualSeal))
                return false;

            try
            {
                snapshot = Create(product, version, machineId, payload, persistedAtUtc);
                return true;
            }
            catch (ArgumentException)
            {
                snapshot = null!;
                return false;
            }
        }

        private static string BuildCanonical(string product, string version, string machineId, string payload, DateTime persistedAtUtc)
        {
            return Header +
                   "\nproduct:" + Encode(product) +
                   "\nversion:" + Encode(version) +
                   "\nmachine:" + Encode(machineId) +
                   "\npersisted-utc-ticks:" + persistedAtUtc.Ticks.ToString(CultureInfo.InvariantCulture) +
                   "\npayload:" + Encode(payload);
        }

        private static bool TryReadUtcTicks(string line, out DateTime value)
        {
            const string Prefix = "persisted-utc-ticks:";
            value = default(DateTime);
            if (!line.StartsWith(Prefix, StringComparison.Ordinal))
                return false;
            if (!long.TryParse(line.Substring(Prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var ticks))
                return false;
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                return false;
            value = new DateTime(ticks, DateTimeKind.Utc);
            return true;
        }

        private static bool TryReadValue(string line, string prefix, int maxBytes, out string value)
        {
            value = null!;
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
                return false;
            try
            {
                var bytes = Convert.FromBase64String(line.Substring(prefix.Length));
                if (bytes.Length == 0 || bytes.Length > maxBytes)
                    return false;
                value = StrictUtf8.GetString(bytes);
                return value.Trim().Length != 0 && string.Equals(value, value.Trim(), StringComparison.Ordinal);
            }
            catch (FormatException) { return false; }
            catch (DecoderFallbackException) { return false; }
        }

        private static bool TryReadSeal(string line, out byte[] seal)
        {
            const string Prefix = "sha256:";
            seal = null!;
            if (!line.StartsWith(Prefix, StringComparison.Ordinal))
                return false;
            var hex = line.Substring(Prefix.Length);
            if (hex.Length != 64)
                return false;
            var bytes = new byte[32];
            for (var i = 0; i < bytes.Length; i++)
            {
                if (!byte.TryParse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]))
                    return false;
            }
            seal = bytes;
            return true;
        }

        private static string RequireCanonicalIdentity(string value, string parameterName, int maxBytes)
        {
            if (value == null) throw new ArgumentNullException(parameterName);
            var normalized = value.Trim();
            if (normalized.Length == 0) throw new ArgumentException("Value must not be blank.", parameterName);
            if (!string.Equals(value, normalized, StringComparison.Ordinal))
                throw new ArgumentException("Value must not contain leading or trailing whitespace.", parameterName);

            for (var i = 0; i < normalized.Length; i++)
            {
                if (char.IsControl(normalized[i]))
                    throw new ArgumentException("Value must not contain control characters.", parameterName);
            }

            try
            {
                if (StrictUtf8.GetByteCount(normalized) > maxBytes)
                    throw new ArgumentException("Value exceeds the persistence bound.", parameterName);
            }
            catch (EncoderFallbackException)
            {
                throw new ArgumentException("Value contains invalid Unicode.", parameterName);
            }

            return normalized;
        }

        private static string RequirePayload(string payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (payload.Length == 0) throw new ArgumentException("Entitlement payload must not be empty.", nameof(payload));
            if (StrictUtf8.GetByteCount(payload) > MaxPayloadBytes) throw new ArgumentException("Entitlement payload exceeds the persistence bound.", nameof(payload));
            return payload;
        }

        private static string Encode(string value) => Convert.ToBase64String(StrictUtf8.GetBytes(value));

        private static string ComputeSha256Hex(string value)
        {
            var digest = ComputeSha256(value);
            var builder = new StringBuilder(digest.Length * 2);
            for (var i = 0; i < digest.Length; i++) builder.Append(digest[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static byte[] ComputeSha256(string value)
        {
            using (var sha256 = SHA256.Create())
                return sha256.ComputeHash(StrictUtf8.GetBytes(value));
        }

        private static UTF8Encoding CreateStrictUtf8()
        {
            var encoding = (UTF8Encoding)new UTF8Encoding(false).Clone();
            encoding.EncoderFallback = EncoderFallback.ExceptionFallback;
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
            return encoding;
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            var difference = 0;
            for (var i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }
}
