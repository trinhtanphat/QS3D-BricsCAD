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
    internal static class DoorOpeningNativeTableBuilder
    {
        internal static readonly ProjectOwnedNativeTableDefinition Definition = new ProjectOwnedNativeTableDefinition(
            "DoorOpeningSchedule",
            "DoorOpeningTable",
            "GeneratedDoorOpeningTable",
            0.0035d,
            0.008d,
            0.028d);

        private static readonly string[] Headers =
        {
            "Tầng",
            "Loại",
            "Family",
            "Vật liệu",
            "Rộng (m)",
            "Cao (m)",
            "Bậu (m)",
            "Dày (m)",
            "Số lượng",
            "Diện tích lỗ mở (m²)",
            "Số host"
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
                    "DOOR_OPENING_" + x.Code,
                    x.Severity,
                    x.Message,
                    x.ElementId))
                .ToList()
                .AsReadOnly();
        }

        public static NativeDocumentationTableSnapshot BuildSnapshot(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var schedule = DoorOpeningScheduleBuilder.Build(project);
            if (schedule.Count == 0)
                throw new InvalidOperationException("Project chưa có Door/WallOpening hợp lệ để tạo Door/Opening Schedule Table.");

            var rows = new List<IReadOnlyList<string>>(schedule.Count);
            foreach (var row in schedule)
            {
                if (row == null) throw new InvalidOperationException("Door/Opening schedule contains a null row.");
                rows.Add(new[]
                {
                    row.Floor ?? string.Empty,
                    row.Category ?? string.Empty,
                    row.FamilyName ?? string.Empty,
                    row.Material ?? string.Empty,
                    Number(row.WidthM, "WidthM"),
                    Number(row.HeightM, "HeightM"),
                    Number(row.SillHeightM, "SillHeightM"),
                    Number(row.ThicknessM, "ThicknessM"),
                    row.Count.ToString(CultureInfo.InvariantCulture),
                    Number(row.OpeningAreaM2, "OpeningAreaM2"),
                    row.HostCount.ToString(CultureInfo.InvariantCulture)
                });
            }

            return new NativeDocumentationTableSnapshot(
                "QS3D Door / Opening Schedule",
                Headers.ToArray(),
                rows.AsReadOnly());
        }

        private static string Number(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Door/Opening schedule " + label + " must be finite.");
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
