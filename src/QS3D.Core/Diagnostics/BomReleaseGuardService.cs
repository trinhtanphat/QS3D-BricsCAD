using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using QS3D.Core.Services;

namespace QS3D.Core.Diagnostics
{
    public static class BomReleaseGuardService
    {
        internal const int MaxLiveGeneratedHandleInputs = 10000;

        public static IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project, ISet<string>? liveGeneratedHandles = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            ISet<string>? liveHandleIndex = null;
            if (liveGeneratedHandles != null)
            {
                var reportedHandleCount = liveGeneratedHandles.Count;
                if (reportedHandleCount < 0)
                    throw LiveHandleInputNegativeCount();
                if (reportedHandleCount > MaxLiveGeneratedHandleInputs)
                    throw LiveHandleInputTooLarge();

                var index = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var observedHandleCount = 0;
                foreach (var handle in liveGeneratedHandles)
                {
                    observedHandleCount++;
                    if (observedHandleCount > MaxLiveGeneratedHandleInputs)
                        throw LiveHandleInputTooLarge();

                    var normalized = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle);
                    if (normalized.Length > 0) index.Add(normalized);
                }
                liveHandleIndex = index;
            }

            var issues = new List<ModelHealthIssue>();
            try
            {
                issues.AddRange(new RoomFinishHealthService().Inspect(project));
            }
            catch (InvalidOperationException)
            {
                issues.Add(new ModelHealthIssue(
                    "BOM_ROOM_FINISH_HEALTH_FAILED",
                    HealthSeverity.Error,
                    "Không thể chạy chẩn đoán Room Finish an toàn; phát hành BQ bị chặn."));
            }
            try
            {
                issues.AddRange(new GeneratedCurtainPanelHealthService().Inspect(project, liveHandleIndex));
            }
            catch (InvalidOperationException)
            {
                issues.Add(new ModelHealthIssue(
                    "BOM_CURTAIN_PANEL_HEALTH_FAILED",
                    HealthSeverity.Error,
                    "Không thể chạy chẩn đoán Curtain Panel an toàn; phát hành BQ bị chặn."));
            }

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
                catch (Exception)
                {
                    issues.Add(new ModelHealthIssue(
                        "BOM_EXCLUSION_FAILED",
                        HealthSeverity.Error,
                        "Không thể quyết định an toàn cấu kiện có được đưa vào BQ hay không.",
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

                foreach (var property in element.Properties)
                    if (IsInvalidDiagnosticKey(property.Key))
                        issues.Add(new ModelHealthIssue("BOM_PROPERTY_KEY_INVALID", HealthSeverity.Error, "Property key phải là tên canonical, không rỗng, không chứa ký tự điều khiển/XML-invalid và không có khoảng trắng bao quanh.", element.Id));

                foreach (var quantity in element.Quantities)
                {
                    if (IsInvalidDiagnosticKey(quantity.Key))
                    {
                        issues.Add(new ModelHealthIssue("BOM_QUANTITY_KEY_INVALID", HealthSeverity.Error, "Quantity key phải là tên canonical, không rỗng, không chứa ký tự điều khiển/XML-invalid và không có khoảng trắng bao quanh.", element.Id));
                        continue;
                    }
                    if (double.IsNaN(quantity.Value) || double.IsInfinity(quantity.Value))
                        issues.Add(new ModelHealthIssue("BOM_QUANTITY_NONFINITE", HealthSeverity.Error, "Quantity " + quantity.Key + " không phải số hữu hạn.", element.Id));
                }

                try
                {
                    var traceHandles = SourceHandleResolver.Resolve(project, new[] { element.Id });
                    if (traceHandles.Count == 0)
                        issues.Add(new ModelHealthIssue("BOM_TRACEABILITY_MISSING", HealthSeverity.Warning, "Dòng khối lượng không truy vết được về CAD Handle nguồn/generated.", element.Id));
                }
                catch (Exception)
                {
                    issues.Add(new ModelHealthIssue("BOM_TRACEABILITY_FAILED", HealthSeverity.Error, "Không thể dựng provenance Handle an toàn cho cấu kiện.", element.Id));
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
            catch (Exception)
            {
                issues.Add(new ModelHealthIssue("BOM_REPORT_FAILED", HealthSeverity.Error, "Không thể dựng bảng khối lượng an toàn."));
            }

            return issues.AsReadOnly();
        }

        private static bool IsInvalidDiagnosticKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) ||
                !string.Equals(key, key.Trim(), StringComparison.Ordinal) ||
                key.Any(char.IsControl))
                return true;

            try
            {
                XmlConvert.VerifyXmlChars(key);
                return false;
            }
            catch (XmlException)
            {
                return true;
            }
        }

        private static InvalidOperationException LiveHandleInputNegativeCount()
        {
            return new InvalidOperationException("BOM live generated Handle input reported a negative known count.");
        }

        private static InvalidOperationException LiveHandleInputTooLarge()
        {
            return new InvalidOperationException("BOM live generated Handle input exceeds the supported bound of " + MaxLiveGeneratedHandleInputs + ".");
        }
    }
}
