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
            Code = code ?? string.Empty; Severity = severity; Message = message ?? string.Empty; ElementId = elementId ?? string.Empty;
        }
        public string Code { get; }
        public HealthSeverity Severity { get; }
        public string Message { get; }
        public string ElementId { get; }
    }

    public sealed class ModelHealthService
    {
        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project)); var issues = new List<ModelHealthIssue>();
            if (project.Metadata.TryGetValue("QS3D.ReadOnlyRecoveryRequired", out var recoveryRequired) && string.Equals(recoveryRequired, "true", StringComparison.OrdinalIgnoreCase))
            {
                var detail = project.Metadata.TryGetValue("QS3D.LoadWarning", out var warning) ? warning : "QSDB could not be loaded."; issues.Add(new ModelHealthIssue("PROJECT_LOAD_FAILED", HealthSeverity.Error, "Project đang ở chế độ bảo vệ và sẽ không ghi đè .qsdb: " + detail));
            }
            else if (project.Metadata.TryGetValue("QS3D.RecoveredFromBackup", out var recovered) && string.Equals(recovered, "true", StringComparison.OrdinalIgnoreCase)) issues.Add(new ModelHealthIssue("PROJECT_RECOVERED_BACKUP", HealthSeverity.Warning, "Project được khôi phục từ file .bak. Hãy kiểm tra dữ liệu rồi lưu lại project."));

<<<<<<< origin/main
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var handles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var floorIds = new HashSet<string>(project.Floors.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var zoneIds = new HashSet<string>(project.Zones.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(project.ActiveZoneId) || !zoneIds.Contains(project.ActiveZoneId))
                issues.Add(new ModelHealthIssue("INVALID_ACTIVE_ZONE", HealthSeverity.Warning, "Zone làm việc hiện tại không còn hợp lệ."));
            if (string.IsNullOrWhiteSpace(project.ActiveFloorId) || !floorIds.Contains(project.ActiveFloorId))
                issues.Add(new ModelHealthIssue("INVALID_ACTIVE_FLOOR", HealthSeverity.Warning, "Tầng làm việc hiện tại không còn hợp lệ."));

=======
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var handles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); var familyIds = new HashSet<string>(project.Families.Select(x => x.Id), StringComparer.OrdinalIgnoreCase); var floorIds = new HashSet<string>(project.Floors.Select(x => x.Id), StringComparer.OrdinalIgnoreCase); var zoneIds = new HashSet<string>(project.Zones.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
>>>>>>> origin/agent/full-domain-20260810
            foreach (var element in project.Elements)
            {
                if (!ids.Add(element.Id)) issues.Add(new ModelHealthIssue("DUPLICATE_ID", HealthSeverity.Error, "Trùng mã cấu kiện.", element.Id));

                var family = string.IsNullOrWhiteSpace(element.FamilyId) ? null : project.FindFamily(element.FamilyId);
                if (family == null) issues.Add(new ModelHealthIssue("MISSING_FAMILY", HealthSeverity.Error, "Cấu kiện chưa liên kết Family hợp lệ.", element.Id));
                else if (family.Category != element.Category) issues.Add(new ModelHealthIssue("FAMILY_CATEGORY_MISMATCH", HealthSeverity.Warning, "Category của cấu kiện không khớp Family.", element.Id));

                if (string.IsNullOrWhiteSpace(element.FloorId) || !floorIds.Contains(element.FloorId)) issues.Add(new ModelHealthIssue("MISSING_FLOOR", HealthSeverity.Warning, "Cấu kiện chưa có tầng hợp lệ.", element.Id));
                if (string.IsNullOrWhiteSpace(element.ZoneId) || !zoneIds.Contains(element.ZoneId)) issues.Add(new ModelHealthIssue("MISSING_ZONE", HealthSeverity.Warning, "Cấu kiện chưa có Zone hợp lệ.", element.Id));
                if (element.Dirty != ElementDirtyFlags.None) issues.Add(new ModelHealthIssue("DIRTY", HealthSeverity.Info, "Khối lượng/cấu kiện cần cập nhật.", element.Id));

                ValidateHost(project, element, issues);
                ValidateDependencies(project, element, issues);
                ValidateDimensions(element, issues);
                if (RequiresMaterial(element.Category) && !HasMaterial(project, element)) issues.Add(new ModelHealthIssue("MISSING_MATERIAL", HealthSeverity.Warning, "Cấu kiện chưa có vật liệu.", element.Id));
<<<<<<< origin/main
                ValidateRebar(element, issues);

=======
                ValidateDomain(project, element, issues);
>>>>>>> origin/agent/full-domain-20260810
                foreach (var handle in element.SourceHandles.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var normalized = handle.Trim(); if (handles.TryGetValue(normalized, out var owner) && !string.Equals(owner, element.Id, StringComparison.OrdinalIgnoreCase)) issues.Add(new ModelHealthIssue("DUPLICATE_HANDLE", HealthSeverity.Warning, "CAD Handle đang được nhiều QS3D element sử dụng; element khác: " + owner, element.Id)); else handles[normalized] = element.Id;
                }
                if (liveHandles != null && element.SourceHandles.Count > 0 && element.SourceHandles.All(x => !liveHandles.Contains(x))) issues.Add(new ModelHealthIssue("ORPHAN_HANDLE", HealthSeverity.Error, "Không còn tìm thấy đối tượng CAD nguồn.", element.Id));
            }
            return issues;
        }

<<<<<<< origin/main
        private static void ValidateHost(ProjectState project, ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            if (element.Category != ElementCategory.WallOpening && element.Category != ElementCategory.Door) return;
            if (!element.Properties.TryGetValue("HostWallId", out var hostId) || string.IsNullOrWhiteSpace(hostId))
            {
                issues.Add(new ModelHealthIssue("MISSING_HOST", HealthSeverity.Error, "Cửa/lỗ mở chưa có Host Wall.", element.Id));
                return;
            }

            var host = project.FindElement(hostId.Trim());
            if (host == null)
            {
                issues.Add(new ModelHealthIssue("INVALID_HOST", HealthSeverity.Error, "Host Wall không tồn tại trong project.", element.Id));
                return;
            }
            if (!IsWall(host.Category)) issues.Add(new ModelHealthIssue("INVALID_HOST_CATEGORY", HealthSeverity.Error, "Host của cửa/lỗ mở không phải cấu kiện tường.", element.Id));
        }

        private static void ValidateDependencies(ProjectState project, ProjectElement element, ICollection<ModelHealthIssue> issues)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dependencyId in element.DependsOn.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
            {
                if (!seen.Add(dependencyId))
                {
                    issues.Add(new ModelHealthIssue("DUPLICATE_DEPENDENCY", HealthSeverity.Warning, "Quan hệ phụ thuộc bị lặp: " + dependencyId, element.Id));
                    continue;
                }
                if (project.FindElement(dependencyId) == null) issues.Add(new ModelHealthIssue("MISSING_DEPENDENCY", HealthSeverity.Error, "Không tìm thấy cấu kiện phụ thuộc: " + dependencyId, element.Id));
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

        private static bool HasMaterial(ProjectState project, ProjectElement element)
=======
        private static void ValidateDomain(ProjectState project, ProjectElement element, IList<ModelHealthIssue> issues)
>>>>>>> origin/agent/full-domain-20260810
        {
            switch (element.Category)
            {
                case ElementCategory.Beam:
                    RequirePositive(project, element, issues, "LengthM", "Dầm thiếu chiều dài."); RequirePositive(project, element, issues, "WidthM", "Dầm thiếu bề rộng."); RequirePositive(project, element, issues, "HeightM", "Dầm thiếu chiều cao."); break;
                case ElementCategory.Slab:
                    RequirePositive(project, element, issues, "AreaM2", "Sàn thiếu diện tích biên dạng."); RequirePositive(project, element, issues, "ThicknessM", "Sàn thiếu chiều dày."); break;
                case ElementCategory.Column:
                    RequirePositive(project, element, issues, "HeightM", "Cột thiếu chiều cao.");
                    if (!((Number(project, element, "AreaM2") > 0d && Number(project, element, "PerimeterM") > 0d) || (Number(project, element, "WidthM") > 0d && Number(project, element, "DepthM") > 0d))) issues.Add(new ModelHealthIssue("MISSING_DIMENSION", HealthSeverity.Error, "Cột cần Area+Perimeter hoặc Width+Depth hợp lệ.", element.Id));
                    break;
                case ElementCategory.StructuralWall:
                    RequirePositive(project, element, issues, "LengthM", "Vách kết cấu thiếu chiều dài."); RequirePositive(project, element, issues, "ThicknessM", "Vách kết cấu thiếu chiều dày."); RequirePositive(project, element, issues, "HeightM", "Vách kết cấu thiếu chiều cao."); break;
                case ElementCategory.Foundation:
                    RequirePositive(project, element, issues, "HeightM", "Móng thiếu chiều cao/chiều dày.");
                    if (!(Number(project, element, "AreaM2") > 0d || (Number(project, element, "LengthM") > 0d && Number(project, element, "WidthM") > 0d))) issues.Add(new ModelHealthIssue("MISSING_DIMENSION", HealthSeverity.Error, "Móng cần Area hoặc Length+Width hợp lệ.", element.Id));
                    break;
                case ElementCategory.Earthwork:
                    RequirePositive(project, element, issues, "AreaM2", "Đào đắp thiếu diện tích."); RequirePositive(project, element, issues, "DepthM", "Đào đắp thiếu chiều sâu."); break;
                case ElementCategory.Rebar:
                    var notation = Text(project, element, "Notation"); var diameter = Number(project, element, "DiameterMm");
                    if (string.IsNullOrWhiteSpace(notation) && diameter <= 0d) issues.Add(new ModelHealthIssue("MISSING_REBAR_DEFINITION", HealthSeverity.Error, "Cốt thép cần Notation hoặc DiameterMm hợp lệ.", element.Id));
                    if (Number(project, element, "CutLengthM") <= 0d && Number(project, element, "A_M") <= 0d && !string.Equals(Text(project, element, "Shape"), "StirrupRect", StringComparison.OrdinalIgnoreCase)) issues.Add(new ModelHealthIssue("MISSING_REBAR_LENGTH", HealthSeverity.Error, "Cốt thép thiếu CutLengthM hoặc kích thước shape.", element.Id));
                    break;
            }
        }

<<<<<<< origin/main
        private static bool IsWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier ||
            category == ElementCategory.StructuralWall;

=======
        private static void RequirePositive(ProjectState project, ProjectElement element, IList<ModelHealthIssue> issues, string name, string message)
        {
            if (Number(project, element, name) <= 0d) issues.Add(new ModelHealthIssue("MISSING_DIMENSION", HealthSeverity.Error, message, element.Id));
        }
        private static double Number(ProjectState project, ProjectElement element, string name)
        {
            if (element.Properties.TryGetValue(name, out var value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var own) && !double.IsNaN(own) && !double.IsInfinity(own)) return own;
            var family = project.FindFamily(element.FamilyId); if (family != null && family.Properties.TryGetValue(name, out value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var inherited) && !double.IsNaN(inherited) && !double.IsInfinity(inherited)) return inherited; return 0d;
        }
        private static string Text(ProjectState project, ProjectElement element, string name)
        {
            if (element.Properties.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)) return value.Trim(); var family = project.FindFamily(element.FamilyId); return family != null && family.Properties.TryGetValue(name, out value) ? value?.Trim() ?? string.Empty : string.Empty;
        }
        private static bool HasMaterial(ProjectState project, ProjectElement element) => !string.IsNullOrWhiteSpace(Text(project, element, "Material"));
>>>>>>> origin/agent/full-domain-20260810
        private static bool RequiresMaterial(ElementCategory category)
        {
            switch (category)
            {
<<<<<<< origin/main
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
=======
                case ElementCategory.ArchitecturalWall: case ElementCategory.GlassWall: case ElementCategory.WallPier: case ElementCategory.FloorFinish: case ElementCategory.Waterproofing: case ElementCategory.Skirting: case ElementCategory.WallFinish: case ElementCategory.CeilingFinish: case ElementCategory.Door:
                case ElementCategory.Beam: case ElementCategory.Slab: case ElementCategory.Column: case ElementCategory.StructuralWall: case ElementCategory.Foundation: return true;
                default: return false;
>>>>>>> origin/agent/full-domain-20260810
            }
        }
    }
}
