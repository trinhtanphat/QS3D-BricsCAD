using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedFoundationMeshHealthService
    {
        private const string HandlesKey = "GeneratedFoundationMeshHandles";
        private const string CountKey = "GeneratedFoundationMeshCount";

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var ownership = BuildOwnershipIndex(project);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Foundation mesh health cannot inspect a null project element.");
                if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var validCount = 0;
                foreach (var item in raw.Split(new[] { ';' }, StringSplitOptions.None))
                {
                    var handleText = item ?? string.Empty;
                    var handle = handleText.Trim();
                    if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                    {
                        issues.Add(new ModelHealthIssue("INVALID_FOUNDATION_MESH_GENERATED_HANDLE", HealthSeverity.Error, HandlesKey + " chứa handle không hợp lệ.", element.Id));
                        continue;
                    }
                    if (!string.Equals(handleText, handle, StringComparison.Ordinal))
                        issues.Add(new ModelHealthIssue("FOUNDATION_MESH_GENERATED_HANDLE_NON_CANONICAL", HealthSeverity.Error, HandlesKey + " không được có khoảng trắng đầu/cuối ở từng handle.", element.Id));
                    var identity = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);
                    if (!local.Add(identity))
                    {
                        issues.Add(new ModelHealthIssue("DUPLICATE_FOUNDATION_MESH_GENERATED_HANDLE", HealthSeverity.Error, "Một foundation mesh handle bị lặp trong cùng element: " + handle, element.Id));
                        continue;
                    }
                    validCount++;
                    var expectedOwner = element.Id + "/" + HandlesKey;
                    if (ownership.IsConflicted(identity, expectedOwner))
                        issues.Add(new ModelHealthIssue("FOUNDATION_MESH_GENERATED_OWNERSHIP_CONFLICT", HealthSeverity.Error, "Generated foundation mesh solid xung đột owner/project handle khác: " + ownership.Describe(identity), element.Id));
                    if (element.SourceHandles.Any(x => string.Equals(GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(x), identity, StringComparison.OrdinalIgnoreCase)))
                        issues.Add(new ModelHealthIssue("FOUNDATION_MESH_GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "Generated foundation mesh handle không được nằm trong SourceHandles.", element.Id));
                    if (liveSolidHandles != null && !ContainsLogicalHandle(liveSolidHandles, identity))
                        issues.Add(new ModelHealthIssue("FOUNDATION_MESH_GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy generated foundation mesh Solid3d: " + handle, element.Id));
                }

                if (!element.Properties.TryGetValue(CountKey, out var countText) ||
                    !int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0 || count != validCount)
                {
                    issues.Add(new ModelHealthIssue("FOUNDATION_MESH_GENERATED_COUNT_MISMATCH", HealthSeverity.Warning, CountKey + " không khớp số handle hợp lệ.", element.Id));
                }
                else if (!string.Equals(countText, count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                {
                    issues.Add(new ModelHealthIssue("FOUNDATION_MESH_GENERATED_COUNT_NON_CANONICAL", HealthSeverity.Warning, CountKey + " phải dùng canonical invariant integer text.", element.Id));
                }

                ValidatePositive(element, "GeneratedFoundationMeshXDiameterMm", "FOUNDATION_MESH_X_DIAMETER_INVALID", issues);
                ValidatePositive(element, "GeneratedFoundationMeshYDiameterMm", "FOUNDATION_MESH_Y_DIAMETER_INVALID", issues);
                ValidatePositive(element, "GeneratedFoundationMeshXActualSpacingM", "FOUNDATION_MESH_X_SPACING_INVALID", issues);
                ValidatePositive(element, "GeneratedFoundationMeshYActualSpacingM", "FOUNDATION_MESH_Y_SPACING_INVALID", issues);
                ValidateNonNegative(element, "GeneratedFoundationMeshCoverM", "FOUNDATION_MESH_COVER_INVALID", issues);

                if (!element.Properties.TryGetValue("GeneratedFoundationMeshFaces", out var faces) ||
                    !(string.Equals(faces, "Bottom", StringComparison.Ordinal) ||
                      string.Equals(faces, "Top", StringComparison.Ordinal) ||
                      string.Equals(faces, "Both", StringComparison.Ordinal)))
                    issues.Add(new ModelHealthIssue("FOUNDATION_MESH_FACES_INVALID", HealthSeverity.Warning, "GeneratedFoundationMeshFaces phải là Bottom, Top hoặc Both.", element.Id));

                if (!element.Properties.TryGetValue("GeneratedFoundationMeshMode", out var mode) || !string.Equals(mode, "FoundationMeshXY", StringComparison.Ordinal))
                    issues.Add(new ModelHealthIssue("FOUNDATION_MESH_MODE_INVALID", HealthSeverity.Warning, "GeneratedFoundationMeshMode thiếu hoặc không hợp lệ.", element.Id));

                if (element.Properties.TryGetValue("GeneratedFoundationMeshFootprintMode", out var footprintMode) &&
                    !(string.Equals(footprintMode, "RectangleLocalXY", StringComparison.Ordinal) ||
                      string.Equals(footprintMode, "PolygonGlobalXY", StringComparison.Ordinal)))
                    issues.Add(new ModelHealthIssue("FOUNDATION_MESH_FOOTPRINT_MODE_INVALID", HealthSeverity.Warning, "GeneratedFoundationMeshFootprintMode phải là RectangleLocalXY hoặc PolygonGlobalXY; missing key is accepted only as legacy rectangle metadata.", element.Id));

                if (element.Category != ElementCategory.Foundation)
                    issues.Add(new ModelHealthIssue("FOUNDATION_MESH_CATEGORY_MISMATCH", HealthSeverity.Error, "Generated foundation mesh metadata chỉ hợp lệ trên Foundation element.", element.Id));

                if (element.IsGeneratedFoundationMeshStale())
                    issues.Add(new ModelHealthIssue("FOUNDATION_MESH_GENERATED_STALE", HealthSeverity.Warning, "Generated foundation mesh snapshot không còn khớp semantic/source hiện tại; rebuild lưới thép móng 3D trước khi phát hành bản vẽ.", element.Id));
            }
            return issues.AsReadOnly();
        }

        private static bool ContainsLogicalHandle(IEnumerable<string> handles, string identity) =>
            handles.Any(x => string.Equals(GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(x), identity, StringComparison.OrdinalIgnoreCase));

        private static void ValidatePositive(ProjectElement element, string key, string code, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(key, out var text) ||
                !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value <= 0d ||
                !string.Equals(text, value.ToString("R", CultureInfo.InvariantCulture), StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, key + " thiếu hoặc không hợp lệ.", element.Id));
        }

        private static void ValidateNonNegative(ProjectElement element, string key, string code, List<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue(key, out var text) ||
                !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value < 0d ||
                !string.Equals(text, value.ToString("R", CultureInfo.InvariantCulture), StringComparison.Ordinal))
                issues.Add(new ModelHealthIssue(code, HealthSeverity.Warning, key + " thiếu hoặc không hợp lệ.", element.Id));
        }

        private sealed class OwnershipIndex
        {
            public Dictionary<string, string> Owners { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Conflicts { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public bool IsConflicted(string handle, string expectedOwner)
            {
                if (Conflicts.Contains(handle)) return true;
                return Owners.TryGetValue(handle, out var owner) && !string.Equals(owner, expectedOwner, StringComparison.OrdinalIgnoreCase);
            }

            public string Describe(string handle)
            {
                if (Conflicts.Contains(handle)) return "multiple owners";
                return Owners.TryGetValue(handle, out var owner) ? owner : "unknown owner";
            }
        }

        private static OwnershipIndex BuildOwnershipIndex(ProjectState project)
        {
            var index = new OwnershipIndex();
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Foundation mesh health cannot inspect a null project element.");
                foreach (var handle in element.SourceHandles)
                    Reserve(index, handle, element.Id + "/SourceHandles");

                foreach (var property in element.Properties)
                {
                    if (!GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)) continue;
                    ReserveProperty(index, element, property.Key, property.Value);
                }
            }
            return index;
        }

        private static void ReserveProperty(OwnershipIndex index, ProjectElement element, string key, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
                Reserve(index, handle, element.Id + "/" + key);
        }

        private static void Reserve(OwnershipIndex index, string? handle, string token)
        {
            var normalized = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);
            if (normalized.Length == 0) return;
            if (!index.Owners.TryGetValue(normalized, out var existing))
            {
                index.Owners[normalized] = token;
                return;
            }
            if (!string.Equals(existing, token, StringComparison.OrdinalIgnoreCase))
                index.Conflicts.Add(normalized);
        }
    }
}
