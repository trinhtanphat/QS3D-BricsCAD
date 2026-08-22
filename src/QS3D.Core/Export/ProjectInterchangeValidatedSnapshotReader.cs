using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.Export
{
    public sealed class InterchangeUnitsSnapshot
    {
        internal InterchangeUnitsSnapshot(string length, string area, string volume, string mass)
        {
            Length = length; Area = area; Volume = volume; Mass = mass;
        }
        public string Length { get; }
        public string Area { get; }
        public string Volume { get; }
        public string Mass { get; }
    }

    public sealed class InterchangeProjectSnapshot
    {
        internal InterchangeProjectSnapshot(string id, string name, int schemaVersion, string drawingFingerprint, string updatedUtcRaw, DateTime? updatedUtc)
        {
            Id = id; Name = name; SchemaVersion = schemaVersion; DrawingFingerprint = drawingFingerprint;
            UpdatedUtcRaw = updatedUtcRaw; UpdatedUtc = updatedUtc;
        }
        public string Id { get; }
        public string Name { get; }
        public int SchemaVersion { get; }
        public string DrawingFingerprint { get; }
        public string UpdatedUtcRaw { get; }
        public DateTime? UpdatedUtc { get; }
    }

    public sealed class InterchangeZoneSnapshot
    {
        internal InterchangeZoneSnapshot(string id, string name) { Id = id; Name = name; }
        public string Id { get; }
        public string Name { get; }
    }

    public sealed class InterchangeFloorSnapshot
    {
        internal InterchangeFloorSnapshot(string id, string name, double elevationM) { Id = id; Name = name; ElevationM = elevationM; }
        public string Id { get; }
        public string Name { get; }
        public double ElevationM { get; }
    }

    public sealed class InterchangeFamilySnapshot
    {
        internal InterchangeFamilySnapshot(string id, string name, ElementCategory category, IReadOnlyDictionary<string, string> properties)
        {
            Id = id; Name = name; Category = category; Properties = properties;
        }
        public string Id { get; }
        public string Name { get; }
        public ElementCategory Category { get; }
        public IReadOnlyDictionary<string, string> Properties { get; }
    }

    public sealed class InterchangeElementSnapshot
    {
        internal InterchangeElementSnapshot(
            string id,
            ElementCategory category,
            string familyId,
            string floorId,
            string zoneId,
            string drawingFingerprint,
            string updatedUtcRaw,
            DateTime? updatedUtc,
            string sourceRefScope,
            IReadOnlyList<string> sourceHandles,
            IReadOnlyList<string> dependencies,
            IReadOnlyDictionary<string, string> properties,
            IReadOnlyDictionary<string, double> quantities)
        {
            Id = id; Category = category; FamilyId = familyId; FloorId = floorId; ZoneId = zoneId;
            DrawingFingerprint = drawingFingerprint; UpdatedUtcRaw = updatedUtcRaw; UpdatedUtc = updatedUtc;
            SourceRefScope = sourceRefScope; SourceHandles = sourceHandles; Dependencies = dependencies;
            Properties = properties; Quantities = quantities;
        }
        public string Id { get; }
        public ElementCategory Category { get; }
        public string FamilyId { get; }
        public string FloorId { get; }
        public string ZoneId { get; }
        public string DrawingFingerprint { get; }
        public string UpdatedUtcRaw { get; }
        public DateTime? UpdatedUtc { get; }
        public string SourceRefScope { get; }
        public IReadOnlyList<string> SourceHandles { get; }
        public IReadOnlyList<string> Dependencies { get; }
        public IReadOnlyDictionary<string, string> Properties { get; }
        public IReadOnlyDictionary<string, double> Quantities { get; }
    }

    public sealed class ProjectInterchangeValidatedSnapshot
    {
        internal ProjectInterchangeValidatedSnapshot(
            ProjectInterchangeValidationResult validation,
            string format,
            int formatVersion,
            InterchangeUnitsSnapshot units,
            InterchangeProjectSnapshot project,
            IReadOnlyList<InterchangeZoneSnapshot> zones,
            IReadOnlyList<InterchangeFloorSnapshot> floors,
            IReadOnlyList<InterchangeFamilySnapshot> families,
            IReadOnlyList<InterchangeElementSnapshot> elements)
        {
            Validation = validation; Format = format; FormatVersion = formatVersion; Units = units; Project = project;
            Zones = zones; Floors = floors; Families = families; Elements = elements;
        }
        public ProjectInterchangeValidationResult Validation { get; }
        public string Format { get; }
        public int FormatVersion { get; }
        public InterchangeUnitsSnapshot Units { get; }
        public InterchangeProjectSnapshot Project { get; }
        public IReadOnlyList<InterchangeZoneSnapshot> Zones { get; }
        public IReadOnlyList<InterchangeFloorSnapshot> Floors { get; }
        public IReadOnlyList<InterchangeFamilySnapshot> Families { get; }
        public IReadOnlyList<InterchangeElementSnapshot> Elements { get; }
    }

    public static class ProjectInterchangeValidatedSnapshotReader
    {
        public static ProjectInterchangeValidatedSnapshot Read(string json)
        {
            var validation = ProjectInterchangeJsonValidator.Validate(json);
            if (!validation.IsValid)
            {
                var details = string.Join("; ", validation.Issues
                    .Where(x => x.Severity == InterchangeValidationSeverity.Error)
                    .Take(8)
                    .Select(x => x.Code + (string.IsNullOrWhiteSpace(x.Path) ? string.Empty : " @ " + x.Path)));
                throw new InvalidDataException("Semantic snapshot validation failed before reading" + (details.Length == 0 ? "." : ": " + details));
            }

            var contract = Parse(json);
            if (contract.Units == null || contract.Project == null || contract.Zones == null || contract.Floors == null || contract.Families == null || contract.Elements == null)
                throw new InvalidDataException("Validated semantic snapshot unexpectedly lost a required block during typed reading.");

            var zones = contract.Zones.Select((x, i) =>
            {
                if (x == null) throw new InvalidDataException("Validated Zone entry is null at index " + i + ".");
                return new InterchangeZoneSnapshot(Id(x.Id, "Zone"), Required(x.Name, "Zone name"));
            }).ToList().AsReadOnly();

            var floors = contract.Floors.Select((x, i) =>
            {
                if (x == null) throw new InvalidDataException("Validated Floor entry is null at index " + i + ".");
                return new InterchangeFloorSnapshot(Id(x.Id, "Floor"), Required(x.Name, "Floor name"), x.ElevationM);
            }).ToList().AsReadOnly();

            var families = contract.Families.Select((x, i) =>
            {
                if (x == null) throw new InvalidDataException("Validated Family entry is null at index " + i + ".");
                return new InterchangeFamilySnapshot(
                    Id(x.Id, "Family"),
                    Required(x.Name, "Family name"),
                    Category(x.Category, "Family"),
                    StringMap(x.Properties, "Family properties"));
            }).ToList().AsReadOnly();

            var elements = contract.Elements.Select((x, i) =>
            {
                if (x == null) throw new InvalidDataException("Validated element entry is null at index " + i + ".");
                var rawTimestamp = TimestampRaw(x.UpdatedUtc, "element updatedUtc");
                return new InterchangeElementSnapshot(
                    Id(x.Id, "element"),
                    Category(x.Category, "element"),
                    CanonicalOptional(x.FamilyId, "element familyId"),
                    CanonicalOptional(x.FloorId, "element floorId"),
                    CanonicalOptional(x.ZoneId, "element zoneId"),
                    CanonicalOptional(x.DrawingFingerprint, "element drawingFingerprint"),
                    rawTimestamp,
                    Timestamp(rawTimestamp),
                    CanonicalRequired(x.SourceRefScope, "sourceRefScope"),
                    Strings(x.SourceHandles, "sourceHandles"),
                    Strings(x.Dependencies, "dependencies"),
                    ElementStringMap(x.Properties, "element properties"),
                    NumberMap(x.Quantities, "element quantities"));
            }).ToList().AsReadOnly();

            var projectTimestamp = TimestampRaw(contract.Project.UpdatedUtc, "project updatedUtc");
            var result = new ProjectInterchangeValidatedSnapshot(
                validation,
                Required(contract.Format, "format"),
                contract.FormatVersion,
                new InterchangeUnitsSnapshot(
                    Required(contract.Units.Length, "length unit"),
                    Required(contract.Units.Area, "area unit"),
                    Required(contract.Units.Volume, "volume unit"),
                    Required(contract.Units.Mass, "mass unit")),
                new InterchangeProjectSnapshot(
                    Id(contract.Project.Id, "project"),
                    Required(contract.Project.Name, "project name"),
                    contract.Project.SchemaVersion,
                    CanonicalOptional(contract.Project.DrawingFingerprint, "project drawingFingerprint"),
                    projectTimestamp,
                    Timestamp(projectTimestamp)),
                zones,
                floors,
                families,
                elements);
            ProjectInterchangeSemanticReferenceValidator.Validate(result);
            return result;
        }

        private static SnapshotContract Parse(string json)
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(SnapshotContract), new DataContractJsonSerializerSettings
                {
                    MaxItemsInObjectGraph = 1000000,
                    UseSimpleDictionaryFormat = true
                });
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json), false))
                {
                    var value = serializer.ReadObject(stream) as SnapshotContract;
                    return value ?? throw new InvalidDataException("Validated semantic snapshot did not deserialize into a typed snapshot.");
                }
            }
            catch (Exception ex) when (ex is SerializationException || ex is FormatException || ex is InvalidCastException)
            {
                throw new InvalidDataException("Semantic snapshot passed validation but typed reading failed.", ex);
            }
        }

        private static string Id(string? value, string label) => CanonicalRequired(value, label + " id");
        private static string CanonicalOptional(string? value, string label)
        {
            var raw = value ?? string.Empty;
            if (raw.Length == 0) return string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidDataException("Validated semantic snapshot contains whitespace-only " + label + ".");
            if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("Validated semantic snapshot contains non-canonical padded " + label + ".");
            return raw;
        }
        private static string TimestampRaw(string? value, string label)
        {
            var raw = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return CanonicalOptional(raw, label);
        }
        private static string CanonicalRequired(string? value, string label)
        {
            var raw = CanonicalOptional(value, label);
            if (raw.Length == 0) throw new InvalidDataException("Validated semantic snapshot contains an empty " + label + ".");
            return raw;
        }
        private static string Required(string? value, string label)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new InvalidDataException("Validated semantic snapshot contains an empty " + label + ".");
            return normalized;
        }
        private static ElementCategory Category(string? value, string label)
        {
            var raw = CanonicalRequired(value, label + " category");
            if (!Enum.TryParse<ElementCategory>(raw, false, out var category) || !Enum.IsDefined(typeof(ElementCategory), category))
                throw new InvalidDataException("Validated semantic snapshot contains an unsupported " + label + " category.");
            return category;
        }
        private static DateTime? Timestamp(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (!HasExplicitUtcOffset(raw) || !DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                throw new InvalidDataException("Validated semantic snapshot contains a timestamp without an explicit timezone.");
            return parsed.UtcDateTime;
        }
        private static bool HasExplicitUtcOffset(string value)
        {
            if (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase)) return true;
            var timeSeparator = value.IndexOf('T');
            if (timeSeparator < 0) return false;
            var offsetSeparator = Math.Max(value.LastIndexOf('+'), value.LastIndexOf('-'));
            return offsetSeparator > timeSeparator;
        }
        private static IReadOnlyList<string> Strings(IEnumerable<string>? source, string label)
        {
            if (source == null) throw new InvalidDataException("Validated semantic snapshot is missing " + label + ".");
            return source.Select((x, i) => CanonicalRequired(x, label + "[" + i.ToString(CultureInfo.InvariantCulture) + "]")).ToList().AsReadOnly();
        }
        private static IReadOnlyDictionary<string, string> StringMap(IDictionary<string, string>? source, string label)
        {
            if (source == null) throw new InvalidDataException("Validated semantic snapshot is missing " + label + ".");
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in source.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var key = CanonicalRequired(pair.Key, label + " key");
                if (IsGeneratedOwnershipProperty(key))
                    throw new InvalidDataException("Validated semantic snapshot contains generated/native ownership property in " + label + ": " + key + ".");
                if (copy.ContainsKey(key))
                    throw new InvalidDataException("Validated semantic snapshot contains ambiguous key in " + label + ": " + key + ".");
                copy[key] = pair.Value ?? string.Empty;
            }
            return new ReadOnlyDictionary<string, string>(copy);
        }
        private static IReadOnlyDictionary<string, string> ElementStringMap(IDictionary<string, string>? source, string label)
        {
            var parsed = StringMap(source, label);
            var portable = parsed
                .Where(x => ProjectInterchangeElementPropertyPolicy.IsPortable(x.Key))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            return new ReadOnlyDictionary<string, string>(portable);
        }
        private static bool IsGeneratedOwnershipProperty(string key)
        {
            var normalized = (key ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            if (GeneratedHandleOwnershipPolicy.IsOwnerSlot(normalized)) return true;
            if (normalized.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalized.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalized.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return true;
            return normalized.StartsWith("QS3D.PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase);
        }
        private static IReadOnlyDictionary<string, double> NumberMap(IDictionary<string, double>? source, string label)
        {
            if (source == null) throw new InvalidDataException("Validated semantic snapshot is missing " + label + ".");
            var copy = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in source.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var key = CanonicalRequired(pair.Key, label + " key");
                if (copy.ContainsKey(key))
                    throw new InvalidDataException("Validated semantic snapshot contains ambiguous key in " + label + ": " + key + ".");
                copy[key] = pair.Value;
            }
            return new ReadOnlyDictionary<string, double>(copy);
        }

        [DataContract] private sealed class SnapshotContract
        {
            [DataMember(Name = "format")] public string? Format { get; set; }
            [DataMember(Name = "formatVersion")] public int FormatVersion { get; set; }
            [DataMember(Name = "units")] public UnitsContract? Units { get; set; }
            [DataMember(Name = "project")] public ProjectContract? Project { get; set; }
            [DataMember(Name = "zones")] public List<ZoneContract>? Zones { get; set; }
            [DataMember(Name = "floors")] public List<FloorContract>? Floors { get; set; }
            [DataMember(Name = "families")] public List<FamilyContract>? Families { get; set; }
            [DataMember(Name = "elements")] public List<ElementContract>? Elements { get; set; }
        }
        [DataContract] private sealed class UnitsContract
        {
            [DataMember(Name = "length")] public string? Length { get; set; }
            [DataMember(Name = "area")] public string? Area { get; set; }
            [DataMember(Name = "volume")] public string? Volume { get; set; }
            [DataMember(Name = "mass")] public string? Mass { get; set; }
        }
        [DataContract] private sealed class ProjectContract
        {
            [DataMember(Name = "id")] public string? Id { get; set; }
            [DataMember(Name = "name")] public string? Name { get; set; }
            [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; }
            [DataMember(Name = "drawingFingerprint")] public string? DrawingFingerprint { get; set; }
            [DataMember(Name = "updatedUtc")] public string? UpdatedUtc { get; set; }
        }
        [DataContract] private sealed class ZoneContract
        {
            [DataMember(Name = "id")] public string? Id { get; set; }
            [DataMember(Name = "name")] public string? Name { get; set; }
        }
        [DataContract] private sealed class FloorContract
        {
            [DataMember(Name = "id")] public string? Id { get; set; }
            [DataMember(Name = "name")] public string? Name { get; set; }
            [DataMember(Name = "elevationM")] public double ElevationM { get; set; }
        }
        [DataContract] private sealed class FamilyContract
        {
            [DataMember(Name = "id")] public string? Id { get; set; }
            [DataMember(Name = "name")] public string? Name { get; set; }
            [DataMember(Name = "category")] public string? Category { get; set; }
            [DataMember(Name = "properties")] public Dictionary<string, string>? Properties { get; set; }
        }
        [DataContract] private sealed class ElementContract
        {
            [DataMember(Name = "id")] public string? Id { get; set; }
            [DataMember(Name = "category")] public string? Category { get; set; }
            [DataMember(Name = "familyId")] public string? FamilyId { get; set; }
            [DataMember(Name = "floorId")] public string? FloorId { get; set; }
            [DataMember(Name = "zoneId")] public string? ZoneId { get; set; }
            [DataMember(Name = "drawingFingerprint")] public string? DrawingFingerprint { get; set; }
            [DataMember(Name = "updatedUtc")] public string? UpdatedUtc { get; set; }
            [DataMember(Name = "sourceRefScope")] public string? SourceRefScope { get; set; }
            [DataMember(Name = "sourceHandles")] public List<string>? SourceHandles { get; set; }
            [DataMember(Name = "dependencies")] public List<string>? Dependencies { get; set; }
            [DataMember(Name = "properties")] public Dictionary<string, string>? Properties { get; set; }
            [DataMember(Name = "quantities")] public Dictionary<string, double>? Quantities { get; set; }
        }
    }
}