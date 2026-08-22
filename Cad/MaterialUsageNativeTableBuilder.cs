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
    internal static class MaterialUsageNativeTableBuilder
    {
        internal static readonly ProjectOwnedNativeTableDefinition Definition = new ProjectOwnedNativeTableDefinition(
            "MaterialUsageSchedule",
            "MaterialUsageTable",
            "GeneratedMaterialUsageTable",
            0.0035d,
            0.008d,
            0.028d);

        private static readonly string[] Headers =
        {
            "Tầng",
            "Vật liệu",
            "ĐVT",
            "Thành phần",
            "Loại",
            "Family",
            "Số cấu kiện",
            "Dài (m)",
            "Diện tích (m²)",
            "Thể tích (m³)",
            "Khối lượng (kg)",
            "Khối lượng chính"
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
            return ProjectOwnedNativeTableArtifactService.Inspect(document, project, Definition, () => BuildSnapshot(project))
                .Select(x => new ModelHealthIssue(
                    "MATERIAL_USAGE_" + x.Code,
                    x.Severity,
                    x.Message,
                    x.ElementId))
                .ToList()
                .AsReadOnly();
        }

        public static NativeDocumentationTableSnapshot BuildSnapshot(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var schedule = MaterialUsageScheduleBuilder.Build(project);
            if (schedule.Count == 0)
                throw new InvalidOperationException("Project chưa có material usage hợp lệ để tạo Material Usage Schedule Table.");

            var rows = new List<IReadOnlyList<string>>(schedule.Count);
            foreach (var row in schedule)
            {
                if (row == null) throw new InvalidOperationException("Material Usage schedule contains a null row.");
                rows.Add(new[]
                {
                    row.Floor ?? string.Empty,
                    row.MaterialName ?? string.Empty,
                    row.UnitHint ?? string.Empty,
                    row.Component ?? string.Empty,
                    row.Category ?? string.Empty,
                    row.FamilyName ?? string.Empty,
                    row.ElementCount.ToString(CultureInfo.InvariantCulture),
                    Number(row.LengthM, "LengthM"),
                    Number(row.AreaM2, "AreaM2"),
                    Number(row.VolumeM3, "VolumeM3"),
                    Number(row.MassKg, "MassKg"),
                    Number(row.PrimaryQuantity, "PrimaryQuantity")
                });
            }

            return new NativeDocumentationTableSnapshot(
                "QS3D Material Usage Schedule",
                Headers.ToArray(),
                rows.AsReadOnly());
        }

        private static string Number(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException("Material Usage schedule " + label + " must be finite and non-negative.");
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
