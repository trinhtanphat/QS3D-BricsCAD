using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;

namespace QS3D.Core.Diagnostics
{
    public enum HealthSeverity { Info, Warning, Error }

    public sealed class ModelHealthIssue
    {
        public ModelHealthIssue(string code, HealthSeverity severity, string message, string elementId = "")
        {
            if (!Enum.IsDefined(typeof(HealthSeverity), severity))
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "Health severity must be defined.");

            Code = code ?? string.Empty;
            Severity = severity;
            Message = message ?? string.Empty;
            ElementId = elementId ?? string.Empty;
        }
        public string Code { get; }
        public HealthSeverity Severity { get; }
        public string Message { get; }
        public string ElementId { get; }
    }

    public sealed class ModelHealthService
    {
        private sealed class DiagnosticIdentityIndex
        {
            public Dictionary<string, ProjectElement> Elements { get; } = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, ProjectFamily> Families { get; } = new Dictionary<string, ProjectFamily>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, FloorDefinition> Floors { get; } = new Dictionary<string, FloorDefinition>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, ZoneDefinition> Zones { get; } = new Dictionary<string, ZoneDefinition>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> DuplicateElementIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> DuplicateFamilyIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> DuplicateFloorIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> DuplicateZoneIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveHandles = null, ISet<string>? liveGeneratedSolidHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();
            var normalizedLiveHandles = NormalizeHandleSet(liveHandles);
            var normalizedLiveGeneratedSolidHandles = NormalizeHandleSet(liveGeneratedSolidHandles);

            if (project.Metadata.TryGetValue("QS3D.ReadOnlyRecoveryRequired", out var recoveryRequired) && string.Equals(recoveryRequired, "true", StringComparison.OrdinalIgnoreCase))
            {
                var detail = project.Metadata.TryGetValue("QS3D.LoadWarning", out var warning) ? warning : "QSDB could not be loaded.";
                issues.Add(new ModelHealthIssue("PROJECT_LOAD_FAILED", HealthSeverity.Error, "Project đang ở chế độ bảo vệ và sẽ không ghi đè .qsdb: " + detail));
            }
            else if (project.Metadata.TryGetValue("QS3D.RecoveredFromBackup", out var recovered) && string.Equals(recovered, "true", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ModelHealthIssue("PROJECT_RECOVERED_BACKUP", HealthSeverity.Warning, "Project được khôi phục từ file .bak. Hãy kiểm tra dữ liệu rồi lưu lại project."));
            }

            var identity = BuildIdentityIndex(project, issues);
            var handles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var generatedHandles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            ValidateActiveZone(project, identity, issues);
            ValidateActiveFloor(project, identity, issues);

            foreach (var element in project.Elements)
            {
                if (element == null) continue;

                ValidateFamily(identity, element, issues);
                ValidateFloor(identity, element, issues);
                ValidateZone(identity, element, issues);
                if (element.Dirty != ElementDirtyFlags.None) issues.Add(new ModelHealthIssue("DIRTY", HealthSeverity.Info, "Khối lượng/cấu kiện cần cập nhật.", element.Id));

                ValidateHost(identity, element, issues);
                ValidateDependencies(identity, element, issues);
                ValidateDimensions(element, issues);
                ValidateGeneratedGeometry(project, element, normalizedLiveGeneratedSolidHandles, generatedHandles, issues);
                if (RequiresMaterial(element.Category) && !HasMaterial(identity, element)) issues.Add(new ModelHealthIssue("MISSING_MATERIAL", HealthSeverity.Warning, "Cấu kiện chưa có vật liệu.", element.Id));
                ValidateRebar(element, issues);

                var normalizedSourceHandles = element.SourceHandles
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                foreach (var normalized in normalizedSourceHandles)
                {
                    if (handles.TryGetValue(normalized, out var owner) && !string.Equals(owner, element.Id, StringComparison.OrdinalIgnoreCase)) issues.Add(new ModelHealthIssue("DUPLICATE_HANDLE", HealthSeverity.Warning, "CAD Handle đang được nhiều QS3D element sử dụng; element khác: " + owner, element.Id));
                    else handles[normalized] = element.Id;
                }

                if (normalizedLiveHandles != null && normalizedSourceHandles.Count > 0 && normalizedSourceHandles.All(x => !normalizedLiveHandles.Contains(x)))
                    issues.Add(new ModelHealthIssue("ORPHAN_HANDLE", HealthSeverity.Error, "Không còn tìm thấy đối tượng CAD nguồn.", element.Id));
            }
            return issues.AsReadOnly();
        }

        private static ISet<string>? NormalizeHandleSet(ISet<string>? handles)
        {
            if (handles == null) return null;
            return new HashSet<string>(
                handles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
        }

        private static DiagnosticIdentityIndex BuildIdentityIndex(ProjectState project, ICollection<ModelHealthIssue> issues)
        {
            var index = new DiagnosticIdentityIndex();

            foreach (var element in project.Elements)
            {
                if (element == null)
                {
                    issues.Add(new ModelHealthIssue("NULL_ELEMENT", HealthSeverity.Error, "Project chứa một semantic element null."));
                    continue;
                }
                AddIdentity(index.Elements, index.DuplicateElementIds, element.Id, element, "DUPLICATE_ID", "Trùng mã cấu kiện: ", issues);
            }

            foreach (var family in project.Families)
            {
                if (family == null)
                {
                    issues.Add(new ModelHealthIssue("NULL_FAMILY", HealthSeverity.Error, "Project chứa một Family null."));
                    continue;
                }
                AddIdentity(index.Families, index.DuplicateFamilyIds, family.Id, family, "DUPLICATE_FAMILY_ID", "Trùng mã Family: ", issues);
            }

            foreach (var floor in project.Floors)
            {
                if (floor == null)
                {
                    issues.Add(new ModelHealthIssue("NULL_FLOOR", HealthSeverity.Error, "Project chứa một Floor/Level null."));
                    continue;
                }
                AddIdentity(index.Floors, index.DuplicateFloorIds, floor.Id, floor, "DUPLICATE_FLOOR_ID", "Trùng mã Floor/Level: ", issues);
            }

            foreach (var zone in project.Zones)
            {
                if (zone == null)
                {
                    issues.Add(new ModelHealthIssue("NULL_ZONE", HealthSeverity.Error, "Project chứa một Zone null."));
                    continue;
                }
                AddIdentity(index.Zones, index.DuplicateZoneIds, zone.Id, zone, "DUPLICATE_ZONE_ID", "Trùng mã Zone: ", issues);
            }

            return index;
        }

        private static void AddIdentity<T>(Dictionary<string, T> unique, HashSet<string> duplicates, string rawId, T value, string code, string messagePrefix, ICollection<ModelHealthIssue> issues) where T : class
        {
            var id = (rawId ?? string.Empty).Trim();
            if (id.Length == 0)
            {
                issues.Add(new ModelHealthIssue("INVALID_SEMANTIC_ID", HealthSeverity.Error, "Project chứa semantic identity rỗng."));
                return;
            }
            if (!unique.ContainsKey(id))
            {
                unique[id] = value;
                return;
            }
            if (duplicates.Add(id)) issues.Add(new ModelHealthIssue(code, HealthSeverity.Error, messagePrefix + id + ".", id));
        }

        private static void ValidateActiveZone(ProjectState project, DiagnosticIdentityIndex identity, ICollection<ModelHealthIssue> issues)
        {
            var id = (project.ActiveZoneId ?? string.Empty).Trim();
            if (id.Length == 0 || !identity.Zones.ContainsKey(id))
            {
                issues.Add(new ModelHealthIssue("INVALID_ACTIVE_ZONE", HealthSeverity.Warning, "Zone làm việc hiện tại không còn hợp lệ."));
                return;
            }
            if (identity.DuplicateZoneIds.Contains(id))
                issues.Add(new ModelHealthIssue("AMBIGUOUS_ACTIVE_ZONE", HealthSeverity.Error, "Zone làm việc hiện tại trỏ tới mã Zone bị trùng: " + id + "."));
        }

        private static void ValidateActiveFloor(ProjectState project, DiagnosticIdentityIndex identity, ICollection<ModelHealthIssue> issues)
        {
            var id = (project.ActiveFloorId ?? string.Empty).Trim();
            if (id.Length == 0 || !identity.Floors.ContainsKey(id))
            {
                issues.Add(new ModelHealthIssue("INVALID_ACTIVE_FLOOR", HealthSeverity.Warning, "Tầng làm việc hiện tại không còn hợp lệ."));
                return;
            }
            if (identity.DuplicateFloorIds.Contains(id))
                issues.Add(new ModelHealthIssue("AMBIGUOUS_ACTIVE_FLOOR", HealthSeverity.Error, "Tầng làm việc hiện tại trỏ tới mã Floor/Level bị trùng: " + id + "."));
        }

        private static void ValidateFamily(DiagnosticIdentityIndex identity, ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            var familyId = (element.FamilyId ?? string.Empty).Trim();
            if (familyId.Length == 0 || !identity.Families.TryGetValue(familyId, out var family))
            {
                issues.Add(new ModelHealthIssue("MISSING_FAMILY", HealthSeverity.Error, "Cấu kiện chưa liên kết Family hợp lệ.", element.Id));
                return;
            }
            if (identity.DuplicateFamilyIds.Contains(familyId))
            {
                issues.Add(new ModelHealthIssue("AMBIGUOUS_FAMILY", HealthSeverity.Error, "FamilyId trỏ tới mã Family bị trùng: " + familyId + ".", element.Id));
                return;
            }
            if (family.Category != element.Category) issues.Add(new ModelHealthIssue("FAMILY_CATEGORY_MISMATCH", HealthSeverity.Warning, "Category của cấu kiện không khớp Family.", element.Id));
        }

        private static void ValidateFloor(DiagnosticIdentityIndex identity, ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            var floorId = (element.FloorId ?? string.Empty).Trim();
            if (floorId.Length == 0 || !identity.Floors.ContainsKey(floorId))
            {
                issues.Add(new ModelHealthIssue("MISSING_FLOOR", HealthSeverity.Warning, "Cấu kiện chưa có tầng hợp lệ.", element.Id));
                return;
            }
            if (identity.DuplicateFloorIds.Contains(floorId))
                issues.Add(new ModelHealthIssue("AMBIGUOUS_FLOOR", HealthSeverity.Error, "FloorId trỏ tới mã Floor/Level bị trùng: " + floorId + ".", element.Id));
        }

        private static void ValidateZone(DiagnosticIdentityIndex identity, ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            var zoneId = (element.ZoneId ?? string.Empty).Trim();
            if (zoneId.Length == 0 || !identity.Zones.ContainsKey(zoneId))
            {
                issues.Add(new ModelHealthIssue("MISSING_ZONE", HealthSeverity.Warning, "Cấu kiện chưa có Zone hợp lệ.", element.Id));
                return;
            }
            if (identity.DuplicateZoneIds.Contains(zoneId))
                issues.Add(new ModelHealthIssue("AMBIGUOUS_ZONE", HealthSeverity.Error, "ZoneId trỏ tới mã Zone bị trùng: " + zoneId + ".", element.Id));
        }

        private static void ValidateHost(DiagnosticIdentityIndex identity, ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (element.Category != ElementCategory.WallOpening && element.Category != ElementCategory.Door) return;
            if (!element.Properties.TryGetValue("HostWallId", out var hostId) || string.IsNullOrWhiteSpace(hostId))
            {
                issues.Add(new ModelHealthIssue("MISSING_HOST", HealthSeverity.Error, "Cửa/lỗ mở chưa có Host Wall.", element.Id));
                return;
            }

            var normalized = hostId.Trim();
            if (identity.DuplicateElementIds.Contains(normalized))
            {
                issues.Add(new ModelHealthIssue("AMBIGUOUS_HOST", HealthSeverity.Error, "Host Wall trỏ tới mã semantic element bị trùng: " + normalized + ".", element.Id));
                return;
            }
            if (!identity.Elements.TryGetValue(normalized, out var host))
            {
                issues.Add(new ModelHealthIssue("INVALID_HOST", HealthSeverity.Error, "Host Wall không tồn tại trong project.", element.Id));
                return;
            }
            if (!IsWall(host.Category)) issues.Add(new ModelHealthIssue("INVALID_HOST_CATEGORY", HealthSeverity.Error, "Host của cửa/lỗ mở không phải cấu kiện tường.", element.Id));
        }

        private static void ValidateDependencies(DiagnosticIdentityIndex identity, ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dependencyId in element.DependsOn.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
            {
                if (!seen.Add(dependencyId))
                {
                    issues.Add(new ModelHealthIssue("DUPLICATE_DEPENDENCY", HealthSeverity.Warning, "Quan hệ phụ thuộc bị lặp: " + dependencyId, element.Id));
                    continue;
                }
                if (identity.DuplicateElementIds.Contains(dependencyId))
                    issues.Add(new ModelHealthIssue("AMBIGUOUS_DEPENDENCY", HealthSeverity.Error, "Quan hệ phụ thuộc trỏ tới mã semantic element bị trùng: " + dependencyId, element.Id));
                else if (!identity.Elements.ContainsKey(dependencyId))
                    issues.Add(new ModelHealthIssue("MISSING_DEPENDENCY", HealthSeverity.Error, "Không tìm thấy cấu kiện phụ thuộc: " + dependencyId, element.Id));
            }
        }

        private static void ValidateDimensions(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            switch (element.Category)
            {
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.GlassWall:
                case ElementCategory.WallPier:
                case ElementCategory.StructuralWall:
                    RequirePositive(element, issues, "LengthM");
                    RequirePositive(element, issues, "HeightM");
                    RequirePositive(element, issues, "ThicknessM");
                    break;
                case ElementCategory.Beam:
                    RequirePositive(element, issues, "LengthM");
                    RequirePositive(element, issues, "WidthM");
                    RequirePositive(element, issues, "HeightM");
                    break;
                case ElementCategory.Slab:
                    RequirePositive(element, issues, "AreaM2");
                    RequirePositive(element, issues, "ThicknessM");
                    break;
                case ElementCategory.Column:
                    RequirePositive(element, issues, "WidthM");
                    RequirePositive(element, issues, "HeightM");
                    ValidateOptionalPositive(element, issues, "DepthM");
                    break;
                case ElementCategory.Foundation:
                    RequireAnyPositive(element, issues, "BaseAreaM2", "AreaM2", "BaseAreaM2/AreaM2");
                    RequireAnyPositive(element, issues, "ThicknessM", "HeightM", "ThicknessM/HeightM");
                    break;
                case ElementCategory.Stair:
                    RequirePositive(element, issues, "AreaM2");
                    RequirePositive(element, issues, "ThicknessM");
                    break;
                case ElementCategory.Railing:
                    RequirePositive(element, issues, "LengthM");
                    break;
                case ElementCategory.Earthwork:
                    RequireAnyPositive(element, issues, "ExcavationAreaM2", "AreaM2", "ExcavationAreaM2/AreaM2");
                    RequirePositive(element, issues, "DepthM");
                    break;
                case ElementCategory.WallOpening:
                case ElementCategory.Door:
                    RequirePositive(element, issues, "WidthM");
                    RequirePositive(element, issues, "HeightM");
                    break;
            }
        }

        private static void ValidateGeneratedGeometry(ProjectState project, ProjectElement element, ISet<string>? liveGeneratedSolidHandles, IDictionary<string, string> owners, ICollection<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue("GeneratedSolidHandle", out var rawHandle)) return;
            var handle = (rawHandle ?? string.Empty).Trim();
            if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            {
                issues.Add(new ModelHealthIssue("INVALID_GENERATED_HANDLE", HealthSeverity.Error, "GeneratedSolidHandle không hợp lệ.", element.Id));
                return;
            }

            if (owners.TryGetValue(handle, out var owner) && !string.Equals(owner, element.Id, StringComparison.OrdinalIgnoreCase))
                issues.Add(new ModelHealthIssue("DUPLICATE_GENERATED_HANDLE", HealthSeverity.Error, "Generated solid đang được nhiều element nhận sở hữu; element khác: " + owner, element.Id));
            else owners[handle] = element.Id;

            if (element.SourceHandles.Any(x => string.Equals(x?.Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                issues.Add(new ModelHealthIssue("GENERATED_HANDLE_IN_SOURCE", HealthSeverity.Error, "GeneratedSolidHandle không được nằm trong SourceHandles.", element.Id));

            if (!element.Properties.TryGetValue("GeneratedSolidCategory", out var rawCategory) || !Enum.TryParse(rawCategory, true, out ElementCategory generatedCategory))
                issues.Add(new ModelHealthIssue("GENERATED_CATEGORY_MISSING", HealthSeverity.Warning, "GeneratedSolidCategory bị thiếu hoặc không hợp lệ.", element.Id));
            else if (generatedCategory != element.Category)
                issues.Add(new ModelHealthIssue("GENERATED_CATEGORY_MISMATCH", HealthSeverity.Error, "GeneratedSolidCategory không khớp category semantic: " + generatedCategory + " ≠ " + element.Category + ".", element.Id));

            var hasVersion = element.Properties.TryGetValue("GeneratedSolidOwnershipVersion", out var ownershipVersion) && !string.IsNullOrWhiteSpace(ownershipVersion);
            var hasProjectOwner = element.Properties.TryGetValue("GeneratedSolidOwnerProjectId", out var ownerProjectId) && !string.IsNullOrWhiteSpace(ownerProjectId);
            var hasElementOwner = element.Properties.TryGetValue("GeneratedSolidOwnerElementId", out var ownerElementId) && !string.IsNullOrWhiteSpace(ownerElementId);
            if (!hasVersion || !hasProjectOwner || !hasElementOwner)
                issues.Add(new ModelHealthIssue("GENERATED_OWNERSHIP_MISSING", HealthSeverity.Warning, "Generated solid is missing a QS3D ownership marker and cannot be replaced automatically.", element.Id));
            else
            {
                if (!string.Equals(ownershipVersion!.Trim(), "1", StringComparison.Ordinal))
                    issues.Add(new ModelHealthIssue("GENERATED_OWNERSHIP_VERSION", HealthSeverity.Error, "Generated solid ownership version is not supported: " + ownershipVersion + ".", element.Id));
                if (!string.Equals(ownerProjectId!.Trim(), project.ProjectId, StringComparison.OrdinalIgnoreCase))
                    issues.Add(new ModelHealthIssue("GENERATED_PROJECT_MISMATCH", HealthSeverity.Error, "Generated solid does not belong to the current project.", element.Id));
                if (!string.Equals(ownerElementId!.Trim(), element.Id, StringComparison.OrdinalIgnoreCase))
                    issues.Add(new ModelHealthIssue("GENERATED_ELEMENT_MISMATCH", HealthSeverity.Error, "Generated solid ownership does not match the current semantic element.", element.Id));
            }

            if (liveGeneratedSolidHandles != null && !liveGeneratedSolidHandles.Contains(handle))
                issues.Add(new ModelHealthIssue("GENERATED_SOLID_MISSING", HealthSeverity.Error, "Không còn tìm thấy Solid3d đã được QS3D tạo hoặc handle hiện trỏ tới đối tượng không phải Solid3d.", element.Id));
        }

        private static void RequirePositive(ProjectElement element, ICollection<ModelHealthIssue> issues, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                issues.Add(new ModelHealthIssue("MISSING_DIMENSION", HealthSeverity.Error, "Thiếu kích thước bắt buộc " + key + ".", element.Id));
                return;
            }
            if (!TryPositiveFinite(raw, out _)) issues.Add(new ModelHealthIssue("INVALID_DIMENSION", HealthSeverity.Error, "Kích thước " + key + " phải là số hữu hạn > 0.", element.Id));
        }

        private static void ValidateOptionalPositive(ProjectElement element, ICollection<ModelHealthIssue> issues, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            if (!TryPositiveFinite(raw, out _)) issues.Add(new ModelHealthIssue("INVALID_DIMENSION", HealthSeverity.Error, "Kích thước " + key + " phải là số hữu hạn > 0 khi được khai báo.", element.Id));
        }

        private static void RequireAnyPositive(ProjectElement element, ICollection<ModelHealthIssue> issues, string first, string second, string label)
        {
            var hasFirst = element.Properties.TryGetValue(first, out var firstRaw) && !string.IsNullOrWhiteSpace(firstRaw);
            var hasSecond = element.Properties.TryGetValue(second, out var secondRaw) && !string.IsNullOrWhiteSpace(secondRaw);
            if ((hasFirst && TryPositiveFinite(firstRaw!, out _)) || (hasSecond && TryPositiveFinite(secondRaw!, out _))) return;
            if (!hasFirst && !hasSecond)
                issues.Add(new ModelHealthIssue("MISSING_DIMENSION", HealthSeverity.Error, "Thiếu kích thước bắt buộc " + label + ".", element.Id));
            else
                issues.Add(new ModelHealthIssue("INVALID_DIMENSION", HealthSeverity.Error, "Kích thước " + label + " phải có ít nhất một giá trị hữu hạn > 0.", element.Id));
        }

        private static bool TryPositiveFinite(string value, out double number)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return false;
            return IsPositiveFinite(number);
        }

        private static void ValidateRebar(ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (!element.Properties.TryGetValue("RebarNotation", out var notation) || string.IsNullOrWhiteSpace(notation)) return;
            IReadOnlyList<RebarGroup> groups;
            try { groups = RebarNotationParser.Parse(notation); }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is OverflowException)
            {
                issues.Add(new ModelHealthIssue("INVALID_REBAR", HealthSeverity.Error, "Ký hiệu thép không hợp lệ: " + ex.Message, element.Id));
                return;
            }

            var hasLength = HasPositiveNumber(element, "RebarCuttingLengthM") || HasPositiveNumber(element, "LengthM") || (element.Quantities.TryGetValue("LengthM", out var quantityLength) && IsPositiveFinite(quantityLength));
            if (!hasLength) issues.Add(new ModelHealthIssue("REBAR_LENGTH_MISSING", HealthSeverity.Warning, "Thép có notation nhưng chưa có chiều dài cắt/chiều dài cấu kiện hợp lệ.", element.Id));

            if (groups.Any(x => x.SpacingMm.HasValue) && !HasPositiveNumber(element, "RebarDistributionLengthM"))
                issues.Add(new ModelHealthIssue("REBAR_DISTRIBUTION_MISSING", HealthSeverity.Error, "Notation theo bước thép cần RebarDistributionLengthM > 0.", element.Id));
        }

        private static bool HasPositiveNumber(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value)) return false;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && IsPositiveFinite(result);
        }

        private static bool IsPositiveFinite(double value) => value > 0d && !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool HasMaterial(DiagnosticIdentityIndex identity, ProjectElement element)
        {
            if (element.Properties.TryGetValue("Material", out var own) && !string.IsNullOrWhiteSpace(own)) return true;
            var familyId = (element.FamilyId ?? string.Empty).Trim();
            if (familyId.Length == 0 || identity.DuplicateFamilyIds.Contains(familyId) || !identity.Families.TryGetValue(familyId, out var family)) return false;
            return family.Properties.TryGetValue("Material", out var familyMaterial) && !string.IsNullOrWhiteSpace(familyMaterial);
        }

        private static bool IsWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier ||
            category == ElementCategory.StructuralWall;

        private static bool RequiresMaterial(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Beam:
                case ElementCategory.Slab:
                case ElementCategory.Column:
                case ElementCategory.StructuralWall:
                case ElementCategory.Foundation:
                case ElementCategory.Stair:
                case ElementCategory.Railing:
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.GlassWall:
                case ElementCategory.WallPier:
                case ElementCategory.FloorFinish:
                case ElementCategory.Waterproofing:
                case ElementCategory.Skirting:
                case ElementCategory.WallFinish:
                case ElementCategory.CeilingFinish:
                case ElementCategory.Door:
                    return true;
                default:
                    return false;
            }
        }
    }
}
