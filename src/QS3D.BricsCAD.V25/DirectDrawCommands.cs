using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// BLT-style direct authoring entry points. These commands create real source CAD in the
    /// active BricsCAD DWG, capture it into the existing semantic model, then reuse the existing
    /// guarded native builders. Existing capture commands remain fully supported.
    /// </summary>
    public sealed class DirectDrawCommands
    {
        [CommandMethod("QS3DDRAWWALL", CommandFlags.Modal)]
        public void DrawWall()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWWALL", () =>
            {
                var points = AcquirePath(document.Editor, "Tường", minimumPoints: 2, close: false);
                if (points == null) return;
                ExecuteDirect(document, ElementCategory.ArchitecturalWall, () =>
                    points.Count == 2 ? CreateLine(document, points[0], points[1]) : CreatePolyline(document, points, false));
            });
        }

        [CommandMethod("QS3DDRAWBEAM", CommandFlags.Modal)]
        public void DrawBeam()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWBEAM", () =>
            {
                var points = AcquireFixedPath(document.Editor, "Dầm", 2);
                if (points == null) return;
                ExecuteDirect(document, ElementCategory.Beam, () => CreateLine(document, points[0], points[1]));
            });
        }

        [CommandMethod("QS3DDRAWSLAB", CommandFlags.Modal)]
        public void DrawSlab()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWSLAB", () =>
            {
                var points = AcquirePath(document.Editor, "Sàn", minimumPoints: 3, close: true);
                if (points == null) return;
                ExecuteDirect(document, ElementCategory.Slab, () => CreatePolyline(document, points, true));
            });
        }

        [CommandMethod("QS3DDRAWCOLUMN", CommandFlags.Modal)]
        public void DrawColumn()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWCOLUMN", () =>
            {
                var centerResult = document.Editor.GetPoint(new PromptPointOptions("\nChọn tâm Cột: "));
                if (centerResult.Status != PromptStatus.OK) return;

                var project = ProjectContextCoordinator.GetOrCreate(document);
                var widthM = PromptPositiveMeters(document.Editor, "Bề rộng Cột (m)", FamilyNumber(project, ElementCategory.Column, "WidthM", 0.4d));
                if (!widthM.HasValue) return;
                var depthM = PromptPositiveMeters(document.Editor, "Bề sâu Cột (m)", FamilyNumber(project, ElementCategory.Column, "DepthM", 0.4d));
                if (!depthM.HasValue) return;

                ExecuteDirect(
                    document,
                    ElementCategory.Column,
                    () => CreateColumnFootprint(document, centerResult.Value, widthM.Value, depthM.Value),
                    element =>
                    {
                        element.Properties["WidthM"] = widthM.Value.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["DepthM"] = depthM.Value.ToString("R", CultureInfo.InvariantCulture);
                        element.MarkDirty(ElementDirtyFlags.Properties);
                    });
            });
        }

        private static void ExecuteDirect(
            Document document,
            ElementCategory category,
            Func<ObjectId> createSource,
            Action<ProjectElement>? configureElement = null)
        {
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var rollback = ProjectStateSnapshot.Capture(project);
            var priorGenerated = new HashSet<string>(GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project), StringComparer.OrdinalIgnoreCase);
            var sourceId = ObjectId.Null;
            string sourceHandle = string.Empty;

            try
            {
                sourceId = createSource();
                if (sourceId.IsNull || !sourceId.IsValid) throw new InvalidOperationException("Không tạo được CAD source cho Direct Draw.");
                sourceHandle = sourceId.Handle.ToString();
                document.Editor.SetImpliedSelection(new[] { sourceId });

                var captured = SemanticCaptureService.Capture(document, category);
                if (captured != 1) throw new InvalidOperationException("Direct Draw cần capture đúng một semantic element, nhận được " + captured + ".");

                var element = project.Elements.SingleOrDefault(x =>
                    x.Category == category && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)));
                if (element == null) throw new InvalidOperationException("Không tìm thấy semantic element vừa tạo cho source " + sourceHandle + ".");

                configureElement?.Invoke(element);

                // Resolve dependency/rule failures before any native builder commits Solid3d output.
                // This is especially important for Column Direct Draw because instance dimensions are
                // configured after the initial capture and therefore need a deterministic regen pass.
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);

                var solids = BuildSelected(document, project, category);
                if (solids <= 0) throw new InvalidOperationException("Native 3D builder không tạo được solid cho " + category + ".");

                project.Touch();
                PaletteCoordinator.RefreshProject();
                document.Editor.SetImpliedSelection(new[] { sourceId });
                document.Editor.Regen();

                var status = "Direct Draw " + category + ": 1 semantic • " + solids + " solid • regenerate " + regenerated + ".";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
                document.SendStringToExecute("QS3DVIEW3D ", true, false, false);
            }
            catch (Exception operationError)
            {
                var cleanupHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(sourceHandle)) cleanupHandles.Add(sourceHandle);
                foreach (var handle in GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project))
                    if (!priorGenerated.Contains(handle)) cleanupHandles.Add(handle);

                Exception? restoreError = null;
                Exception? cadCleanupError = null;
                try { rollback.Restore(project); }
                catch (Exception ex) { restoreError = ex; }
                try { EraseHandles(document, cleanupHandles); }
                catch (Exception ex) { cadCleanupError = ex; }
                try { document.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); }
                catch { }

                if (restoreError != null || cadCleanupError != null)
                {
                    var errors = new List<Exception> { operationError };
                    if (restoreError != null) errors.Add(restoreError);
                    if (cadCleanupError != null) errors.Add(cadCleanupError);
                    throw new InvalidOperationException("Direct Draw thất bại và rollback không hoàn tất đầy đủ.", new AggregateException(errors));
                }
                throw;
            }
        }

        private static int BuildSelected(Document document, ProjectState project, ElementCategory category)
        {
            if (category == ElementCategory.ArchitecturalWall)
            {
                var count = WallSolidBuilder.BuildSelectedLineWalls(document, project, category);
                return count + PolylineWallSolidBuilder.BuildSelected(document, project, category);
            }
            if (category == ElementCategory.Beam || category == ElementCategory.Slab || category == ElementCategory.Column)
                return StructuralSolidBuilder.BuildSelected(document, project, category);
            throw new InvalidOperationException("Direct Draw P0 chưa hỗ trợ category " + category + ".");
        }

        private static IReadOnlyList<Point3d>? AcquireFixedPath(Editor editor, string label, int count)
        {
            var points = new List<Point3d>(count);
            for (var index = 0; index < count; index++)
            {
                var options = new PromptPointOptions("\n" + label + " - chọn điểm " + (index + 1) + "/" + count + ": ");
                if (points.Count > 0)
                {
                    options.UseBasePoint = true;
                    options.BasePoint = points[points.Count - 1];
                }
                var result = editor.GetPoint(options);
                if (result.Status != PromptStatus.OK) return null;
                if (points.Count > 0 && result.Value.DistanceTo(points[points.Count - 1]) <= 1e-9d)
                    throw new InvalidOperationException(label + " có hai điểm trùng nhau.");
                points.Add(result.Value);
            }
            return points;
        }

        private static IReadOnlyList<Point3d>? AcquirePath(Editor editor, string label, int minimumPoints, bool close)
        {
            var points = new List<Point3d>();
            while (true)
            {
                var prompt = points.Count == 0
                    ? "\n" + label + " - chọn điểm đầu: "
                    : "\n" + label + " - chọn điểm tiếp theo" + (points.Count >= minimumPoints ? " hoặc Enter để kết thúc" : string.Empty) + ": ";
                var options = new PromptPointOptions(prompt) { AllowNone = points.Count >= minimumPoints };
                if (points.Count > 0)
                {
                    options.UseBasePoint = true;
                    options.BasePoint = points[points.Count - 1];
                }
                var result = editor.GetPoint(options);
                if (result.Status == PromptStatus.None && points.Count >= minimumPoints) break;
                if (result.Status != PromptStatus.OK) return null;
                if (points.Count > 0 && result.Value.DistanceTo(points[points.Count - 1]) <= 1e-9d)
                    continue;
                points.Add(result.Value);
            }

            if (close && points.Count >= 3 && points[0].DistanceTo(points[points.Count - 1]) <= 1e-9d)
                points.RemoveAt(points.Count - 1);
            if (points.Count < minimumPoints) return null;
            ValidatePlanView(points, label);
            return points;
        }

        private static void ValidatePlanView(IReadOnlyList<Point3d> points, string label)
        {
            if (points.Count == 0) return;
            var z = points[0].Z;
            for (var index = 1; index < points.Count; index++)
                if (Math.Abs(points[index].Z - z) > 1e-6d)
                    throw new InvalidOperationException(label + " Direct Draw hiện yêu cầu các điểm cùng cao độ plan-view.");
        }

        private static ObjectId CreateLine(Document document, Point3d start, Point3d end)
        {
            if (start.DistanceTo(end) <= 1e-9d) throw new InvalidOperationException("LINE Direct Draw quá ngắn.");
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var line = new Line(start, end);
                line.SetDatabaseDefaults(document.Database);
                var id = modelSpace.AppendEntity(line);
                transaction.AddNewlyCreatedDBObject(line, true);
                transaction.Commit();
                return id;
            }
        }

        private static ObjectId CreatePolyline(Document document, IReadOnlyList<Point3d> points, bool closed)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < (closed ? 3 : 2)) throw new InvalidOperationException("Không đủ điểm để tạo POLYLINE Direct Draw.");
            ValidatePlanView(points, closed ? "Closed POLYLINE" : "Open POLYLINE");

            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var polyline = new Polyline();
                polyline.SetDatabaseDefaults(document.Database);
                polyline.Elevation = points[0].Z;
                for (var index = 0; index < points.Count; index++)
                    polyline.AddVertexAt(index, new Point2d(points[index].X, points[index].Y), 0d, 0d, 0d);
                polyline.Closed = closed;
                var id = modelSpace.AppendEntity(polyline);
                transaction.AddNewlyCreatedDBObject(polyline, true);
                transaction.Commit();
                return id;
            }
        }

        private static ObjectId CreateColumnFootprint(Document document, Point3d center, double widthM, double depthM)
        {
            var width = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, widthM, "DirectDraw Column WidthM"), "DirectDraw Column width drawing units");
            var depth = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, depthM, "DirectDraw Column DepthM"), "DirectDraw Column depth drawing units");
            var halfWidth = width / 2d;
            var halfDepth = depth / 2d;
            return CreatePolyline(document, new[]
            {
                new Point3d(center.X - halfWidth, center.Y - halfDepth, center.Z),
                new Point3d(center.X + halfWidth, center.Y - halfDepth, center.Z),
                new Point3d(center.X + halfWidth, center.Y + halfDepth, center.Z),
                new Point3d(center.X - halfWidth, center.Y + halfDepth, center.Z)
            }, true);
        }

        private static double? PromptPositiveMeters(Editor editor, string label, double defaultValue)
        {
            var options = new PromptDoubleOptions("\n" + label + " <" + defaultValue.ToString("0.###", CultureInfo.InvariantCulture) + ">: ")
            {
                AllowNegative = false,
                AllowZero = false,
                AllowNone = true,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };
            var result = editor.GetDouble(options);
            if (result.Status == PromptStatus.Cancel) return null;
            if (result.Status != PromptStatus.OK && result.Status != PromptStatus.None) return null;
            var value = result.Status == PromptStatus.OK ? result.Value : defaultValue;
            if (double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d)) throw new InvalidOperationException(label + " phải là số hữu hạn > 0.");
            return value;
        }

        private static double FamilyNumber(ProjectState project, ElementCategory category, string key, double fallback)
        {
            ProjectFamily? family = null;
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId))
            {
                var active = project.FindFamily(activeId);
                if (active != null && active.Category == category) family = active;
            }
            family = family ?? project.Families.FirstOrDefault(x => x.Category == category);
            if (family == null || !family.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d)) return fallback;
            return value;
        }

        private static void EraseHandles(Document document, IEnumerable<string> handles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (handles == null) throw new ArgumentNullException(nameof(handles));

            var normalized = new HashSet<string>(
                handles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            if (normalized.Count == 0) return;

            var ids = CadHandleService.Resolve(document, normalized);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                foreach (var id in ids)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                    if (entity == null)
                        throw new InvalidOperationException("Direct Draw rollback handle " + id.Handle + " không còn trỏ tới Entity hợp lệ.");
                    if (!entity.IsErased) entity.Erase(true);
                }
                transaction.Commit();
            }

            var remaining = CadHandleService.GetLiveHandles(document, normalized);
            if (remaining.Count > 0)
                throw new InvalidOperationException("Direct Draw rollback còn CAD handle chưa xóa: " + string.Join(", ", remaining.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) + ".");
            document.Editor.Regen();
        }

        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;

        private static void Guard(Document document, string operation, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message);
                PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message);
            }
        }
    }
}
