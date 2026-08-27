using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QS3D.Core.Geometry
{
    public sealed class GridIntersectionMarkerRecordEntry
    {
        public GridIntersectionMarkerRecordEntry(int occurrence, string ownerToken, string handle, Point2 point, double elevation)
        {
            if (occurrence < 0 || occurrence > 1) throw new ArgumentOutOfRangeException(nameof(occurrence));
            Occurrence = occurrence;
            OwnerToken = RequireCanonical(ownerToken, nameof(ownerToken));
            Handle = RequireCanonicalHandle(handle);
            if (!Finite(point.X) || !Finite(point.Y) || !Finite(elevation))
                throw new ArgumentOutOfRangeException(nameof(point), "Grid intersection marker record coordinates must be finite.");
            Point = point;
            Elevation = elevation == 0d ? 0d : elevation;
        }

        public int Occurrence { get; }
        public string OwnerToken { get; }
        public string Handle { get; }
        public Point2 Point { get; }
        public double Elevation { get; }

        private static string RequireCanonical(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Grid intersection marker identity must be nonblank and canonical.", name);
            if (value.Any(char.IsControl))
                throw new ArgumentException("Grid intersection marker identity cannot contain control characters.", name);
            return value;
        }

        private static string RequireCanonicalHandle(string value)
        {
            var handle = RequireCanonical(value, nameof(value));
            if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0L)
                throw new ArgumentException("Grid intersection marker handle must be a positive hexadecimal CAD handle.", nameof(value));
            var canonical = parsed.ToString("X", CultureInfo.InvariantCulture);
            if (!string.Equals(handle, canonical, StringComparison.Ordinal))
                throw new ArgumentException("Grid intersection marker handle must use canonical uppercase hexadecimal form.", nameof(value));
            return handle;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class GridIntersectionPairRecord
    {
        public GridIntersectionPairRecord(
            string firstElementId,
            string secondElementId,
            string pairToken,
            IEnumerable<GridIntersectionMarkerRecordEntry> entries)
        {
            FirstElementId = RequireCanonicalGridId(firstElementId, nameof(firstElementId));
            SecondElementId = RequireCanonicalGridId(secondElementId, nameof(secondElementId));
            if (string.CompareOrdinal(FirstElementId, SecondElementId) >= 0)
                throw new ArgumentException("Grid intersection pair record ids must be distinct and canonical ordinal order.");

            PairToken = RequireCanonical(pairToken, nameof(pairToken));
            var expectedPair = GridIntersectionIdentityPlanner.BuildPairToken(FirstElementId, SecondElementId);
            if (!string.Equals(PairToken, expectedPair, StringComparison.Ordinal))
                throw new ArgumentException("Grid intersection pair record token does not match its canonical Grid ids.", nameof(pairToken));

            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var materialized = entries.ToList();
            if (materialized.Count < 1 || materialized.Count > 2)
                throw new ArgumentException("Grid intersection pair record requires one or two marker entries.", nameof(entries));
            materialized.Sort((left, right) => left.Occurrence.CompareTo(right.Occurrence));
            for (var index = 0; index < materialized.Count; index++)
            {
                var entry = materialized[index] ?? throw new ArgumentException("Grid intersection pair record contains null entry.", nameof(entries));
                if (entry.Occurrence != index)
                    throw new ArgumentException("Grid intersection pair record occurrences must be contiguous from zero.", nameof(entries));
                var expectedOwner = GridIntersectionIdentityPlanner.BuildIntersectionOwner(FirstElementId, SecondElementId, index);
                if (!string.Equals(entry.OwnerToken, expectedOwner, StringComparison.Ordinal))
                    throw new ArgumentException("Grid intersection marker owner token does not match pair/occurrence.", nameof(entries));
            }
            Entries = materialized.AsReadOnly();
        }

        public string FirstElementId { get; }
        public string SecondElementId { get; }
        public string PairToken { get; }
        public IReadOnlyList<GridIntersectionMarkerRecordEntry> Entries { get; }

        private static string RequireCanonicalGridId(string value, string name)
        {
            var canonical = RequireCanonical(value, name);
            if (canonical.Length > 128) throw new ArgumentException("Grid intersection record id exceeds 128 characters.", name);
            if (!string.Equals(canonical, canonical.ToUpperInvariant(), StringComparison.Ordinal))
                throw new ArgumentException("Grid intersection record id must be canonical uppercase.", name);
            try { new UTF8Encoding(false, true).GetByteCount(canonical); }
            catch (EncoderFallbackException ex) { throw new ArgumentException("Grid intersection record id must contain well-formed Unicode text.", name, ex); }
            return canonical;
        }

        private static string RequireCanonical(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Grid intersection marker identity must be nonblank and canonical.", name);
            if (value.Any(char.IsControl))
                throw new ArgumentException("Grid intersection marker identity cannot contain control characters.", name);
            return value;
        }
    }

    public static class GridIntersectionMarkerRecordCodec
    {
        public const string MetadataPrefix = "QS3D.GridIntersectionPair.";
        private const string Version = "1";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static string MetadataKey(string pairToken)
        {
            if (string.IsNullOrWhiteSpace(pairToken) || !pairToken.StartsWith("GIP1:", StringComparison.Ordinal) || pairToken.Length != 69)
                throw new ArgumentException("Grid intersection pair token must be canonical GIP1 identity.", nameof(pairToken));
            return MetadataPrefix + pairToken;
        }

        public static bool IsMetadataKey(string key) =>
            !string.IsNullOrEmpty(key) && key.StartsWith(MetadataPrefix, StringComparison.Ordinal);

        public static string Encode(GridIntersectionPairRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            var entries = string.Join(";", record.Entries.Select(entry => string.Join(",", new[]
            {
                entry.Occurrence.ToString(CultureInfo.InvariantCulture),
                entry.OwnerToken,
                entry.Handle,
                entry.Point.X.ToString("R", CultureInfo.InvariantCulture),
                entry.Point.Y.ToString("R", CultureInfo.InvariantCulture),
                entry.Elevation.ToString("R", CultureInfo.InvariantCulture)
            })));
            return string.Join("|", new[]
            {
                Version,
                Convert.ToBase64String(StrictUtf8.GetBytes(record.FirstElementId)),
                Convert.ToBase64String(StrictUtf8.GetBytes(record.SecondElementId)),
                entries
            });
        }

        public static GridIntersectionPairRecord Decode(string metadataKey, string value)
        {
            if (!IsMetadataKey(metadataKey)) throw new ArgumentException("Grid intersection metadata key has invalid prefix.", nameof(metadataKey));
            var pairToken = metadataKey.Substring(MetadataPrefix.Length);
            if (string.IsNullOrWhiteSpace(value)) throw new FormatException("Grid intersection pair record is blank.");
            var fields = value.Split('|');
            if (fields.Length != 4 || !string.Equals(fields[0], Version, StringComparison.Ordinal))
                throw new FormatException("Grid intersection pair record has unsupported version/field count.");

            string first;
            string second;
            try
            {
                first = StrictUtf8.GetString(Convert.FromBase64String(fields[1]));
                second = StrictUtf8.GetString(Convert.FromBase64String(fields[2]));
            }
            catch (Exception ex) when (ex is FormatException || ex is DecoderFallbackException)
            {
                throw new FormatException("Grid intersection pair record contains invalid encoded Grid ids.", ex);
            }

            var entryFields = fields[3].Split(';');
            if (entryFields.Length < 1 || entryFields.Length > 2)
                throw new FormatException("Grid intersection pair record has invalid marker count.");
            var entries = new List<GridIntersectionMarkerRecordEntry>(entryFields.Length);
            foreach (var encodedEntry in entryFields)
            {
                var parts = encodedEntry.Split(',');
                if (parts.Length != 6 ||
                    !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var occurrence) ||
                    !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                    !double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
                    !double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                    throw new FormatException("Grid intersection pair record contains malformed marker fields.");
                entries.Add(new GridIntersectionMarkerRecordEntry(occurrence, parts[1], parts[2], new Point2(x, y), z));
            }

            try { return new GridIntersectionPairRecord(first, second, pairToken, entries); }
            catch (ArgumentException ex) { throw new FormatException("Grid intersection pair record violates canonical ownership.", ex); }
        }
    }
}
