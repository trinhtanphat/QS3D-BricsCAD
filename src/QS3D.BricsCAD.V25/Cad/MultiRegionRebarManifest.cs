using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class SourceManifestEntry
    {
        public SourceManifestEntry(string regionId, string outerSourceHandle, IReadOnlyList<string> holeSourceHandles)
        {
            RegionId = regionId ?? throw new ArgumentNullException(nameof(regionId));
            OuterSourceHandle = outerSourceHandle ?? throw new ArgumentNullException(nameof(outerSourceHandle));
            HoleSourceHandles = holeSourceHandles ?? throw new ArgumentNullException(nameof(holeSourceHandles));
        }

        public string RegionId { get; }
        public string OuterSourceHandle { get; }
        public IReadOnlyList<string> HoleSourceHandles { get; }
    }

    internal sealed class GeneratedManifestEntry
    {
        public GeneratedManifestEntry(string regionId, IReadOnlyList<string> handles)
        {
            RegionId = regionId ?? throw new ArgumentNullException(nameof(regionId));
            Handles = handles ?? throw new ArgumentNullException(nameof(handles));
        }

        public string RegionId { get; }
        public IReadOnlyList<string> Handles { get; }
    }

    internal static class MultiRegionRebarManifest
    {
        private const string Version = "1";
        private const int MaxIdentifierLength = 160;
        private const int MaxSerializedLength = 1024 * 1024;
        public const int MaxRegions = 1024;
        public const int MaxHandlesPerRegion = 12000;
        public const int MaxTotalGeneratedHandles = 12000;

        public static string SerializeSources(IEnumerable<SourceManifestEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var normalized = NormalizeSources(entries);
            var rows = new List<string>(normalized.Count);
            foreach (var entry in normalized)
            {
                var fields = new List<string>(3 + entry.HoleSourceHandles.Count)
                {
                    entry.RegionId,
                    entry.OuterSourceHandle,
                    entry.HoleSourceHandles.Count.ToString(CultureInfo.InvariantCulture)
                };
                fields.AddRange(entry.HoleSourceHandles);
                rows.Add(string.Join(",", fields));
            }
            return RequireSerializedBound(Version + "|" + string.Join(";", rows));
        }

        public static IReadOnlyList<SourceManifestEntry> ParseSources(string serialized)
        {
            var payload = ParsePayload(serialized, "source");
            if (payload.Length == 0) return new List<SourceManifestEntry>().AsReadOnly();

            var rows = payload.Split(new[] { ';' }, StringSplitOptions.None);
            if (rows.Length > MaxRegions) throw new FormatException("Multi-region source manifest exceeds the supported region limit.");
            var parsed = new List<SourceManifestEntry>(rows.Length);
            foreach (var row in rows)
            {
                var fields = row.Split(new[] { ',' }, StringSplitOptions.None);
                if (fields.Length < 3) throw new FormatException("Multi-region source manifest row is malformed.");
                var holeCount = ParseCount(fields[2], MaxHandlesPerRegion, "source hole");
                if (fields.Length != 3 + holeCount) throw new FormatException("Multi-region source manifest hole count does not match its payload.");
                var holes = new List<string>(holeCount);
                for (var index = 0; index < holeCount; index++) holes.Add(NormalizeIdentifier(fields[3 + index], "source hole handle"));
                parsed.Add(new SourceManifestEntry(
                    NormalizeIdentifier(fields[0], "region id"),
                    NormalizeIdentifier(fields[1], "outer source handle"),
                    holes.AsReadOnly()));
            }
            return NormalizeSources(parsed);
        }

        public static string SerializeGenerated(IEnumerable<GeneratedManifestEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var normalized = NormalizeGenerated(entries);
            var rows = new List<string>(normalized.Count);
            foreach (var entry in normalized)
            {
                var fields = new List<string>(2 + entry.Handles.Count)
                {
                    entry.RegionId,
                    entry.Handles.Count.ToString(CultureInfo.InvariantCulture)
                };
                fields.AddRange(entry.Handles);
                rows.Add(string.Join(",", fields));
            }
            return RequireSerializedBound(Version + "|" + string.Join(";", rows));
        }

        public static IReadOnlyList<GeneratedManifestEntry> ParseGenerated(string serialized)
        {
            var payload = ParsePayload(serialized, "generated");
            if (payload.Length == 0) return new List<GeneratedManifestEntry>().AsReadOnly();

            var rows = payload.Split(new[] { ';' }, StringSplitOptions.None);
            if (rows.Length > MaxRegions) throw new FormatException("Multi-region generated manifest exceeds the supported region limit.");
            var parsed = new List<GeneratedManifestEntry>(rows.Length);
            foreach (var row in rows)
            {
                var fields = row.Split(new[] { ',' }, StringSplitOptions.None);
                if (fields.Length < 2) throw new FormatException("Multi-region generated manifest row is malformed.");
                var handleCount = ParseCount(fields[1], MaxHandlesPerRegion, "generated handle");
                if (fields.Length != 2 + handleCount) throw new FormatException("Multi-region generated manifest handle count does not match its payload.");
                var handles = new List<string>(handleCount);
                for (var index = 0; index < handleCount; index++) handles.Add(NormalizeIdentifier(fields[2 + index], "generated handle"));
                parsed.Add(new GeneratedManifestEntry(
                    NormalizeIdentifier(fields[0], "region id"),
                    handles.AsReadOnly()));
            }
            return NormalizeGenerated(parsed);
        }

        private static IReadOnlyList<SourceManifestEntry> NormalizeSources(IEnumerable<SourceManifestEntry> entries)
        {
            var materialized = entries.Take(MaxRegions + 1).ToList();
            if (materialized.Count > MaxRegions) throw new ArgumentException("Multi-region source manifest exceeds the supported " + MaxRegions + " region limit.", nameof(entries));

            var regionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sourceHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var normalized = new List<SourceManifestEntry>(materialized.Count);
            for (var index = 0; index < materialized.Count; index++)
            {
                var entry = materialized[index] ?? throw new ArgumentException("Multi-region source manifest entry cannot be null.", nameof(entries));
                var regionId = NormalizeIdentifier(entry.RegionId, "region id");
                var outer = NormalizeIdentifier(entry.OuterSourceHandle, "outer source handle");
                if (!regionIds.Add(regionId)) throw new ArgumentException("Multi-region source manifest contains duplicate region id " + regionId + ".", nameof(entries));
                if (!sourceHandles.Add(outer)) throw new ArgumentException("Multi-region source manifest reuses source handle " + outer + ".", nameof(entries));

                var rawHoles = entry.HoleSourceHandles ?? throw new ArgumentException("Multi-region source manifest hole collection cannot be null.", nameof(entries));
                if (rawHoles.Count > MaxHandlesPerRegion) throw new ArgumentException("Multi-region source manifest exceeds the per-region source handle limit.", nameof(entries));
                var holes = new List<string>(rawHoles.Count);
                foreach (var rawHole in rawHoles)
                {
                    var hole = NormalizeIdentifier(rawHole, "source hole handle");
                    if (!sourceHandles.Add(hole)) throw new ArgumentException("Multi-region source manifest reuses source handle " + hole + ".", nameof(entries));
                    holes.Add(hole);
                }
                holes.Sort(StringComparer.Ordinal);
                normalized.Add(new SourceManifestEntry(regionId, outer, holes.AsReadOnly()));
            }
            normalized.Sort((left, right) => StringComparer.Ordinal.Compare(left.RegionId, right.RegionId));
            return normalized.AsReadOnly();
        }

        private static IReadOnlyList<GeneratedManifestEntry> NormalizeGenerated(IEnumerable<GeneratedManifestEntry> entries)
        {
            var materialized = entries.Take(MaxRegions + 1).ToList();
            if (materialized.Count > MaxRegions) throw new ArgumentException("Multi-region generated manifest exceeds the supported " + MaxRegions + " region limit.", nameof(entries));

            var regionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var generatedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var normalized = new List<GeneratedManifestEntry>(materialized.Count);
            var totalHandles = 0;
            for (var index = 0; index < materialized.Count; index++)
            {
                var entry = materialized[index] ?? throw new ArgumentException("Multi-region generated manifest entry cannot be null.", nameof(entries));
                var regionId = NormalizeIdentifier(entry.RegionId, "region id");
                if (!regionIds.Add(regionId)) throw new ArgumentException("Multi-region generated manifest contains duplicate region id " + regionId + ".", nameof(entries));
                var rawHandles = entry.Handles ?? throw new ArgumentException("Multi-region generated manifest handle collection cannot be null.", nameof(entries));
                if (rawHandles.Count > MaxHandlesPerRegion) throw new ArgumentException("Multi-region generated manifest exceeds the per-region generated handle limit.", nameof(entries));

                var handles = new List<string>(rawHandles.Count);
                foreach (var rawHandle in rawHandles)
                {
                    var handle = NormalizeIdentifier(rawHandle, "generated handle");
                    if (!generatedHandles.Add(handle)) throw new ArgumentException("Multi-region generated manifest reuses generated handle " + handle + ".", nameof(entries));
                    handles.Add(handle);
                }
                totalHandles += handles.Count;
                if (totalHandles > MaxTotalGeneratedHandles) throw new ArgumentException("Multi-region generated manifest exceeds the native " + MaxTotalGeneratedHandles + " bar limit.", nameof(entries));
                handles.Sort(StringComparer.Ordinal);
                normalized.Add(new GeneratedManifestEntry(regionId, handles.AsReadOnly()));
            }
            normalized.Sort((left, right) => StringComparer.Ordinal.Compare(left.RegionId, right.RegionId));
            return normalized.AsReadOnly();
        }

        private static string ParsePayload(string serialized, string kind)
        {
            if (serialized == null) throw new ArgumentNullException(nameof(serialized));
            RequireSerializedBound(serialized);
            var prefix = Version + "|";
            if (!serialized.StartsWith(prefix, StringComparison.Ordinal))
                throw new FormatException("Unsupported multi-region " + kind + " manifest version.");
            return serialized.Substring(prefix.Length);
        }

        private static int ParseCount(string value, int maximum, string label)
        {
            int count;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out count) || count < 0 || count > maximum)
                throw new FormatException("Multi-region manifest " + label + " count is invalid.");
            return count;
        }

        private static string NormalizeIdentifier(string value, string label)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Multi-region manifest " + label + " is required.");
            if (normalized.Length > MaxIdentifierLength) throw new ArgumentException("Multi-region manifest " + label + " exceeds the supported identifier length.");
            for (var index = 0; index < normalized.Length; index++)
            {
                var character = normalized[index];
                if (char.IsControl(character) || character == ',' || character == ';' || character == '|')
                    throw new ArgumentException("Multi-region manifest " + label + " contains an unsupported character.");
            }
            return normalized.ToUpperInvariant();
        }

        private static string RequireSerializedBound(string serialized)
        {
            if (serialized.Length > MaxSerializedLength)
                throw new ArgumentException("Multi-region manifest exceeds the supported serialized-size limit.");
            return serialized;
        }
    }
}
