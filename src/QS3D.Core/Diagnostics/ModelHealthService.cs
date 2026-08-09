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
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (!ids.Add(element.Id)) issues.Add(new ModelHealthIssue("DUPLICATE_ID", HealthSeverity.Error, "Trùng mã cấu kiện.", element.Id));
                if (string.IsNullOrWhiteSpace(element.FloorId)) issues.Add(new ModelHealthIssue("MISSING_FLOOR", HealthSeverity.Warning, "Cấu kiện chưa có tầng.", element.Id));
                if (element.Dirty != ElementDirtyFlags.None) issues.Add(new ModelHealthIssue("DIRTY", HealthSeverity.Info, "Khối lượng/cấu kiện cần cập nhật.", element.Id));
                if ((element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door) && element.DependsOn.Count == 0)
                    issues.Add(new ModelHealthIssue("MISSING_HOST", HealthSeverity.Error, "Cửa/lỗ mở chưa có Host Wall.", element.Id));
                if (liveHandles != null && element.SourceHandles.Count > 0 && element.SourceHandles.All(x => !liveHandles.Contains(x)))
                    issues.Add(new ModelHealthIssue("ORPHAN_HANDLE", HealthSeverity.Error, "Không còn tìm thấy đối tượng CAD nguồn.", element.Id));
            }
            return issues;
        }
    }
}
