using System;
using System.Collections.Generic;
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
    public enum InterchangeValidationSeverity
    {
        Warning = 0,
        Error = 1
    }

    public sealed class InterchangeValidationIssue
    {
        public InterchangeValidationIssue(string code, InterchangeValidationSeverity severity, string message, string path = "")
        {
            Code = code ?? string.Empty;
            Severity = severity;
            Message = message ?? string.Empty;
            Path = path ?? string.Empty;
        }

        public string Code { get; }
        public InterchangeValidationSeverity Severity { get; }
        public string Message { get; }
        public string Path { get; }
    }

    public sealed class ProjectInterchangeValidationResult
    {
        private readonly IReadOnlyList<InterchangeValidationIssue> _issues;

        internal ProjectInterchangeValidationResult(
            string format,
            int formatVersion,
            int zoneCount,
            int floorCount,
            int familyCount,
            int elementCount,
            IEnumerable<InterchangeValidationIssue> issues)
        {
            Format = format ?? string.Empty;
            FormatVersion = formatVersion;
            ZoneCount = zoneCount;
            FloorCount = floorCount;
            FamilyCount = familyCount;
            ElementCount = elementCount;
            _issues = (issues ?? Enumerable.Empty<InterchangeValidationIssue>()).ToList().AsReadOnly();
        }

        public string Format { get; }
        public int FormatVersion { get; }
        public int ZoneCount { get; }
        public int FloorCount { get; }
        public int FamilyCount { get; }
        public int ElementCount { get; }
        public IReadOnlyList<InterchangeValidationIssue> Issues => _issues;
        public int ErrorCount => _issues.Count(x => x.Severity == InterchangeValidationSeverity.Error);
        public int WarningCount => _issues.Count(x => x.Severity == InterchangeValidationSeverity.Warning);
        public bool IsValid => ErrorCount == 0;
    }

    public static class ProjectInterchangeJsonValidator
    {
        public const long MaxFileBytes = 16L * 1024L * 1024L;
        public const int MaxCollectionItems = 250000;
        public const int MaxElements = 100000;
        public const int MaxIssues = 500;

        private const int MaxIdLength = 128;
        private const int MaxNameLength = 512;
        private const int MaxPropertyKeyLength = 256;
        private const int MaxPropertyValueLength = 32768;
        private const int MaxSourceHandleLength = 128;
        private const int MaxDependenciesPerElement = 4096;
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static ProjectInterchangeValidationResult ValidateFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Interchange validation path is required.", nameof(path));
            var fullPath = Path.GetFullPath(path);
            var info = new FileInfo(fullPath);
            if (!info.Exists) throw new FileNotFoundException("Semantic snapshot file does not exist.", fullPath);
            if (info.Length > MaxFileBytes) throw new InvalidDataException("Semantic snapshot exceeds the guarded " + MaxFileBytes.ToString(CultureInfo.InvariantCulture) + " byte limit.");

            var bytes = File.ReadAllBytes(fullPath);
            try
            {
                return Validate(StrictUtf8.GetString(bytes));
            }
            catch (DecoderFallbackException ex)
            {
                var issues = new IssueCollector();
                issues.Error("JSON_UTF8", "Semantic snapshot is not valid UTF-8: " + ex.Message, "$.");
                return Result(null, issues);
            }
        }

        public static ProjectInterchangeValidationResult Validate(string json)
        {
            var issues = new IssueCollector();
            if (string.IsNullOrWhiteSpace(json))
            {
                issues.Error("JSON_EMPTY", "Semantic snapshot JSON is empty.", "$.");
                return Result(null, issues);
            }
            if (Encoding.UTF8.GetByteCount(json) > MaxFileBytes)
            {
                issues.Error("JSON_TOO_LARGE", "Semantic snapshot exceeds the guarded size limit.", "$.");
                return Result(null, issues);
            }

            SnapshotContract? snapshot;
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(SnapshotContract), new DataContractJsonSerializerSettings
                {
                    MaxItemsInObjectGraph = 1000000,
                    UseSimpleDictionaryFormat = true
                });
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json), false))
                    snapshot = serializer.ReadObject(stream) as SnapshotContract;
            }
            catch (Exception ex) when (ex is SerializationException || ex is FormatException || ex is InvalidCastException)
            {
                issues.Error("JSON_PARSE", "Semantic snapshot JSON cannot be parsed: " + ex.Message, "$.");
                return Result(null, issues);
            }

            if (snapshot == null)
            {
                issues.Error("JSON_ROOT", "Semantic snapshot JSON did not produce an object root.", "$.");
                return Result(null, issues);
            }

            ValidateHeader(snapshot, issues);
            ValidateProject(snapshot.Project, issues);
            RequireCollection(snapshot.Zones, "zones", issues);
            RequireCollection(snapshot.Floors, "floors", issues);
            RequireCollection(snapshot.Families, "families", issues);
            RequireCollection(snapshot.Elements, "elements", issues);

            var zones = snapshot.Zones ?? new List<ZoneContract>();
            var floors = snapshot.Floors ?? new List<FloorContract>();
            var families = snapshot.Families ?? new List<FamilyContract>();
            var elements = snapshot.Elements ?? new List<ElementContract>();
            ValidateCounts(zones.Count, floors.Count, families.Count, elements.Count, issues);

            var zoneIds = ValidateSimpleDefinitions(zones.Select(x => (x == null ? null : x.Id, x == null ? null : x.Name)), "zones", issues);
            var floorIds = ValidateFloors(floors, issues);
            var familyIndex = ValidateFamilies(families, issues);
            var elementIndex = ValidateElements(elements, familyIndex, floorIds, zoneIds, issues);
            ValidateDependencies(elements, elementIndex, issues);
            ValidateDependencyCycles(elementIndex, issues);

            return new ProjectInterchangeValidationResult(
                snapshot.Format ?? string.Empty,
                snapshot.FormatVersion,
                zones.Count,
                floors.Count,
                families.Count,
                elements.Count,
                issues.Items);
        }

        private static void ValidateHeader(SnapshotContract snapshot, IssueCollector issues)
        {
            if (!string.Equals(snapshot.Format, ProjectInterchangeJsonExporter.FormatName, StringComparison.Ordinal))
                issues.Error("FORMAT_NAME", "Expected format '" + ProjectInterchangeJsonExporter.FormatName + "'.", "$.format");
            if (snapshot.FormatVersion != ProjectInterchangeJsonExporter.FormatVersion)
                issues.Error("FORMAT_VERSION", "Unsupported formatVersion " + snapshot.FormatVersion.ToString(CultureInfo.InvariantCulture) + "; this validator accepts exactly version " + ProjectInterchangeJsonExporter.FormatVersion.ToString(CultureInfo.InvariantCulture) + ".", "$.formatVersion");

            var units = snapshot.Units;
            if (units == null)
            {
                issues.Error("UNITS_MISSING", "Interchange units block is required.", "$.units");
                return;
            }
            RequireUnit(units.Length, "m", "length", issues);
            RequireUnit(units.Area, "m2", "area", issues);
            RequireUnit(units.Volume, "m3", "volume", issues);
            RequireUnit(units.Mass, "kg", "mass", issues);
        }

        private static void RequireUnit(string? actual, string expected, string name, IssueCollector issues)
        {
            if (!string.Equals((actual ?? string.Empty).Trim(), expected, StringComparison.Ordinal))
                issues.Error("UNIT_" + name.ToUpperInvariant(), "Expected " + name + " unit '" + expected + "'.", "$.units." + name);
        }

        private static void ValidateProject(ProjectContract? project, IssueCollector issues)
        {
            if (project == null)
            {
                issues.Error("PROJECT_MISSING", "Project block is required.", "$.project");
                return;
            }
            ValidateId(project.Id, "$.project.id", issues);
            ValidateRequiredString(project.Name, MaxNameLength, "PROJECT_NAME_EMPTY", "PROJECT_NAME_TOO_LONG", "$.project.name", issues);
            if (project.SchemaVersion <= 0)
                issues.Error("PROJECT_SCHEMA", "Project schemaVersion must be positive.", "$.project.schemaVersion");
            ValidateOptionalString(project.DrawingFingerprint, MaxNameLength, "PROJECT_FINGERPRINT_TOO_LONG", "$.project.drawingFingerprint", issues);
            ValidateTimestamp(project.UpdatedUtc, "$.project.updatedUtc", issues);
        }

        private static void RequireCollection<T>(IReadOnlyList<T>? collection, string name, IssueCollector issues)
        {
            if (collection == null)
                issues.Error("COLLECTION_MISSING", "Semantic snapshot collection is required: " + name + ".", "$." + name);
        }

        private static void ValidateCounts(int zones, int floors, int families, int elements, IssueCollector issues)
        {
            var total = (long)zones + floors + families + elements;
            if (zones < 0 || floors < 0 || families < 0 || elements < 0 || total > MaxCollectionItems)
                issues.Error("COLLECTION_LIMIT", "Semantic snapshot collection count exceeds guarded limits.", "$.");
            if (elements > MaxElements)
                issues.Error("ELEMENT_LIMIT", "Semantic snapshot contains more than " + MaxElements.ToString(CultureInfo.InvariantCulture) + " elements.", "$.elements");
        }

        private static HashSet<string> ValidateSimpleDefinitions(IEnumerable<(string? Id, string? Name)> definitions, string path, IssueCollector issues)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var definition in definitions)
            {
                var itemPath = "$." + path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                var id = NormalizeId(definition.Id, itemPath + ".id", issues);
                if (id.Length > 0 && !ids.Add(id)) issues.Error("ID_DUPLICATE", "Duplicate id: " + id, itemPath + ".id");
                ValidateRequiredString(definition.Name, MaxNameLength, "NAME_EMPTY", "NAME_TOO_LONG", itemPath + ".name", issues);
                index++;
            }
            return ids;
        }

        private static HashSet<string> ValidateFloors(IReadOnlyList<FloorContract> floors, IssueCollector issues)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < floors.Count; i++)
            {
                var floor = floors[i];
                var path = "$.floors[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (floor == null)
                {
                    issues.Error("FLOOR_NULL", "Floor entry cannot be null.", path);
                    continue;
                }
                var id = NormalizeId(floor.Id, path + ".id", issues);
                if (id.Length > 0 && !ids.Add(id)) issues.Error("ID_DUPLICATE", "Duplicate floor id: " + id, path + ".id");
                ValidateRequiredString(floor.Name, MaxNameLength, "NAME_EMPTY", "NAME_TOO_LONG", path + ".name", issues);
                if (!Finite(floor.ElevationM)) issues.Error("FLOOR_ELEVATION", "Floor elevationM must be finite.", path + ".elevationM");
            }
            return ids;
        }

        private static Dictionary<string, FamilyContract> ValidateFamilies(IReadOnlyList<FamilyContract> families, IssueCollector issues)
        {
            var index = new Dictionary<string, FamilyContract>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < families.Count; i++)
            {
                var family = families[i];
                var path = "$.families[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (family == null)
                {
                    issues.Error("FAMILY_NULL", "Family entry cannot be null.", path);
                    continue;
                }
                var id = NormalizeId(family.Id, path + ".id", issues);
                if (id.Length > 0)
                {
                    if (index.ContainsKey(id)) issues.Error("ID_DUPLICATE", "Duplicate Family id: " + id, path + ".id");
                    else index[id] = family;
                }
                ValidateRequiredString(family.Name, MaxNameLength, "NAME_EMPTY", "NAME_TOO_LONG", path + ".name", issues);
                if (!TryCategory(family.Category, out _)) issues.Error("FAMILY_CATEGORY", "Unknown Family category: " + (family.Category ?? string.Empty), path + ".category");
                if (family.Properties == null) issues.Error("PROPERTIES_MISSING", "Family properties object is required, even when empty.", path + ".properties");
                ValidateProperties(family.Properties, path + ".properties", issues);
            }
            return index;
        }

        private static Dictionary<string, ElementContract> ValidateElements(
            IReadOnlyList<ElementContract> elements,
            IReadOnlyDictionary<string, FamilyContract> families,
            ISet<string> floors,
            ISet<string> zones,
            IssueCollector issues)
        {
            var index = new Dictionary<string, ElementContract>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                var path = "$.elements[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (element == null)
                {
                    issues.Error("ELEMENT_NULL", "Element entry cannot be null.", path);
                    continue;
                }

                var id = NormalizeId(element.Id, path + ".id", issues);
                if (id.Length > 0)
                {
                    if (index.ContainsKey(id)) issues.Error("ID_DUPLICATE", "Duplicate element id: " + id, path + ".id");
                    else index[id] = element;
                }

                var hasCategory = TryCategory(element.Category, out var category);
                if (!hasCategory) issues.Error("ELEMENT_CATEGORY", "Unknown element category: " + (element.Category ?? string.Empty), path + ".category");

                var familyId = (element.FamilyId ?? string.Empty).Trim();
                if (familyId.Length > 0)
                {
                    if (!families.TryGetValue(familyId, out var family))
                        issues.Error("FAMILY_REF_MISSING", "Element references missing Family: " + familyId, path + ".familyId");
                    else if (hasCategory && TryCategory(family.Category, out var familyCategory) && familyCategory != category)
                        issues.Error("FAMILY_CATEGORY_MISMATCH", "Element category does not match referenced Family category.", path + ".familyId");
                }

                ValidateReference(element.FloorId, floors, "FLOOR_REF_MISSING", path + ".floorId", issues);
                ValidateReference(element.ZoneId, zones, "ZONE_REF_MISSING", path + ".zoneId", issues);
                ValidateOptionalString(element.DrawingFingerprint, MaxNameLength, "ELEMENT_FINGERPRINT_TOO_LONG", path + ".drawingFingerprint", issues);
                ValidateTimestamp(element.UpdatedUtc, path + ".updatedUtc", issues);

                if (!string.Equals((element.SourceRefScope ?? string.Empty).Trim(), "drawing-local", StringComparison.Ordinal))
                    issues.Error("SOURCE_SCOPE", "sourceRefScope must be exactly 'drawing-local'; source handles are provenance only and are not import authority.", path + ".sourceRefScope");

                if (element.SourceHandles == null) issues.Error("SOURCE_HANDLES_MISSING", "sourceHandles array is required, even when empty.", path + ".sourceHandles");
                if (element.Dependencies == null) issues.Error("DEPENDENCIES_MISSING", "dependencies array is required, even when empty.", path + ".dependencies");
                if (element.Properties == null) issues.Error("PROPERTIES_MISSING", "properties object is required, even when empty.", path + ".properties");
                if (element.Quantities == null) issues.Error("QUANTITIES_MISSING", "quantities object is required, even when empty.", path + ".quantities");
                ValidateSourceHandles(element.SourceHandles, path + ".sourceHandles", issues);
                ValidateProperties(element.Properties, path + ".properties", issues);
                ValidateQuantities(element.Quantities, path + ".quantities", issues);
            }
            return index;
        }

        private static void ValidateReference(string? raw, ISet<string> known, string code, string path, IssueCollector issues)
        {
            var id = (raw ?? string.Empty).Trim();
            if (id.Length == 0) return;
            if (id.Length > MaxIdLength)
            {
                issues.Error("ID_TOO_LONG", "Reference id exceeds " + MaxIdLength.ToString(CultureInfo.InvariantCulture) + " characters.", path);
                return;
            }
            if (!known.Contains(id)) issues.Error(code, "Reference target does not exist: " + id, path);
        }

        private static void ValidateSourceHandles(IReadOnlyList<string>? handles, string path, IssueCollector issues)
        {
            if (handles == null) return;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < handles.Count; i++)
            {
                var handle = (handles[i] ?? string.Empty).Trim();
                var itemPath = path + "[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (handle.Length == 0) issues.Error("SOURCE_HANDLE_EMPTY", "Source handle cannot be empty when present.", itemPath);
                else if (handle.Length > MaxSourceHandleLength) issues.Error("SOURCE_HANDLE_TOO_LONG", "Source handle is too long.", itemPath);
                else if (!seen.Add(handle)) issues.Error("SOURCE_HANDLE_DUPLICATE", "Duplicate source handle within one element: " + handle, itemPath);
            }
        }

        private static void ValidateDependencies(IReadOnlyList<ElementContract> elements, IReadOnlyDictionary<string, ElementContract> elementIndex, IssueCollector issues)
        {
            for (var i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null) continue;
                var elementId = (element.Id ?? string.Empty).Trim();
                var dependencies = element.Dependencies ?? new List<string>();
                var path = "$.elements[" + i.ToString(CultureInfo.InvariantCulture) + "].dependencies";
                if (dependencies.Count > MaxDependenciesPerElement)
                    issues.Error("DEPENDENCY_LIMIT", "Element dependency list exceeds the guarded limit.", path);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var d = 0; d < dependencies.Count; d++)
                {
                    var dependency = (dependencies[d] ?? string.Empty).Trim();
                    var itemPath = path + "[" + d.ToString(CultureInfo.InvariantCulture) + "]";
                    if (dependency.Length == 0)
                    {
                        issues.Error("DEPENDENCY_EMPTY", "Dependency id cannot be empty.", itemPath);
                        continue;
                    }
                    if (dependency.Length > MaxIdLength)
                    {
                        issues.Error("ID_TOO_LONG", "Dependency id is too long.", itemPath);
                        continue;
                    }
                    if (!seen.Add(dependency)) issues.Error("DEPENDENCY_DUPLICATE", "Duplicate dependency id: " + dependency, itemPath);
                    if (string.Equals(dependency, elementId, StringComparison.OrdinalIgnoreCase)) issues.Error("DEPENDENCY_SELF", "Element cannot depend on itself.", itemPath);
                    else if (!elementIndex.ContainsKey(dependency)) issues.Error("DEPENDENCY_REF_MISSING", "Dependency target does not exist: " + dependency, itemPath);
                }
            }
        }

        private static void ValidateDependencyCycles(IReadOnlyDictionary<string, ElementContract> elementIndex, IssueCollector issues)
        {
            if (elementIndex.Count == 0 || issues.Full) return;

            var indegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var dependents = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in elementIndex.Keys) indegree[id] = 0;

            foreach (var pair in elementIndex)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var raw in pair.Value.Dependencies ?? new List<string>())
                {
                    var dependency = (raw ?? string.Empty).Trim();
                    if (dependency.Length == 0 || string.Equals(dependency, pair.Key, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!elementIndex.ContainsKey(dependency) || !seen.Add(dependency)) continue;
                    indegree[pair.Key] = indegree[pair.Key] + 1;
                    if (!dependents.TryGetValue(dependency, out var next))
                    {
                        next = new List<string>();
                        dependents[dependency] = next;
                    }
                    next.Add(pair.Key);
                }
            }

            var ready = new SortedSet<string>(indegree.Where(x => x.Value == 0).Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            var processed = 0;
            while (ready.Count > 0)
            {
                var id = ready.Min!;
                ready.Remove(id);
                processed++;
                if (!dependents.TryGetValue(id, out var next)) continue;
                foreach (var dependent in next.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    var remaining = indegree[dependent] - 1;
                    indegree[dependent] = remaining;
                    if (remaining == 0) ready.Add(dependent);
                }
            }

            if (processed == elementIndex.Count) return;
            var unresolved = indegree.Where(x => x.Value > 0).Select(x => x.Key).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Take(8).ToArray();
            issues.Error(
                "DEPENDENCY_CYCLE",
                "Dependency cycle detected across " + (elementIndex.Count - processed).ToString(CultureInfo.InvariantCulture) + " element(s)" +
                (unresolved.Length == 0 ? "." : ": " + string.Join(", ", unresolved) + (elementIndex.Count - processed > unresolved.Length ? ", ..." : ".")),
                "$.elements");
        }

        private static void ValidateProperties(IDictionary<string, string>? properties, string path, IssueCollector issues)
        {
            if (properties == null) return;
            foreach (var pair in properties)
            {
                var key = (pair.Key ?? string.Empty).Trim();
                if (key.Length == 0) issues.Error("PROPERTY_KEY_EMPTY", "Property key cannot be empty.", path);
                else if (key.Length > MaxPropertyKeyLength) issues.Error("PROPERTY_KEY_TOO_LONG", "Property key is too long: " + key, path);
                else if (IsGeneratedRuntimeProperty(key)) issues.Error("GENERATED_RUNTIME_PROPERTY", "Interchange snapshot must not carry generated/native ownership runtime property: " + key, path + "." + key);
                if ((pair.Value ?? string.Empty).Length > MaxPropertyValueLength)
                    issues.Error("PROPERTY_VALUE_TOO_LONG", "Property value exceeds guarded interchange length: " + key, path + "." + key);
            }
        }

        private static bool IsGeneratedRuntimeProperty(string key)
        {
            if (GeneratedHandleOwnershipPolicy.IsOwnerSlot(key)) return true;
            if (key.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return true;
            if (key.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)) return true;
            if (key.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void ValidateQuantities(IDictionary<string, double>? quantities, string path, IssueCollector issues)
        {
            if (quantities == null) return;
            foreach (var pair in quantities)
            {
                var key = (pair.Key ?? string.Empty).Trim();
                if (key.Length == 0) issues.Error("QUANTITY_KEY_EMPTY", "Quantity key cannot be empty.", path);
                else if (key.Length > MaxPropertyKeyLength) issues.Error("QUANTITY_KEY_TOO_LONG", "Quantity key is too long: " + key, path);
                if (!Finite(pair.Value)) issues.Error("QUANTITY_NONFINITE", "Quantity must be finite: " + key, path + "." + key);
            }
        }

        private static bool TryCategory(string? value, out ElementCategory category)
        {
            return Enum.TryParse((value ?? string.Empty).Trim(), false, out category) && Enum.IsDefined(typeof(ElementCategory), category);
        }

        private static string NormalizeId(string? value, string path, IssueCollector issues)
        {
            var id = (value ?? string.Empty).Trim();
            if (id.Length == 0) issues.Error("ID_EMPTY", "ID is required.", path);
            else if (id.Length > MaxIdLength) issues.Error("ID_TOO_LONG", "ID exceeds " + MaxIdLength.ToString(CultureInfo.InvariantCulture) + " characters.", path);
            return id;
        }

        private static void ValidateId(string? value, string path, IssueCollector issues)
        {
            NormalizeId(value, path, issues);
        }

        private static void ValidateRequiredString(string? value, int maxLength, string emptyCode, string tooLongCode, string path, IssueCollector issues)
        {
            var raw = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) issues.Error(emptyCode, "Value is required.", path);
            if (raw.Length > maxLength) issues.Error(tooLongCode, "Value exceeds " + maxLength.ToString(CultureInfo.InvariantCulture) + " characters.", path);
        }

        private static void ValidateOptionalString(string? value, int maxLength, string code, string path, IssueCollector issues)
        {
            if ((value ?? string.Empty).Length > maxLength) issues.Error(code, "Value exceeds " + maxLength.ToString(CultureInfo.InvariantCulture) + " characters.", path);
        }

        private static void ValidateTimestamp(string? raw, string path, IssueCollector issues)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                issues.Warning("TIMESTAMP_MISSING", "UTC timestamp is missing; review provenance before any future import.", path);
                return;
            }
            if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                issues.Error("TIMESTAMP_INVALID", "Timestamp is not an ISO-8601 round-trip value.", path);
                return;
            }
            if (parsed.Kind != DateTimeKind.Utc) issues.Warning("TIMESTAMP_NOT_UTC", "Timestamp is valid but not explicitly UTC.", path);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static ProjectInterchangeValidationResult Result(SnapshotContract? snapshot, IssueCollector issues)
        {
            return new ProjectInterchangeValidationResult(
                snapshot == null ? string.Empty : snapshot.Format ?? string.Empty,
                snapshot == null ? 0 : snapshot.FormatVersion,
                snapshot == null || snapshot.Zones == null ? 0 : snapshot.Zones.Count,
                snapshot == null || snapshot.Floors == null ? 0 : snapshot.Floors.Count,
                snapshot == null || snapshot.Families == null ? 0 : snapshot.Families.Count,
                snapshot == null || snapshot.Elements == null ? 0 : snapshot.Elements.Count,
                issues.Items);
        }

        private sealed class IssueCollector
        {
            private readonly List<InterchangeValidationIssue> _items = new List<InterchangeValidationIssue>();
            public IReadOnlyList<InterchangeValidationIssue> Items => _items.AsReadOnly();
            public bool Full => _items.Count >= MaxIssues;

            public void Error(string code, string message, string path) => Add(code, InterchangeValidationSeverity.Error, message, path);
            public void Warning(string code, string message, string path) => Add(code, InterchangeValidationSeverity.Warning, message, path);

            private void Add(string code, InterchangeValidationSeverity severity, string message, string path)
            {
                if (Full) return;
                _items.Add(new InterchangeValidationIssue(code, severity, message, path));
            }
        }

        [DataContract]
        private sealed class SnapshotContract
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

        [DataContract]
        private sealed class UnitsContract
        {
            [DataMember(Name = "length")] public string? Length { get; set; }
            [DataMember(Name = "area")] public string? Area { get; set; }
            [DataMember(Name = "volume")] public string? Volume { get; set; }
            [DataMember(Name = "mass")] public string? Mass { get; set; }
        }

        [DataContract]
        private sealed class ProjectContract
        {
            [DataMember(Name = "id")] public string? Id { get; set; }
            [DataMember(Name = "name")] public string? Name { get; set; }
            [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; }
            [DataMember(Name = "drawingFingerprint")] public string? DrawingFingerprint { get; set; }
            [DataMember(Name = "updatedUtc")] public string? UpdatedUtc { get; set; }
        }

        [DataContract]
        private sealed class ZoneContract
        {
            [DataMember(Name = "id")] public string? Id { get; set; }
            [DataMember(Name = "name")] public string? Name { get; set; }
        }

        [DataContract]
        private sealed class FloorContract
        {
            [DataMember(Name = "id")] public string? Id { get; set; }
            [DataMember(Name = "name")] public string? Name { get; set; }
            [DataMember(Name = "elevationM")] public double ElevationM { get; set; }
        }

        [DataContract]
        private sealed class FamilyContract
        {
            [DataMember(Name = "id")] public string? Id { get; set; }
            [DataMember(Name = "name")] public string? Name { get; set; }
            [DataMember(Name = "category")] public string? Category { get; set; }
            [DataMember(Name = "properties")] public Dictionary<string, string>? Properties { get; set; }
        }

        [DataContract]
        private sealed class ElementContract
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
