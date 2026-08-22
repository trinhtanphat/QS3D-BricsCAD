using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.Domain
{
    public sealed class FloorGeneratedIdentity
    {
        internal FloorGeneratedIdentity(
            string floorId,
            string displayName,
            double elevationM,
            string ownerKey,
            string ownerToken,
            string stateKey,
            string stateToken)
        {
            FloorId = floorId;
            DisplayName = displayName;
            ElevationM = elevationM;
            OwnerKey = ownerKey;
            OwnerToken = ownerToken;
            StateKey = stateKey;
            StateToken = stateToken;
        }

        public string FloorId { get; }
        public string DisplayName { get; }
        public double ElevationM { get; }
        public string OwnerKey { get; }
        public string OwnerToken { get; }
        public string StateKey { get; }
        public string StateToken { get; }
    }

    public static class FloorGeneratedIdentityPlanner
    {
        private const int MaxFloorIdLength = 64;
        private const int MaxFloorNameLength = 120;
        private const string OwnerTokenPrefix = "LVO1:";
        private const string StateTokenPrefix = "LVS1:";

        public static FloorGeneratedIdentity Create(FloorDefinition floor)
        {
            if (floor == null) throw new ArgumentNullException(nameof(floor));
            var floorId = CanonicalFloorId(floor.Id);
            var displayName = NormalizeName(floor.Name);
            var elevation = Finite(floor.ElevationM, nameof(floor.ElevationM));

            var ownerKey = floorId.Length + ":" + floorId;
            var ownerToken = OwnerTokenPrefix + Sha256Hex(ownerKey);
            var elevationText = elevation.ToString("R", CultureInfo.InvariantCulture);
            var stateKey = ownerKey + "|" + displayName.Length + ":" + displayName + "|" + elevationText;
            var stateToken = StateTokenPrefix + Sha256Hex(stateKey);

            return new FloorGeneratedIdentity(
                floorId,
                displayName,
                elevation,
                ownerKey,
                ownerToken,
                stateKey,
                stateToken);
        }

        public static string BuildOwnerToken(string floorId)
        {
            var canonical = CanonicalFloorId(floorId);
            return OwnerTokenPrefix + Sha256Hex(canonical.Length + ":" + canonical);
        }

        private static string CanonicalFloorId(string value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0 || normalized.Length > MaxFloorIdLength)
                throw new ArgumentException("Floor id must contain 1.." + MaxFloorIdLength + " characters.", nameof(value));
            return normalized.ToUpperInvariant();
        }

        private static string NormalizeName(string value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0 || normalized.Length > MaxFloorNameLength)
                throw new ArgumentException("Floor name must contain 1.." + MaxFloorNameLength + " characters.", nameof(value));
            return normalized;
        }

        private static double Finite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "Floor elevation must be finite.");
            return value;
        }

        private static string Sha256Hex(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var item in hash) builder.Append(item.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
