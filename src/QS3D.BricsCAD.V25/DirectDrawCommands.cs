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
using QS3D.Core.Persistence;
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
        private const double PlanarityToleranceM = 0.005d;
        private const double UcsAxisTolerance = 1e-9d;

        [CommandMethod("QS3DDRAWWALL", CommandFlags.Modal)]
        public void DrawWall()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWWALL", () =>
            {
                RequireModelSpace(document);
                var points = AcquireFixedPath(document, "Tường nhanh", 2);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "ThicknessM", 0.2d) : 0.2d;
                var heightM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "HeightM", 3.6d) : 3.6d;
                var bottomOffsetM = hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "BottomOffsetM", 0d) : 0d;

                document.Editor.WriteMessage(
                    "\nQS3D Tường nhanh: dùng Family hiện tại (dày " + thicknessM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, cao " + heightM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, offset " + bottomOffsetM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m). Dùng QS3DDRAWWALLADV khi cần vẽ chuỗi hoặc nhập tham số riêng.");

                ExecuteDirect(
                    document,
                    ElementCategory.ArchitecturalWall,
                    () => CreateLine(document, points[0], points[1]),
                    element =>
                    {
                        element.SetProperty("ThicknessM", thicknessM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("HeightM", heightM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        [CommandMethod("QS3DDRAWWALLADV", CommandFlags.Modal)]
        public void DrawWallAdvanced()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWWALLADV", () =>
            {
                RequireModelSpace(document);
                var points = AcquirePath(document, "Tường tùy chỉnh", minimumPoints: 2, close: false);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = PromptPositiveMeters(document.Editor, "Bề dày Tường (m)", hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "ThicknessM", 0.2d) : 0.2d);
                if (!thicknessM.HasValue) return;
                var heightM = PromptPositiveMeters(document.Editor, "Chiều cao Tường (m)", hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "HeightM", 3.6d) : 3.6d);
                if (!heightM.HasValue) return;
                var bottomOffsetM = PromptFiniteMeters(document.Editor, "Offset đáy Tường so với Z source (m)", hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "BottomOffsetM", 0d) : 0d);
                if (!bottomOffsetM.HasValue) return;

                ExecuteDirect(
                    document,
                    ElementCategory.ArchitecturalWall,
                    () => points.Count == 2 ? CreateLine(document, points[0], points[1]) : CreatePolyline(document, points, false),
                    element =>
                    {
                        element.SetProperty("ThicknessM", thicknessM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("HeightM", heightM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.Value.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        [CommandMethod("QS3DDRAWBEAM", CommandFlags.Modal)]
        public void DrawBeam()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWBEAM", () =>
            {
                RequireModelSpace(document);
                var points = AcquireFixedPath(document, "Dầm nhanh", 2);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var widthM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Beam, "WidthM", 0.3d) : 0.3d;
                var heightM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Beam, "HeightM", 0.5d) : 0.5d;
                var bottomOffsetM = hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.Beam, "BottomOffsetM", 0d) : 0d;

                document.Editor.WriteMessage(
                    "\nQS3D Dầm nhanh: dùng Family hiện tại (rộng " + widthM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, cao " + heightM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, offset " + bottomOffsetM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m). Dùng QS3DDRAWBEAMADV khi cần nhập tham số riêng.");

                ExecuteDirect(
                    document,
                    ElementCategory.Beam,
                    () => CreateLine(document, points[0], points[1]),
                    element =>
                    {
                        element.SetProperty("WidthM", widthM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("HeightM", heightM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        [CommandMethod("QS3DDRAWBEAMADV", CommandFlags.Modal)]
        public void DrawBeamAdvanced()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWBEAMADV", () =>
            {
                RequireModelSpace(document);
                var points = AcquireFixedPath(document, "Dầm tùy chỉnh", 2);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var widthM = PromptPositiveMeters(document.Editor, "Bề rộng Dầm (m)", hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Beam, "WidthM", 0.3d) : 0.3d);
                if (!widthM.HasValue) return;
                var heightM = PromptPositiveMeters(document.Editor, "Chiều cao Dầm (m)", hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Beam, "HeightM", 0.5d) : 0.5d);
                if (!heightM.HasValue) return;
                var bottomOffsetM = PromptFiniteMeters(document.Editor, "Offset đáy Dầm so với Z source (m)", hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.Beam, "BottomOffsetM", 0d) : 0d);
                if (!bottomOffsetM.HasValue) return;

                ExecuteDirect(
                    document,
                    ElementCategory.Beam,
                    () => CreateLine(document, points[0], points[1]),
                    element =>
                    {
                        element.SetProperty("WidthM", widthM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("HeightM", heightM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.Value.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        [CommandMethod("QS3DDRAWSLAB", CommandFlags.Modal)]
        public void DrawSlab()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWSLAB", () =>
            {
                RequireModelSpace(document);
                var points = AcquirePath(document, "Sàn nhanh", minimumPoints: 3, close: true);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Slab, "ThicknessM", 0.12d) : 0.12d;
                var bottomOffsetM = hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.Slab, "BottomOffsetM", 0d) : 0d;

                document.Editor.WriteMessage(
                    "\nQS3D Sàn nhanh: dùng Family hiện tại (dày " + thicknessM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, offset " + bottomOffsetM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m). Dùng QS3DDRAWSLABADV khi cần nhập tham số riêng.");

                ExecuteDirect(
                    document,
                    ElementCategory.Slab,
                    () => CreatePolyline(document, points, true),
                    element =>
                    {
                        element.SetProperty("ThicknessM", thicknessM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        [CommandMethod("QS3DDRAWSLABADV", CommandFlags.Modal)]
        public void DrawSlabAdvanced()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWSLABADV", () =>
            {
                RequireModelSpace(document);
                var points = AcquirePath(document, "Sàn tùy chỉnh", minimumPoints: 3, close: true);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = PromptPositiveMeters(document.Editor, "Bề dày Sàn (m)", hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Slab, "ThicknessM", 0.12d) : 0.12d);
                if (!thicknessM.HasValue) return;
                var bottomOffsetM = PromptFiniteMeters(document.Editor, "Offset đáy Sàn so với Z source (m)", hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.Slab, "BottomOffsetM", 0d) : 0d);
                if (!bottomOffsetM.HasValue) return;

                ExecuteDirect(
                    document,
                    ElementCategory.Slab,
                    () => CreatePolyline(document, points, true),
                    element =>
                    {
                        element.SetProperty("ThicknessM", thicknessM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.Value.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        [CommandMethod("QS3DDRAWCOLUMN", CommandFlags.Modal)]
        public void DrawColumn()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWCOLUMN", () =>
            {
                RequireModelSpace(document);
                var promptUnit = (object)CadUnitService.GetLengthUnit(document);
                var promptUcs = document.Editor.CurrentUserCoordinateSystem;
                var centerResult = document.Editor.GetPoint(new PromptPointOptions("\nChọn tâm Cột nhanh: "));
                if (centerResult.Status != PromptStatus.OK) return;
                RequirePromptContextUnchanged(document, promptUnit, promptUcs, "QS3DDRAWCOLUMN");

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var widthM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Column, "WidthM", 0.4d) : 0.4d;
                var depthM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Column, "DepthM", 0.4d) : 0.4d;
                var heightM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Column, "HeightM", 3.6d) : 3.6d;
                var bottomOffsetM = hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.Column, "BottomOffsetM", 0d) : 0d;

                document.Editor.WriteMessage(
                    "\nQS3D Cột nhanh: dùng Family hiện tại (" + widthM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " x " + depthM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, cao " + heightM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, offset " + bottomOffsetM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m). Dùng QS3DDRAWCOLUMNADV khi cần nhập tham số riêng.");

                ExecuteDirect(
                    document,
                    ElementCategory.Column,
                    () => CreateColumnFootprint(document, centerResult.Value, widthM, depthM),
                    element =>
                    {
                        element.SetProperty("WidthM", widthM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("DepthM", depthM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("HeightM", heightM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        [CommandMethod("QS3DDRAWCOLUMNADV", CommandFlags.Modal)]
        public void DrawColumnAdvanced()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWCOLUMNADV", () =>
            {
                RequireModelSpace(document);
                var promptUnit = (object)CadUnitService.GetLengthUnit(document);
                var promptUcs = document.Editor.CurrentUserCoordinateSystem;
                var centerResult = document.Editor.GetPoint(new PromptPointOptions("\nChọn tâm Cột tùy chỉnh: "));
                if (centerResult.Status != PromptStatus.OK) return;
                RequirePromptContextUnchanged(document, promptUnit, promptUcs, "QS3DDRAWCOLUMNADV");

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var widthM = PromptPositiveMeters(document.Editor, "Bề rộng Cột (m)", hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Column, "WidthM", 0.4d) : 0.4d);
                if (!widthM.HasValue) return;
                var depthM = PromptPositiveMeters(document.Editor, "Bề sâu Cột (m)", hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Column, "DepthM", 0.4d) : 0.4d);
                if (!depthM.HasValue) return;
                var heightM = PromptPositiveMeters(document.Editor, "Chiều cao Cột (m)", hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Column, "HeightM", 3.6d) : 3.6d);
                if (!heightM.HasValue) return;
                var bottomOffsetM = PromptFiniteMeters(document.Editor, "Offset đáy Cột so với Z source (m)", hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.Column, "BottomOffsetM", 0d) : 0d);
                if (!bottomOffsetM.HasValue) return;

                ExecuteDirect(
                    document,
                    ElementCategory.Column,
                    () => CreateColumnFootprint(document, centerResult.Value, widthM.Value, depthM.Value),
                    element =>
                    {
                        element.SetProperty("WidthM", widthM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("DepthM", depthM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("HeightM", heightM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.Value.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        private static void ExecuteDirect(
            Document document,
            ElementCategory category,
            Func<ObjectId> createSource,
            Action<ProjectElement>? configureElement = null,
            DirectDrawProjectPreviewContext? projectPreview = null)
        {
            var operation = "Direct Draw " + category;
            EnsureActive(document, operation);
            var projectExistedBeforeAuthoring = projectPreview != null
                ? projectPreview.HasProject
                : ProjectContextCoordinator.TryGetReadOnly(document, out _);
            var project = projectPreview != null
                ? projectPreview.ResolveForMutation(document, operation)
                : ProjectContextCoordinator.GetOrCreate(document);
            var rollback = ProjectStateSnapshot.Capture(project);
            var sourceId = ObjectId.Null;
            var sourceHandle = string.Empty;
            ProjectElement? createdElement = null;
            var regenerated = 0;
            var solids = 0;

            try
            {
                sourceId = createSource();
                if (sourceId.IsNull || !sourceId.IsValid) throw new InvalidOperationException("Không tạo được CAD source cho Direct Draw.");
                sourceHandle = sourceId.Handle.ToString();
                document.Editor.SetImpliedSelection(new[] { sourceId });

                var captured = SemanticCaptureService.Capture(document, category);
                if (captured != 1) throw new InvalidOperationException("Direct Draw cần capture đúng một semantic element, nhận được " + captured + ".");

                createdElement = project.Elements.SingleOrDefault(x =>
                    x.Category == category && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)));
                if (createdElement == null) throw new InvalidOperationException("Không tìm thấy semantic element vừa tạo cho source " + sourceHandle + ".");

                configureElement?.Invoke(createdElement);

                regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault())
                    .RegenerateDirtySubset(project, new[] { createdElement.Id });

                solids = BuildSelected(document, project, category);
                if (solids <= 0) throw new InvalidOperationException("Native 3D builder không tạo được solid cho " + category + ".");

                project.Touch();
            }
            catch (Exception operationError)
            {
                var generatedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Exception? ownershipDiscoveryError = null;
                if (createdElement != null)
                {
                    foreach (var entry in GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(createdElement))
                        if (!string.IsNullOrWhiteSpace(entry.Key)) generatedHandles.Add(entry.Key.Trim());
                    try
                    {
                        foreach (var handle in GeneratedGeometryService.FindMatchingOwnedHandles(document, project.ProjectId, createdElement.Id, createdElement.Category))
                            if (!string.IsNullOrWhiteSpace(handle)) generatedHandles.Add(handle.Trim());
                    }
                    catch (Exception ex)
                    {
                        ownershipDiscoveryError = ex;
                    }
                }

                Exception? cadCleanupError = null;
                Exception? restoreError = null;
                try { EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles); }
                catch (Exception ex) { cadCleanupError = ex; }
                try { rollback.Restore(project); }
                catch (Exception ex) { restoreError = ex; }
                if (!projectExistedBeforeAuthoring) ProjectContextCoordinator.Forget(document);
                try { document.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); }
                catch { }

                if (ownershipDiscoveryError != null || cadCleanupError != null || restoreError != null)
                {
                    var errors = new List<Exception> { operationError };
                    if (ownershipDiscoveryError != null) errors.Add(ownershipDiscoveryError);
                    if (cadCleanupError != null) errors.Add(cadCleanupError);
                    if (restoreError != null) errors.Add(restoreError);
                    throw new InvalidOperationException("Direct Draw thất bại và rollback không hoàn tất đầy đủ.", new AggregateException(errors));
                }
                throw;
            }

            FinalizeUi(document, createdElement!, sourceId, solids, regenerated);
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

        private static IReadOnlyList<Point3d>? AcquireFixedPath(Document document, string label, int count)
        {
            var editor = document.Editor;
            var promptUnit = (object)CadUnitService.GetLengthUnit(document);
            var promptUcs = editor.CurrentUserCoordinateSystem;
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
            RequirePromptContextUnchanged(document, promptUnit, promptUcs, label);
            ValidatePlanView(document, points, label);
            return points;
        }

        private static IReadOnlyList<Point3d>? AcquirePath(Document document, string label, int minimumPoints, bool close)
        {
            var editor = document.Editor;
            var promptUnit = (object)CadUnitService.GetLengthUnit(document);
            var promptUcs = editor.CurrentUserCoordinateSystem;
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
            RequirePromptContextUnchanged(document, promptUnit, promptUcs, label);
            ValidatePlanView(document, points, label);
            return points;
        }

        private static void ValidatePlanView(Document document, IReadOnlyList<Point3d> points, string label)
        {
            if (points.Count == 0) return;
            var z = CadGeometryGuard.Finite(points[0].Z, label + "/base Z");
            for (var index = 1; index < points.Count; index++)
            {
                var deltaDrawingUnits = Math.Abs(CadGeometryGuard.Subtract(points[index].Z, z, label + "/delta Z"));
                var deltaM = Math.Abs(CadGeometryGuard.ToMeters(document, deltaDrawingUnits, label + "/delta Z"));
                if (deltaM > PlanarityToleranceM)
                    throw new InvalidOperationException(label + " Direct Draw yêu cầu plan-view |ΔZ| <= 0.005 m.");
            }
        }

        private static ObjectId CreateLine(Document document, Point3d start, Point3d end)
        {
            ValidatePlanView(document, new[] { start, end }, "LINE");
            if (start.DistanceTo(end) <= 1e-9d) throw new InvalidOperationException("LINE Direct Draw quá ngắn.");
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var line = new Line(start, end);
                line.SetDatabaseDefaults(document.Database);
                line.TransformBy(document.Editor.CurrentUserCoordinateSystem);
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
            ValidatePlanView(document, points, closed ? "Closed POLYLINE" : "Open POLYLINE");

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
                polyline.TransformBy(document.Editor.CurrentUserCoordinateSystem);
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
            var left = CadGeometryGuard.Subtract(center.X, halfWidth, "DirectDraw Column left X");
            var right = CadGeometryGuard.Add(center.X, halfWidth, "DirectDraw Column right X");
            var bottom = CadGeometryGuard.Subtract(center.Y, halfDepth, "DirectDraw Column bottom Y");
            var top = CadGeometryGuard.Add(center.Y, halfDepth, "DirectDraw Column top Y");
            var z = CadGeometryGuard.Finite(center.Z, "DirectDraw Column Z");
            return CreatePolyline(document, new[]
            {
                new Point3d(left, bottom, z),
                new Point3d(right, bottom, z),
                new Point3d(right, top, z),
                new Point3d(left, top, z)
            }, true);
        }

        private static double? PromptPositiveMeters(Editor editor, string label, double defaultValue)
        {
            var options = new PromptDoubleOptions("\n" + label + ": ")
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

        private static double? PromptFiniteMeters(Editor editor, string label, double defaultValue)
        {
            var options = new PromptDoubleOptions("\n" + label + ": ")
            {
                AllowNegative = true,
                AllowZero = true,
                AllowNone = true,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };
            var result = editor.GetDouble(options);
            if (result.Status == PromptStatus.Cancel) return null;
            if (result.Status != PromptStatus.OK && result.Status != PromptStatus.None) return null;
            var value = result.Status == PromptStatus.OK ? result.Value : defaultValue;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException(label + " phải là số hữu hạn.");
            return value;
        }

        private static double FamilyNumber(ProjectState project, ElementCategory category, string key, double fallback)
        {
            var value = FamilyFiniteNumber(project, category, key, fallback);
            if (!(value > 0d))
                throw new InvalidOperationException("Family " + category + "/" + key + " phải là số hữu hạn > 0 trước khi Direct Draw.");
            return value;
        }

        private static double FamilyFiniteNumber(ProjectState project, ElementCategory category, string key, double fallback)
        {
            var family = PreferredFamily(project, category);
            if (family == null || !family.Properties.TryGetValue(key, out var raw)) return fallback;
            if (string.IsNullOrWhiteSpace(raw) ||
                !double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Family '" + family.Name + "' (" + category + ") có " + key + " không hợp lệ: '" + (raw ?? string.Empty) + "'. Sửa Family trước khi Direct Draw.");
            return value;
        }

        private static ProjectFamily? PreferredFamily(ProjectState project, ElementCategory category)
        {
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId))
            {
                var active = project.FindFamily(activeId);
                if (active != null && active.Category == category) return active;
            }
            return project.Families.FirstOrDefault(x => x.Category == category);
        }

        private static void RequirePromptContextUnchanged(Document document, object promptUnit, Matrix3d promptUcs, string operation)
        {
            EnsureActive(document, operation + " / geometry prompt freshness");
            RequireModelSpace(document);
            if (!Equals(CadUnitService.GetLengthUnit(document), promptUnit))
                throw new InvalidOperationException("Drawing unit policy đã thay đổi trong lúc chọn geometry cho " + operation + ". Hãy chạy lại lệnh.");
            if (!document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs))
                throw new InvalidOperationException("Current UCS đã thay đổi trong lúc chọn geometry cho " + operation + ". Hãy chạy lại lệnh.");
        }

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                if (!document.Database.CurrentSpaceId.Equals(modelSpaceId))
                    throw new InvalidOperationException("Direct Draw P0 hiện chỉ hỗ trợ Model Space. Chuyển sang tab Model trước khi vẽ.");
                transaction.Commit();
            }
            RequireSupportedUcs(document);
        }

        private static void RequireSupportedUcs(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var coordinateSystem = document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
            var zAxis = coordinateSystem.Zaxis;
            var length = zAxis.Length;
            if (double.IsNaN(length) || double.IsInfinity(length) || !(length > 0d))
                throw new InvalidOperationException("Current UCS có Z axis không hợp lệ.");

            var x = zAxis.X / length;
            var y = zAxis.Y / length;
            var z = zAxis.Z / length;
            if (Math.Abs(x) > UcsAxisTolerance || Math.Abs(y) > UcsAxisTolerance || Math.Abs(z - 1d) > UcsAxisTolerance)
                throw new InvalidOperationException("Direct Draw P0 hiện chỉ hỗ trợ UCS có mặt phẳng XY song song WCS XY (có thể xoay/di chuyển trong mặt phẳng). UCS nghiêng/3D chưa được hỗ trợ.");
        }

        private static void EraseDirectDrawCad(Document document, ProjectState project, ProjectElement? createdElement, ObjectId sourceId, IEnumerable<string> generatedHandles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = new HashSet<string>(
                (generatedHandles ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            if (normalized.Count > 0 && createdElement == null)
                throw new InvalidOperationException("Direct Draw rollback found generated CAD without the newly-created semantic owner.");
            var ids = CadHandleService.Resolve(document, normalized);

            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                if (!sourceId.IsNull && sourceId.IsValid)
                {
                    var source = transaction.GetObject(sourceId, OpenMode.ForWrite, true) as Entity;
                    if (source != null && !source.IsErased) source.Erase(true);
                }

                foreach (var id in ids)
                {
                    if (id.IsNull || !id.IsValid || id == sourceId) continue;
                    var entity = transaction.GetObject(id, OpenMode.ForWrite, true) as Entity;
                    if (entity == null) throw new InvalidOperationException("Direct Draw rollback generated handle " + id.Handle + " không còn trỏ tới Entity hợp lệ.");
                    if (entity.IsErased) continue;
                    GeneratedGeometryService.RequireMatchingOwnership(entity, project, createdElement!, "rollback Direct Draw generated CAD " + id.Handle);
                    entity.Erase(true);
                }
                transaction.Commit();
            }

            var remainingGenerated = CadHandleService.GetLiveHandles(document, normalized);
            if (remainingGenerated.Count > 0)
                throw new InvalidOperationException("Direct Draw rollback còn generated CAD handle chưa xóa: " + string.Join(", ", remainingGenerated.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) + ".");
            if (!sourceId.IsNull && sourceId.IsValid)
            {
                var remainingSource = CadHandleService.GetLiveHandles(document, new[] { sourceId.Handle.ToString() });
                if (remainingSource.Count > 0)
                    throw new InvalidOperationException("Direct Draw rollback còn source CAD chưa xóa: " + sourceId.Handle + ".");
            }
        }

        private static void FinalizeUi(Document document, ProjectElement element, ObjectId sourceId, int solids, int regenerated)
        {
            var status = "Direct Draw " + element.Category + ": 1 semantic • " + solids + " solid • regenerate " + regenerated + ".";
            try
            {
                PaletteCoordinator.RefreshProject();
                var generatedHandle = element.Properties.TryGetValue("GeneratedSolidHandle", out var generated) ? generated : string.Empty;
                if (!string.IsNullOrWhiteSpace(generatedHandle)) CadHandleService.Select(document, new[] { generatedHandle });
                else if (!sourceId.IsNull && sourceId.IsValid) document.Editor.SetImpliedSelection(new[] { sourceId });
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
                document.SendStringToExecute("QS3DVIEW3D ", true, false, false);
            }
            catch (Exception ex)
            {
                try { document.Editor.WriteMessage("\nQS3D " + status + " UI sync warning: " + ex.Message); }
                catch { }
            }
        }

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " yêu cầu đúng DWG đã bắt đầu lệnh vẫn là bản vẽ active.");
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