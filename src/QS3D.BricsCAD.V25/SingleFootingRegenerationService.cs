using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Rebuilds the native geometry owned by one Móng đơn Family after a six-dimension edit.
    /// Project metadata and all owned Solid3d replacements are one fail-closed operation: any
    /// native failure aborts the CAD transaction and restores the semantic project snapshot.
    /// </summary>
    internal static class SingleFootingRegenerationService
    {
        public static int ApplyFamilyDimensions(
            Document document,
            ProjectState project,
            ProjectFamily family,
            SingleFootingDimensions dimensions)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (family == null) throw new ArgumentNullException(nameof(family));
            if (dimensions == null) throw new ArgumentNullException(nameof(dimensions));
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("DWG active đã thay đổi trước khi regenerate Móng đơn.");
            if (!document.Database.TileMode)
                throw new InvalidOperationException("Regenerate Móng đơn chỉ chạy trong Model Space.");
            if (!SingleFootingContract.IsSingleFooting(family))
                throw new InvalidOperationException("Family được chọn không phải Móng đơn.");
            if (!ReferenceEquals(project.FindFamily(family.Id), family))
                throw new InvalidOperationException("Family Móng đơn đã stale hoặc không thuộc project hiện hành.");

            var targets = project.Elements
                .Where(element =>
                    element.Category == ElementCategory.Foundation &&
                    string.Equals(element.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase) &&
                    SingleFootingContract.IsSingleFooting(element))
                .ToList();
            var rollback = ProjectStateSnapshot.Capture(project);

            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                    var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    SingleFootingContract.Apply(family, dimensions);

                    foreach (var element in targets)
                    {
                        var source = ResolveUniqueFootprint(document, transaction, element);
                        var center = ReadCenter(source);
                        var baseElevationM = ReadBaseElevation(element);
                        var baseElevationDrawing = CadUnitService.MetersToDrawingUnits(document, baseElevationM);
                        var l1 = CadUnitService.MetersToDrawingUnits(document, dimensions.L1M);
                        var w1 = CadUnitService.MetersToDrawingUnits(document, dimensions.W1M);

                        ResizeFootprint(source, center.X, center.Y, baseElevationDrawing, l1, w1);

                        var solid = BuildSolid(document, dimensions, center.X, center.Y, baseElevationDrawing);
                        try
                        {
                            solid.Layer = source.Layer;
                            var previousHandle = GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                            modelSpace.AppendEntity(solid);
                            transaction.AddNewlyCreatedDBObject(solid, true);
                            GeneratedGeometryService.MarkGenerated(
                                document,
                                transaction,
                                solid,
                                project.ProjectId,
                                element.Id,
                                ElementCategory.Foundation);

                            var generatedHandle = solid.Handle.ToString();
                            SingleFootingContract.Apply(element, dimensions);
                            element.Properties[SingleFootingContract.BaseElevationKey] =
                                baseElevationM.ToString("R", CultureInfo.InvariantCulture);
                            GeneratedGeometryService.CommitReplacement(
                                project,
                                element,
                                previousHandle,
                                generatedHandle,
                                ElementCategory.Foundation);
                            element.Properties["GeneratedSolidMode"] = SingleFootingContract.GeneratedMode;
                            element.Properties[SingleFootingContract.VolumeKey] =
                                dimensions.VolumeM3.ToString("R", CultureInfo.InvariantCulture);
                            element.Properties["VolumeM3"] = dimensions.VolumeM3.ToString("R", CultureInfo.InvariantCulture);
                            element.MarkClean(ElementGeometryPolicy.SemanticCleanFlags(ElementCategory.Foundation));
                            AuditTrail.ForProject(project).Record(
                                "geometry.single-footing.regenerate",
                                element.Id,
                                previousHandle + " -> " + generatedHandle + " • " + family.Name);
                            solid = null!;
                        }
                        finally
                        {
                            solid?.Dispose();
                        }
                    }

                    project.Touch();
                    AuditTrail.ForProject(project).Record(
                        "geometry.single-footing.regenerate-family",
                        string.Empty,
                        family.Id + " • " + family.Name + " • instances=" + targets.Count.ToString(CultureInfo.InvariantCulture));
                    transaction.Commit();
                }
            }
            catch (Exception operationError)
            {
                try
                {
                    rollback.Restore(project);
                }
                catch (Exception restoreError)
                {
                    throw new InvalidOperationException(
                        "Regenerate Móng đơn thất bại và rollback project cũng không hoàn tất.",
                        new AggregateException(operationError, restoreError));
                }
                throw;
            }

            try { CadPostCommitUi.TryRegen(document, "Móng đơn"); } catch { }
            return targets.Count;
        }

        private static Polyline ResolveUniqueFootprint(Document document, Transaction transaction, ProjectElement element)
        {
            Polyline? match = null;
            foreach (var sourceHandle in element.SourceHandles)
            {
                if (string.IsNullOrWhiteSpace(sourceHandle) ||
                    !long.TryParse(sourceHandle.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;

                ObjectId id;
                try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                catch { continue; }
                if (id.IsNull || !id.IsValid) continue;

                var candidate = transaction.GetObject(id, OpenMode.ForWrite, true) as Polyline;
                if (candidate == null || candidate.IsErased || !candidate.Closed) continue;
                if (candidate.NumberOfVertices != 4)
                    throw new InvalidOperationException("Footprint Móng đơn " + sourceHandle + " không còn là rectangle 4 đỉnh.");
                for (var index = 0; index < candidate.NumberOfVertices; index++)
                    if (Math.Abs(candidate.GetBulgeAt(index)) > 1e-12d)
                        throw new InvalidOperationException("Footprint Móng đơn " + sourceHandle + " có cung/bulge; từ chối regenerate mơ hồ.");
                if (match != null)
                    throw new InvalidOperationException("Móng đơn " + element.Id + " có nhiều footprint nguồn hợp lệ; từ chối regenerate mơ hồ.");
                match = candidate;
            }

            if (match == null)
                throw new InvalidOperationException("Không tìm thấy footprint nguồn còn sống cho Móng đơn " + element.Id + ".");
            return match;
        }

        private static Point2d ReadCenter(Polyline source)
        {
            var x = 0d;
            var y = 0d;
            for (var index = 0; index < 4; index++)
            {
                var point = source.GetPoint2dAt(index);
                x += point.X;
                y += point.Y;
            }
            return new Point2d(x / 4d, y / 4d);
        }

        private static double ReadBaseElevation(ProjectElement element)
        {
            if (!element.Properties.TryGetValue(SingleFootingContract.BaseElevationKey, out var raw) ||
                string.IsNullOrWhiteSpace(raw) ||
                !double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Móng đơn " + element.Id + " thiếu " + SingleFootingContract.BaseElevationKey + " hợp lệ.");
            return value;
        }

        private static void ResizeFootprint(
            Polyline source,
            double centerX,
            double centerY,
            double elevation,
            double length,
            double width)
        {
            source.Elevation = elevation;
            source.SetPointAt(0, new Point2d(centerX - length / 2d, centerY - width / 2d));
            source.SetPointAt(1, new Point2d(centerX + length / 2d, centerY - width / 2d));
            source.SetPointAt(2, new Point2d(centerX + length / 2d, centerY + width / 2d));
            source.SetPointAt(3, new Point2d(centerX - length / 2d, centerY + width / 2d));
        }

        private static Solid3d BuildSolid(
            Document document,
            SingleFootingDimensions dimensions,
            double centerX,
            double centerY,
            double baseZ)
        {
            var l1 = CadUnitService.MetersToDrawingUnits(document, dimensions.L1M);
            var w1 = CadUnitService.MetersToDrawingUnits(document, dimensions.W1M);
            var l2 = CadUnitService.MetersToDrawingUnits(document, dimensions.L2M);
            var w2 = CadUnitService.MetersToDrawingUnits(document, dimensions.W2M);
            var h1 = CadUnitService.MetersToDrawingUnits(document, dimensions.H1M);
            var h2 = CadUnitService.MetersToDrawingUnits(document, dimensions.H2M);

            var lower = new Solid3d();
            try
            {
                lower.SetDatabaseDefaults(document.Database);
                lower.CreateBox(l1, w1, h1);
                lower.TransformBy(Matrix3d.Displacement(new Vector3d(centerX, centerY, baseZ + h1 / 2d)));
                if (!(h2 > 0d))
                {
                    var completed = lower;
                    lower = null!;
                    return completed;
                }

                Solid3d upper;
                if (Math.Abs(l1 - l2) <= 1e-10d && Math.Abs(w1 - w2) <= 1e-10d)
                {
                    upper = new Solid3d();
                    upper.SetDatabaseDefaults(document.Database);
                    upper.CreateBox(l1, w1, h2);
                    upper.TransformBy(Matrix3d.Displacement(new Vector3d(centerX, centerY, baseZ + h1 + h2 / 2d)));
                }
                else
                {
                    upper = CreateTaperedLoft(
                        document,
                        centerX,
                        centerY,
                        baseZ + h1,
                        l1,
                        w1,
                        baseZ + h1 + h2,
                        l2,
                        w2);
                }

                using (upper)
                    lower.BooleanOperation(BooleanOperationType.BoolUnite, upper);

                var result = lower;
                lower = null!;
                return result;
            }
            finally
            {
                lower?.Dispose();
            }
        }

        private static Solid3d CreateTaperedLoft(
            Document document,
            double centerX,
            double centerY,
            double bottomZ,
            double bottomLength,
            double bottomWidth,
            double topZ,
            double topLength,
            double topWidth)
        {
            using (var bottom = CreateProfile(centerX, centerY, bottomZ, bottomLength, bottomWidth))
            using (var top = CreateProfile(centerX, centerY, topZ, topLength, topWidth))
            using (var options = new LoftOptions())
            {
                var solid = new Solid3d();
                try
                {
                    solid.SetDatabaseDefaults(document.Database);
                    solid.CreateLoftedSolid(
                        new Entity[] { bottom, top },
                        Array.Empty<Entity>(),
                        null,
                        options);
                    return solid;
                }
                catch
                {
                    solid.Dispose();
                    throw;
                }
            }
        }

        private static Polyline CreateProfile(
            double centerX,
            double centerY,
            double elevation,
            double length,
            double width)
        {
            var profile = new Polyline(4) { Closed = true, Elevation = elevation };
            profile.AddVertexAt(0, new Point2d(centerX - length / 2d, centerY - width / 2d), 0d, 0d, 0d);
            profile.AddVertexAt(1, new Point2d(centerX + length / 2d, centerY - width / 2d), 0d, 0d, 0d);
            profile.AddVertexAt(2, new Point2d(centerX + length / 2d, centerY + width / 2d), 0d, 0d, 0d);
            profile.AddVertexAt(3, new Point2d(centerX - length / 2d, centerY + width / 2d), 0d, 0d, 0d);
            return profile;
        }
    }
}
