using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class BqNativeTableBuilder
    {
        internal static readonly ProjectOwnedNativeTableDefinition Definition = new ProjectOwnedNativeTableDefinition(
            "QuantityReportSchedule",
            "BqQuantityTable",
            "GeneratedBqTable",
            0.0032d,
            0.0075d,
            0.028d);

        private static readonly string[] Headers =
        {
            "Tầng",
            "Zone",
            "Loại",
            "Tên cấu kiện",
            "SL",
            "BT gộp (m³)",
            "Trừ giao (m³)",
            "BT còn (m³)",
            "Cốp pha (m²)",
            "Dài (m)",
            "Chu vi ngoài (m)",
            "Chu vi trong (m)",
            "DT cửa (m²)",
            "Thành bên (m²)",
            "DT đáy (m²)",
            "DT đỉnh (m²)",
            "DT khác (m²)",
            "QS3D Element ID",
            "CAD Handle (hex)",
            "QS3D Drawing Fingerprint"
        };

        public static string Build(Document document, ProjectState project, Point3d position)
        {
            return ProjectOwnedNativeTableArtifactService.Build(document, project, Definition, BuildSnapshot(project), position);
        }

        public static void Remove(Document document, ProjectState project)
        {
            ProjectOwnedNativeTableArtifactService.Remove(document, project, Definition);
        }

        public static Point3d StoredPosition(ProjectState project)
        {
            return ProjectOwnedNativeTableArtifactService.StoredPosition(project, Definition);
        }

        public static IReadOnlyList<ModelHealthIssue> Inspect(Document document, ProjectState project)
        {
            var issues = ProjectOwnedNativeTableArtifactService.Inspect(document, project, Definition, () => BuildSnapshot(project))
                .Select(x => new ModelHealthIssue(
                    "BQ_" + x.Code,
                    x.Severity,
                    x.Message,
                    x.ElementId))
                .ToList();

            if (project.Metadata.ContainsKey(Definition.HandleKey) &&
                project.Elements.Any(x => x != null && x.Dirty != ElementDirtyFlags.None))
            {
                issues.Add(new ModelHealthIssue(
                    "BQ_TABLE_PROJECT_DIRTY",
                    HealthSeverity.Warning,
                    "BQ native Table đang tồn tại nhưng semantic model còn dirty. Chạy QS3DBQTABLEREFRESH hoặc QS3DREGEN trước khi dùng số liệu.",
                    string.Empty));
            }

            return issues.AsReadOnly();
        }

        public static NativeDocumentationTableSnapshot BuildSnapshot(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var rows = ProjectQuantityReportBuilder.Group(project);
            if (rows.Count == 0)
                throw new InvalidOperationException("Project chưa có quantity row hợp lệ để tạo BQ native Table.");

            var output = new List<IReadOnlyList<string>>(rows.Count);
            foreach (var row in rows)
            {
                if (row == null) throw new InvalidOperationException("BQ report contains a null row.");
                output.Add(new[]
                {
                    row.Floor ?? string.Empty,
                    row.Zone ?? string.Empty,
                    row.Category ?? string.Empty,
                    row.FamilyName ?? string.Empty,
                    row.Count.ToString(CultureInfo.InvariantCulture),
                    Number(row.GrossConcreteM3, "GrossConcreteM3"),
                    Number(row.DeductionM3, "DeductionM3"),
                    Number(row.NetConcreteM3, "NetConcreteM3"),
                    Number(row.FormworkM2, "FormworkM2"),
                    Number(row.LengthM, "LengthM"),
                    Number(row.OuterPerimeterM, "OuterPerimeterM"),
                    Number(row.InnerPerimeterM, "InnerPerimeterM"),
                    Number(row.DoorAreaM2, "DoorAreaM2"),
                    Number(row.SideAreaM2, "SideAreaM2"),
                    Number(row.BottomAreaM2, "BottomAreaM2"),
                    Number(row.TopAreaM2, "TopAreaM2"),
                    Number(row.OtherAreaM2, "OtherAreaM2"),
                    row.ElementIdText ?? string.Empty,
                    row.SourceHandleText ?? string.Empty,
                    row.DrawingFingerprint ?? string.Empty
                });
            }

            return new NativeDocumentationTableSnapshot(
                "QS3D BQ Tổng hợp",
                Headers.ToArray(),
                output.AsReadOnly());
        }

        private static string Number(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("BQ report " + label + " must be finite.");
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }
    }
}
