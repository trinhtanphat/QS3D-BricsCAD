using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class RebarFabricationQualificationHealthService
    {
        public const string RequireQualificationMetadataKey = "QS3D.RebarFabrication.RequireQualification";
        public const string StandardCodeMetadataKey = "QS3D.RebarFabrication.StandardCode";
        public const string DetailingRevisionMetadataKey = "QS3D.RebarFabrication.DetailingRevision";

        public const string StatusPropertyKey = "RebarFabricationStatus";
        public const string StandardCodePropertyKey = "RebarFabricationStandardCode";
        public const string DetailingRevisionPropertyKey = "RebarFabricationDetailingRevision";

        private const string ApprovedStatus = "Approved";

        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            var issues = new List<ModelHealthIssue>();
            var requirement = Read(project.Metadata, RequireQualificationMetadataKey);
            var required = ParseRequirement(requirement, out var validRequirement);
            if (!validRequirement)
            {
                issues.Add(new ModelHealthIssue(
                    "REBAR_FAB_REQUIREMENT_INVALID",
                    HealthSeverity.Error,
                    "QS3D.RebarFabrication.RequireQualification chỉ chấp nhận true/yes/1 hoặc false/no/0. Giá trị không hợp lệ không được phép âm thầm tắt fabrication qualification."));
                required = true;
            }
            if (!required) return issues.AsReadOnly();

            if (validRequirement)
            {
                foreach (var element in project.Elements)
                    if (element == null)
                        throw new InvalidOperationException("Rebar fabrication qualification diagnostics cannot inspect a project containing a null semantic element.");
            }

            var standardCode = Read(project.Metadata, StandardCodeMetadataKey);
            var detailingRevision = Read(project.Metadata, DetailingRevisionMetadataKey);

            if (standardCode.Length == 0)
                issues.Add(new ModelHealthIssue(
                    "REBAR_FAB_STANDARD_MISSING",
                    HealthSeverity.Error,
                    "Fabrication qualification đã được bật nhưng project chưa khai báo mã tiêu chuẩn thép. QS3D không tự suy đoán TCVN/ACI/BS hoặc giá trị kỹ thuật tương đương."));

            if (detailingRevision.Length == 0)
                issues.Add(new ModelHealthIssue(
                    "REBAR_FAB_REVISION_MISSING",
                    HealthSeverity.Error,
                    "Fabrication qualification đã được bật nhưng project chưa khai báo revision hồ sơ/detailing thép."));

            var rebarElements = project.Elements
                .Where(x => x != null && HasGeneratedRebarOutput(x))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (rebarElements.Length == 0)
            {
                issues.Add(new ModelHealthIssue(
                    "REBAR_FAB_OUTPUT_MISSING",
                    HealthSeverity.Error,
                    "Fabrication qualification đã được bật nhưng project chưa có generated rebar output để kiểm tra."));
                return issues.AsReadOnly();
            }

            foreach (var element in rebarElements)
            {
                var status = Read(element.Properties, StatusPropertyKey);
                if (!string.Equals(status, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
                    issues.Add(new ModelHealthIssue(
                        "REBAR_FAB_NOT_APPROVED",
                        HealthSeverity.Error,
                        "Generated rebar chưa có RebarFabricationStatus=Approved. Gate này chỉ xác nhận evidence/provenance đã được duyệt, không tự chứng nhận tuân thủ tiêu chuẩn kỹ thuật.",
                        element.Id));

                ValidateProjectBinding(
                    element,
                    StandardCodePropertyKey,
                    standardCode,
                    "REBAR_FAB_ELEMENT_STANDARD_MISSING",
                    "REBAR_FAB_STANDARD_MISMATCH",
                    "mã tiêu chuẩn thép",
                    issues);

                ValidateProjectBinding(
                    element,
                    DetailingRevisionPropertyKey,
                    detailingRevision,
                    "REBAR_FAB_ELEMENT_REVISION_MISSING",
                    "REBAR_FAB_REVISION_MISMATCH",
                    "revision detailing",
                    issues);
            }

            return issues.AsReadOnly();
        }

        private static bool ParseRequirement(string value, out bool valid)
        {
            if (value.Length == 0)
            {
                valid = true;
                return false;
            }
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "1", StringComparison.OrdinalIgnoreCase))
            {
                valid = true;
                return true;
            }
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "0", StringComparison.OrdinalIgnoreCase))
            {
                valid = true;
                return false;
            }
            valid = false;
            return true;
        }

        private static bool HasGeneratedRebarOutput(ProjectElement element)
        {
            foreach (var key in GeneratedHandleOwnershipPolicy.RebarHandleKeys)
            {
                if (element.Properties.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw)) return true;
            }
            return false;
        }

        private static void ValidateProjectBinding(
            ProjectElement element,
            string propertyKey,
            string projectValue,
            string missingCode,
            string mismatchCode,
            string label,
            ICollection<ModelHealthIssue> issues)
        {
            var elementValue = Read(element.Properties, propertyKey);
            if (elementValue.Length == 0)
            {
                issues.Add(new ModelHealthIssue(
                    missingCode,
                    HealthSeverity.Error,
                    "Generated rebar chưa khai báo " + label + " cho fabrication qualification.",
                    element.Id));
                return;
            }

            if (projectValue.Length > 0 && !string.Equals(elementValue, projectValue, StringComparison.OrdinalIgnoreCase))
                issues.Add(new ModelHealthIssue(
                    mismatchCode,
                    HealthSeverity.Error,
                    "Generated rebar có " + label + " không khớp project: '" + elementValue + "' != '" + projectValue + "'.",
                    element.Id));
        }

        private static string Read(IDictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return raw.Trim();
        }
    }
}
