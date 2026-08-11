using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using QS3D.Core.Services;

namespace QS3D.Core.Diagnostics
{
    public static class BomReleaseGuardService
    {
        public static IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveGeneratedHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            ISet<string>? liveHandleIndex = null;
            if (liveGeneratedHandles != null)
            {
                var index = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var handle in liveGeneratedHandles)
                    if (handle != null) index.Add(handle);
                liveHandleIndex = index;
            }

            var issues = new List<ModelHealthIssue>();
            issues.AddRange(new RoomFinishHealthService().Inspect(project));
            issues.AddRange(new GeneratedCurtainPanelHealthService().Inspect(project, liveHandleIndex));

            var included = new List<ProjectElement>();
            foreach (var element in project.Elements)
            {
                if (element == null)
                {
                    issues.Add(new ModelHealthIssue("BOM_NULL_ELEMENT", HealthSeverity.Error, "Project chứa semantic element null; phát hành BQ bị chặn cho tới khi project được repair."));
                    continue;
                }
                try
                {
                    if (!AutoRoomLifecycle.IsExcludedFromQuantity(project, element)) included.Add(element);
                }
                catch (Exception ex)
                {
                    issues.Add(new ModelHealthIssue(
                        "BOM_EXCLUSION_FAILED",
                        HealthSeverity.Error,
                        "Không thể quyết định an toàn cấu kiện có được đưa vào BQ hay không: " + ex.Message,
                        element.Id));
                }
            }

            if (included.Count == 0)
                issues.Add(new ModelHealthIssue("BOM_EMPTY", HealthSeverity.Warning, "Project chưa có semantic element đủ điều kiện để phát hành bảng khối lượng."));

            foreach (var element in included)
            {
                if ((element.Dirty & ElementDirtyFlags.Quantity) != 0)
                    issues.Add(new ModelHealthIssue("BOM_QUANTITY_DIRTY", HealthSeverity.Warning, "Khối lượng chưa được regenerate sau thay đổi semantic gần nhất.", element.Id));

                if (element.Quantities.Count == 0)
                    issues.Add(new ModelHealthIssue("BOM_QUANTITY_EMPTY", HealthSeverity.Warning, "Cấu kiện chưa có quantity đã tính để đưa vào bảng khối lượng.", element.Id));

                foreach (var quantity in element.Quantities)
                {
                    if (string.IsNullOrWhiteSpace(quantity.Key))
                        issues.Add(new ModelHealthIssue("BOM_QUANTITY_KEY_INVALID", HealthSeverity.Error, "Quantity key không được để trống.", element.Id));
                    if (double.IsNaN(quantity.Value) || double.IsInfinity(quantity.Value))
                        issues.Add(new ModelHealthIssue("BOM_QUANTITY_NONFINITE", HealthSeverity.Error, "Quantity " + quantity.Key + " không phải số hữu hạn.", element.Id));
                }

                try
                {
                    var traceHandles = SourceHandleResolver.Resolve(project, new[] { element.Id });
                    if (traceHandles.Count == 0)
                        issues.Add(new ModelHealthIssue("BOM_TRACEABILITY_MISSING", HealthSeverity.Warning, "Dòng khối lượng không truy vết được về CAD Handle nguồn/generated.", element.Id));
                }
                catch (Exception ex)
                {
                    issues.Add(new ModelHealthIssue("BOM_TRACEABILITY_FAILED", HealthSeverity.Error, "Không thể dựng provenance Handle an toàn: " + ex.Message, element.Id));
                }

                if (liveHandleIndex != null)
                    foreach (var entry in GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(element))
                        if (!liveHandleIndex.Contains(entry.Key))
                            issues.Add(new ModelHealthIssue("BOM_GENERATED_HANDLE_MISSING", HealthSeverity.Error, entry.Value + " tham chiếu CAD Handle không còn tồn tại: " + entry.Key + ".", element.Id));
            }

            try
            {
                var rows = ProjectQuantityReportBuilder.Group(project);
                var rowOwners = rows.SelectMany(x => x.ElementIds).GroupBy(x => x, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
                foreach (var element in included)
                {
                    if (!rowOwners.TryGetValue(element.Id, out var count))
                        issues.Add(new ModelHealthIssue("BOM_ROW_MISSING", HealthSeverity.Error, "Cấu kiện không xuất hiện trong bảng khối lượng đã nhóm.", element.Id));
                    else if (count != 1)
                        issues.Add(new ModelHealthIssue("BOM_ROW_DUPLICATE", HealthSeverity.Error, "Cấu kiện xuất hiện nhiều hơn một lần trong bảng khối lượng đã nhóm.", element.Id));
                }
                foreach (var row in rows.Where(x => x.ElementIds.Count > 0 && x.SourceHandles.Count == 0))
                    issues.Add(new ModelHealthIssue("BOM_ROW_HANDLE_MISSING", HealthSeverity.Warning, "Nhóm khối lượng không có CAD Handle để truy xuất ngược.", row.ElementIds[0]));
            }
            catch (Exception ex)
            {
                issues.Add(new ModelHealthIssue("BOM_REPORT_FAILED", HealthSeverity.Error, "Không thể dựng bảng khối lượng an toàn: " + ex.Message));
            }

            return issues.AsReadOnly();
        }
    }
}
