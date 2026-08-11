using System;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RoomBoundaryCommands
    {
        private const double DefaultSnapToleranceM = 0.005d;
        private const double DefaultArcSagittaM = 0.002d;
        private const double DefaultPlanarityToleranceM = 0.005d;
        private const double DefaultSplineChordM = 0.02d;
        private const double DefaultMinimumAreaM2 = 0.01d;

        [CommandMethod("QS3DROOMAUTO", CommandFlags.UsePickSet)]
        public void DiscoverRooms()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                ProjectState? previewProject = null;
                string? expectedProjectId = null;
                if (ProjectContextCoordinator.TryGetReadOnly(document, out var existingPreview))
                {
                    previewProject = existingPreview;
                    expectedProjectId = existingPreview.ProjectId;
                }

                var snapTolerance = previewProject == null
                    ? DefaultSnapToleranceM
                    : MetadataNumber(previewProject, "RoomBoundarySnapToleranceM", DefaultSnapToleranceM, 0d);
                var sagitta = previewProject == null
                    ? DefaultArcSagittaM
                    : MetadataNumber(previewProject, "RoomBoundaryArcSagittaM", DefaultArcSagittaM, 0d);
                var planarityTolerance = previewProject == null
                    ? DefaultPlanarityToleranceM
                    : MetadataNumber(previewProject, "RoomBoundaryPlanarityToleranceM", DefaultPlanarityToleranceM, 0d);
                var splineChord = previewProject == null
                    ? DefaultSplineChordM
                    : MetadataNumber(previewProject, "RoomBoundarySplineChordM", DefaultSplineChordM, 0d);

                var segments = RoomBoundarySegmentReader.ReadCurrentSelection(document, sagitta, planarityTolerance, splineChord);
                if (segments.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DROOMAUTO: chọn LINE/ARC/SPLINE/POLYLINE boundary đồng phẳng tạo vùng kín.");
                    return;
                }

                var minimumArea = previewProject == null
                    ? DefaultMinimumAreaM2
                    : MetadataNumber(previewProject, "RoomBoundaryMinimumAreaM2", DefaultMinimumAreaM2, 0d);
                var boundaries = RoomBoundaryDiscovery.Discover(segments, snapTolerance, minimumArea);
                if (boundaries.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DROOMAUTO: không phát hiện closed boundary hợp lệ trong selection.");
                    return;
                }

                ProjectState project;
                if (expectedProjectId != null)
                {
                    project = ExistingProjectMutationContext.Require(document, "Room Auto");
                    if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("QS3D project đã thay đổi trong lúc đọc Room boundary. Hãy chạy lại lệnh.");
                }
                else
                {
                    // QS3DROOMAUTO is creation-capable only after the user supplied usable CAD
                    // boundaries and at least one closed face was discovered. Cancel/empty/no-face
                    // paths above must never bootstrap a blank project.
                    project = ProjectContextCoordinator.GetOrCreate(document);
                }

                var result = RoomBoundaryMaterializer.Materialize(document, project, boundaries);
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();
                var summary = "Room Auto: " + boundaries.Count + " boundary • " + result.RoomCount + " Room • " + result.FloorCount + " floor • " + result.WallCount + " wall";
                PaletteCoordinator.SetStatus(summary);
                document.Editor.WriteMessage("\nQS3D " + summary + ".");
                if (result.RoomCount == 0)
                    document.Editor.WriteMessage("\nQS3DROOMAUTO: tất cả boundary bị bỏ qua hoặc đã có RoomFingerprint hiện hữu.");
            }
            catch (Exception ex)
            {
                var message = "QS3DROOMAUTO lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static double MetadataNumber(ProjectState project, string key, double fallback, double minimumExclusive)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= minimumExclusive)
                throw new InvalidOperationException(key + " không hợp lệ: " + raw);
            return value;
        }
    }
}
