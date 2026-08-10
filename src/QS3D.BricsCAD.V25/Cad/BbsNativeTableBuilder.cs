using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class BbsNativeTableBuilder
    {
        internal static readonly ProjectOwnedNativeTableDefinition Definition = new ProjectOwnedNativeTableDefinition(
            "RebarBbsSchedule",
            "RebarBbsTable",
            "GeneratedBbsTable",
            0.0032d,
            0.0075d,
            0.027d);

        private static readonly string[] Headers =
        {
            "Mã cấu kiện",
            "Bar Mark",
            "Shape",
            "Ký hiệu",
            "Ø (mm)",
            "SL",
            "Dài cắt (m)",
            "Tổng dài (m)",
            "kg/m",
            "KL tịnh (kg)",
            "Hao hụt (%)",
            "KL tổng (kg)",
            "Fabrication",
            "Standard",
            "Revision"
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
                    "BBS_" + x.Code,
                    x.Severity,
                    x.Message,
                    x.ElementId))
                .ToList();

            if (project.Metadata.ContainsKey(Definition.HandleKey) &&
                project.Elements.Any(x => x != null &&
                    x.Dirty != ElementDirtyFlags.None &&
                    x.Properties.TryGetValue("RebarNotation", out var notation) &&
                    !string.IsNullOrWhiteSpace(notation)))
            {
                issues.Add(new ModelHealthIssue(
                    "BBS_TABLE_PROJECT_DIRTY",
                    HealthSeverity.Warning,
                    "BBS native Table đang tồn tại nhưng semantic rebar input còn dirty. Chạy QS3DBBSTABLEREFRESH hoặc QS3DREGEN trước khi dùng schedule.",
                    string.Empty));
            }

            return issues.AsReadOnly();
        }

        public static NativeDocumentationTableSnapshot BuildSnapshot(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var schedule = ProjectRebarScheduleBuilder.Build(project);
            if (schedule.Count == 0)
                throw new InvalidOperationException("Project chưa có RebarNotation hợp lệ để tạo BBS native Table.");

            var rows = new List<IReadOnlyList<string>>(schedule.Count);
            foreach (var row in schedule)
            {
                if (row == null) throw new InvalidOperationException("BBS schedule contains a null row.");
                if (row.Quantity <= 0) throw new InvalidOperationException("BBS schedule Quantity must be greater than zero.");
                rows.Add(new[]
                {
                    row.ElementId ?? string.Empty,
                    row.BarMark ?? string.Empty,
                    row.ShapeCode ?? string.Empty,
                    row.Notation ?? string.Empty,
                    Number(row.DiameterMm, "DiameterMm"),
                    row.Quantity.ToString(CultureInfo.InvariantCulture),
                    Number(row.CuttingLengthM, "CuttingLengthM"),
                    Number(row.TotalLengthM, "TotalLengthM"),
                    Number(row.UnitWeightKgM, "UnitWeightKgM"),
                    Number(row.NetWeightKg, "NetWeightKg"),
                    Number(row.WastePercent, "WastePercent"),
                    Number(row.TotalWeightKg, "TotalWeightKg"),
                    row.FabricationStatus ?? string.Empty,
                    row.FabricationStandardCode ?? string.Empty,
                    row.FabricationDetailingRevision ?? string.Empty
                });
            }

            return new NativeDocumentationTableSnapshot(
                "QS3D BBS • Bar Bending Schedule",
                Headers.ToArray(),
                rows.AsReadOnly());
        }

        private static string Number(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException("BBS schedule " + label + " must be finite and non-negative.");
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }
    }
}
