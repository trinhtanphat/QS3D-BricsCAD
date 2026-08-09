using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public enum HealthSeverity { Info, Warning, Error }

    public sealed class ModelHealthIssue
    {
        public ModelHealthIssue(string code, HealthSeverity severity, string message, string elementId = "")
        {
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
        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();

            if (project.Metadata.TryGetValue("QS3D.ReadOnlyRecoveryRequired", out var recoveryRequired) && string.Equals(recoveryRequired, "true", StringComparison.OrdinalIgnoreCase))
            {
                var detail = project.Metadata.TryGetValue("QS3D.LoadWarning", out var warning) ? warning : "QSDB could not be loaded.";
                issues.Add(new ModelHealthIssue("PROJECT_LOAD_FAILED", HealthSeverity.Error, "Project đang ở chế độ bảo vệ và sẽ không ghi đè .qsdb: " + detail));
            }
            else if (project.Metadata.TryGetValue("QS3D.RecoveredFromBackup", out var recovered) && string.Equals(recovered, "true", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ModelHealthIssue("PROJECT_RECOVERED_BACKUP", HealthSeverity.Warning, "Project được khôi phục từ file .bak. Hãy kiểm tra dữ liệu rồi lưu lại project."));
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var handles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var familyIds = new HashSet<string>(project.Families.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var floorIds = new HashSet<string>(project.Floors.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var zoneIds = new HashSet<string>(project.Zones.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);

            foreach (var element in project.Elements)
            {
                if (!ids.Add(element.Id)) issues.Add(new ModelHealthIssue("DUPLICATE_ID", HealthSeverity.Error, "Trùng mã cấu kiện.", element.Id));
                if (string.IsNullOrWhiteSpace(element.FamilyId) || !familyIds.Contains(element.FamilyId)) issues.Add(new ModelHealthIssue("MISSING_FAMILY", HealthSeverity.Error, "Cấu kiện chưa liên kết Family hợp lệ.", element.Id));
                if (string.IsNullOrWhiteSpace(element.FloorId) || !floorIds.Contains(element.FloorId)) issues.Add(new ModelHealthIssue("MISSING_FLOOR", HealthSeverity.Warning, "Cấu kiện chưa có tầng hợp lệ.", element.Id));
                if (string.IsNullOrWhiteSpace(element.ZoneId) || !zoneIds.Contains(element.ZoneId)) issues.Add(new ModelHealthIssue("MISSING_ZONE", HealthSeverity.Warning, "Cấu kiện chưa có Zone hợp lệ.", element.Id));
                if (element.Dirty != ElementDirtyFlags.None) issues.Add(new ModelHealthIssue("DIRTY", HealthSeverity.Info, "Khối lượng/cấu kiện cần cập nhật.", element.Id));
                if ((element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door) && !element.Properties.ContainsKey("HostWallId")) issues.Add(new ModelHealthIssue("MISSING_HOST", HealthSeverity.Error, "Cửa/lỗ mở chưa có Host Wall.", element.Id));
                if (RequiresMaterial(element.Category) && !HasMaterial(project, element)) issues.Add(new ModelHealthIssue("MISSING_MATERIAL", HealthSeverity.Warning, "Cấu kiện chưa có vật liệu.", element.Id));

                foreach (var handle in element.SourceHandles.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var normalized = handle.Trim();
                    if (handles.TryGetValue(normalized, out var owner) && !string.Equals(owner, element.Id, StringComparison.OrdinalIgnoreCase)) issues.Add(new ModelHealthIssue("DUPLICATE_HANDLE", HealthSeverity.Warning, "CAD Handle đang được nhiều QS3D element sử dụng; element khác: " + owner, element.Id));
                    else handles[normalized] = element.Id;
                }

                if (liveHandles != null && element.SourceHandles.Count > 0 && element.SourceHandles.All(x => !liveHandles.Contains(x))) issues.Add(new ModelHealthIssue("ORPHAN_HANDLE", HealthSeverity.Error, "Không còn tìm thấy đối tượng CAD nguồn.", element.Id));
            }
            return issues;
        }

        private static bool HasMaterial(ProjectState project, ProjectElement element)
        {
            if (element.Properties.TryGetValue("Material", out var own) && !string.IsNullOrWhiteSpace(own)) return true;
            var family = project.FindFamily(element.FamilyId);
            return family != null && family.Properties.TryGetValue("Material", out var familyMaterial) && !string.IsNullOrWhiteSpace(familyMaterial);
        }

        private static bool RequiresMaterial(ElementCategory category)
        {
            switch (category)
            {
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
