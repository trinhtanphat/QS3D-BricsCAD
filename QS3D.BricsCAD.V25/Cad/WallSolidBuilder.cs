using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class WallSolidBuilder
    {
        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public string PreviousHandle { get; set; } = string.Empty;
            public string GeneratedHandle { get; set; } = string.Empty;
            public double LengthM { get; set; }
            public double ThicknessM { get; set; }
            public double HeightM { get; set; }
        }

        public static int BuildSelectedLineWalls(Document document, ProjectState project) =>
            BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall);

        public static int BuildSelectedLineWalls(Document document, ProjectState project, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!IsSupportedWall(category)) throw new ArgumentOutOfRangeException(nameof(category), "Unsupported architectural wall category: " + category);
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var sourceIds = selection.Value.GetObjectIds();
            if (sourceIds.Length == 0) return 0;
            var pending = new List<PendingUpdate>();
            var processedElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var id in sourceIds)
                {
                    var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line;
                    if (line == null) continue;
                    var sourceHandle = line.Handle.ToString();
                    var matches = project.Elements
                        .Where(x => x.Category == category && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)))
                        .Take(2)
                        .ToList();
                    if (matches.Count == 0) continue;
                    if (matches.Count > 1) throw new InvalidOperationException("CAD source handle " + sourceHandle + " đang thuộc nhiều QS3D wall element.");
                    var element = matches[0];
                    if (!processedElements.Add(element.Id)) throw new InvalidOperationException("Wall element " + element.Id + " có nhiều source đang được chọn. Tách/capture từng source thành element riêng trước khi Vẽ 3D.");

                    var family = project.FindFamily(element.FamilyId);
                    var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "ThicknessM", .2d), element.Id + "/ThicknessM");
                    var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", 3.6d), element.Id + "/HeightM");
                    var bottomOffsetM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
                    var dx = CadGeometryGuard.Finite(line.EndPoint.X - line.StartPoint.X, element.Id + "/dx");
                    var dy = CadGeometryGuard.Finite(line.EndPoint.Y - line.StartPoint.Y, element.Id + "/dy");
                    var length = CadGeometryGuard.Hypot(dx, dy, element.Id + "/source length");
                    if (length <= 1e-6) throw new InvalidOperationException("Wall source LINE quá ngắn: " + element.Id);

                    var thickness = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, thicknessM, element.Id + "/ThicknessM"), element.Id + "/Thickness drawing units");
                    var height = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, heightM, element.Id + "/HeightM"), element.Id + "/Height drawing units");
                    var bottomOffset = CadGeometryGuard.ToDrawingUnits(document, bottomOffsetM, element.Id + "/BottomOffsetM");
                    var angle = CadGeometryGuard.Finite(Math.Atan2(dy, dx), element.Id + "/angle");
                    var midX = CadGeometryGuard.Midpoint(line.StartPoint.X, line.EndPoint.X, element.Id + "/mid X");
                    var midY = CadGeometryGuard.Midpoint(line.StartPoint.Y, line.EndPoint.Y, element.Id + "/mid Y");
                    var midZ = CadGeometryGuard.Add(line.StartPoint.Z, bottomOffset, element.Id + "/base Z");
                    midZ = CadGeometryGuard.Add(midZ, height / 2d, element.Id + "/mid Z");
                    var mid = new Point3d(midX, midY, midZ);

                    var solid = new Solid3d();
                    try
                    {
                        solid.SetDatabaseDefaults(document.Database);
                        solid.CreateBox(length, thickness, height);
                        solid.TransformBy(Matrix3d.Displacement(new Vector3d(-length / 2d, -thickness / 2d, -height / 2d)));
                        solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
                        solid.TransformBy(Matrix3d.Displacement(new Vector3d(mid.X, mid.Y, mid.Z)));
                        solid.Layer = line.Layer;

                        var previousHandle = GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                        modelSpace.AppendEntity(solid);
                        transaction.AddNewlyCreatedDBObject(solid, true);
                        GeneratedGeometryService.MarkGenerated(document, transaction, solid, project.ProjectId, element.Id, ElementCategory.ArchitecturalWall);
                        pending.Add(new PendingUpdate
                        {
                            Element = element,
                            PreviousHandle = previousHandle,
                            GeneratedHandle = solid.Handle.ToString(),
                            LengthM = CadGeometryGuard.ToMeters(document, length, element.Id + "/source length"),
                            ThicknessM = thicknessM,
                            HeightM = heightM
                        });
                    }
                    catch
                    {
                        solid.Dispose();
                        throw;
                    }
                }
                transaction.Commit();
            }

            foreach (var update in pending)
            {
<<<<<<< Updated upstream
                GeneratedGeometryService.CommitReplacement(update.Element, update.PreviousHandle, update.GeneratedHandle, category);
=======
                GeneratedGeometryService.CommitReplacement(project, update.Element, update.PreviousHandle, update.GeneratedHandle, ElementCategory.ArchitecturalWall);
>>>>>>> Stashed changes
                update.Element.Properties["LengthM"] = update.LengthM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["ThicknessM"] = update.ThicknessM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["HeightM"] = update.HeightM.ToString("R", CultureInfo.InvariantCulture);
            }

            if (pending.Count > 0)
            {
                document.Editor.Regen();
                project.Touch();
            }
            return pending.Count;
        }

        private static bool IsSupportedWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall || category == ElementCategory.GlassWall || category == ElementCategory.WallPier;
    }
}
