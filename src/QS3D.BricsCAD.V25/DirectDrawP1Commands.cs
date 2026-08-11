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
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Guarded Direct Draw P1 commands for categories already supported by the existing
    /// semantic capture and QS3DBUILD3D compatibility path. BricsCAD remains the CAD host;
    /// these commands create real DWG source geometry rather than a parallel QS3D CAD model.
    /// </summary>
    public sealed class DirectDrawP1Commands
    {
        private const double PlanarityToleranceM = 0.005d;
        private const double UcsAxisTolerance = 1e-9d;

        [CommandMethod("QS3DDRAWGLASSWALL", CommandFlags.Modal)]
        public void DrawGlassWall()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWGLASSWALL", () =>
            {
                RequireModelSpace(document);
                var points = AcquirePath(document, "Vách Kính nhanh", 2, false);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.GlassWall, "ThicknessM", 0.012d) : 0.012d;
                var heightM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.GlassWall, "HeightM", 3.6d) : 3.6d;
                var bottomOffsetM = hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.GlassWall, "BottomOffsetM", 0d) : 0d;

                document.Editor.WriteMessage(
                    "\nQS3D Vách Kính nhanh: dùng Family hiện tại (dày " + thicknessM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, cao " + heightM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, offset " + bottomOffsetM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m). Dùng QS3DDRAWGLASSWALLADV khi cần nhập tham số riêng.");

                Execute(
                    document,
                    ElementCategory.GlassWall,
                    () => points.Count == 2 ? CreateLine(document, points[0], points[1]) : CreatePolyline(document, points, false),
                    element =>
                    {
                        element.SetProperty("ThicknessM", thicknessM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("HeightM", heightM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        [CommandMethod("QS3DDRAWGLASSWALLADV", CommandFlags.Modal)]
        public void DrawGlassWallAdvanced()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWGLASSWALLADV", () =>
            {
                RequireModelSpace(document);
                var promptUnit = CadUnitService.GetLengthUnit(document);
                var promptUcs = document.Editor.CurrentUserCoordinateSystem;
                var points = AcquirePath(document, "Vách Kính", 2, false);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = PromptPositiveMeters(
                    document.Editor,
                    "Bề dày Vách Kính (m)",
                    hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.GlassWall, "ThicknessM", 0.012d) : 0.012d);
                if (!thicknessM.HasValue) return;
                var heightM = PromptPositiveMeters(
                    document.Editor,
                    "Chiều cao Vách Kính (m)",
                    hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.GlassWall, "HeightM", 3.6d) : 3.6d);
                if (!heightM.HasValue) return;
                var bottomOffsetM = PromptFiniteMeters(
                    document.Editor,
                    "Offset đáy Vách Kính so với Z source (m)",
                    hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.GlassWall, "BottomOffsetM", 0d) : 0d);
                if (!bottomOffsetM.HasValue) return;

                RequirePromptContextUnchanged(document, promptUnit, promptUcs, "QS3DDRAWGLASSWALLADV");
                Execute(
                    document,
                    ElementCategory.GlassWall,
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

        [CommandMethod("QS3DDRAWWALLPIER", CommandFlags.Modal)]
        public void DrawWallPier()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWWALLPIER", () =>
            {
                RequireModelSpace(document);
                // WallPier stays LINE-only so QS3DBUILD3D reaches the specialized profile builder.
                var points = AcquireFixedPath(document, "Trụ Tường nhanh", 2);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.WallPier, "ThicknessM", 0.2d) : 0.2d;
                var heightM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.WallPier, "HeightM", 3.6d) : 3.6d;
                var bottomOffsetM = hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.WallPier, "BottomOffsetM", 0d) : 0d;

                document.Editor.WriteMessage(
                    "\nQS3D Trụ Tường nhanh: dùng Family hiện tại (dày " + thicknessM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, cao " + heightM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, offset " + bottomOffsetM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m). Dùng QS3DDRAWWALLPIERADV khi cần nhập tham số riêng.");

                Execute(
                    document,
                    ElementCategory.WallPier,
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

        [CommandMethod("QS3DDRAWWALLPIERADV", CommandFlags.Modal)]
        public void DrawWallPierAdvanced()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWWALLPIERADV", () =>
            {
                RequireModelSpace(document);
                var promptUnit = CadUnitService.GetLengthUnit(document);
                var promptUcs = document.Editor.CurrentUserCoordinateSystem;
                // Advanced WallPier keeps the same specialized two-point LINE geometry contract.
                var points = AcquireFixedPath(document, "Trụ Tường", 2);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = PromptPositiveMeters(
                    document.Editor,
                    "Bề dày Trụ Tường (m)",
                    hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.WallPier, "ThicknessM", 0.2d) : 0.2d);
                if (!thicknessM.HasValue) return;
                var heightM = PromptPositiveMeters(
                    document.Editor,
                    "Chiều cao Trụ Tường (m)",
                    hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.WallPier, "HeightM", 3.6d) : 3.6d);
                if (!heightM.HasValue) return;
                var bottomOffsetM = PromptFiniteMeters(
                    document.Editor,
                    "Offset đáy Trụ Tường so với Z source (m)",
                    hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.WallPier, "BottomOffsetM", 0d) : 0d);
                if (!bottomOffsetM.HasValue) return;

                RequirePromptContextUnchanged(document, promptUnit, promptUcs, "QS3DDRAWWALLPIERADV");
                Execute(
                    document,
                    ElementCategory.WallPier,
                    () => CreateLine(document, points[0], points[1]),
                    element =>
                    {
                        element.SetProperty("ThicknessM", thicknessM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("HeightM", heightM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.Value.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        [CommandMethod("QS3DDRAWSTRUCTWALL", CommandFlags.Modal)]
        public void DrawStructuralWall()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWSTRUCTWALL", () =>
            {
                RequireModelSpace(document);
                var points = AcquireFixedPath(document, "Vách BTCT nhanh", 2);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.StructuralWall, "ThicknessM", 0.2d) : 0.2d;
                var heightM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.StructuralWall, "HeightM", 3.6d) : 3.6d;
                var bottomOffsetM = hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.StructuralWall, "BottomOffsetM", 0d) : 0d;

                document.Editor.WriteMessage(
                    "\nQS3D Vách BTCT nhanh: dùng Family hiện tại (dày " + thicknessM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, cao " + heightM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, offset " + bottomOffsetM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m). Dùng QS3DDRAWSTRUCTWALLADV khi cần nhập tham số riêng.");

                Execute(
                    document,
                    ElementCategory.StructuralWall,
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

        [CommandMethod("QS3DDRAWSTRUCTWALLADV", CommandFlags.Modal)]
        public void DrawStructuralWallAdvanced()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWSTRUCTWALLADV", () =>
            {
                RequireModelSpace(document);
                var promptUnit = CadUnitService.GetLengthUnit(document);
                var promptUcs = document.Editor.CurrentUserCoordinateSystem;
                var points = AcquireFixedPath(document, "Vách BTCT", 2);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = PromptPositiveMeters(
                    document.Editor,
                    "Bề dày Vách BTCT (m)",
                    hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.StructuralWall, "ThicknessM", 0.2d) : 0.2d);
                if (!thicknessM.HasValue) return;
                var heightM = PromptPositiveMeters(
                    document.Editor,
                    "Chiều cao Vách BTCT (m)",
                    hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.StructuralWall, "HeightM", 3.6d) : 3.6d);
                if (!heightM.HasValue) return;
                var bottomOffsetM = PromptFiniteMeters(
                    document.Editor,
                    "Offset đáy Vách BTCT so với Z source (m)",
                    hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.StructuralWall, "BottomOffsetM", 0d) : 0d);
                if (!bottomOffsetM.HasValue) return;

                RequirePromptContextUnchanged(document, promptUnit, promptUcs, "QS3DDRAWSTRUCTWALLADV");
                Execute(
                    document,
                    ElementCategory.StructuralWall,
                    () => CreateLine(document, points[0], points[1]),
                    element =>
                    {
                        element.SetProperty("ThicknessM", thicknessM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("HeightM", heightM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.Value.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        [CommandMethod("QS3DDRAWFOUNDATION", CommandFlags.Modal)]
        public void DrawFoundation()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWFOUNDATION", () =>
            {
                RequireModelSpace(document);
                var points = AcquirePath(document, "Móng nhanh", 3, true);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Foundation, "ThicknessM", 0.5d) : 0.5d;
                var bottomOffsetM = hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.Foundation, "BottomOffsetM", 0d) : 0d;

                document.Editor.WriteMessage(
                    "\nQS3D Móng nhanh: dùng Family hiện tại (dày " + thicknessM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m, offset " + bottomOffsetM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m). Dùng QS3DDRAWFOUNDATIONADV khi cần nhập tham số riêng.");

                Execute(
                    document,
                    ElementCategory.Foundation,
                    () => CreatePolyline(document, points, true),
                    element =>
                    {
                        element.SetProperty("ThicknessM", thicknessM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        [CommandMethod("QS3DDRAWFOUNDATIONADV", CommandFlags.Modal)]
        public void DrawFoundationAdvanced()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DDRAWFOUNDATIONADV", () =>
            {
                RequireModelSpace(document);
                var promptUnit = CadUnitService.GetLengthUnit(document);
                var promptUcs = document.Editor.CurrentUserCoordinateSystem;
                var points = AcquirePath(document, "Móng", 3, true);
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var thicknessM = PromptPositiveMeters(
                    document.Editor,
                    "Bề dày Móng (m)",
                    hasDefaultsProject ? FamilyNumber(defaultsProject!, ElementCategory.Foundation, "ThicknessM", 0.5d) : 0.5d);
                if (!thicknessM.HasValue) return;
                var bottomOffsetM = PromptFiniteMeters(
                    document.Editor,
                    "Offset đáy Móng so với Z source (m)",
                    hasDefaultsProject ? FamilyFiniteNumber(defaultsProject!, ElementCategory.Foundation, "BottomOffsetM", 0d) : 0d);
                if (!bottomOffsetM.HasValue) return;

                RequirePromptContextUnchanged(document, promptUnit, promptUcs, "QS3DDRAWFOUNDATIONADV");
                Execute(
                    document,
                    ElementCategory.Foundation,
                    () => CreatePolyline(document, points, true),
                    element =>
                    {
                        element.SetProperty("ThicknessM", thicknessM.Value.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.Value.ToString("R", CultureInfo.InvariantCulture));
                    },
                    projectPreview);
            });
        }

        private static void Execute(
            Document document,
            ElementCategory category,
            Func<ObjectId> createSource,
            Action<ProjectElement> configureElement,
            DirectDrawProjectPreviewContext? projectPreview = null)
        {
            var operation = "Direct Draw P1 " + category;
            EnsureActive(document, operation);
            var project = projectPreview != null
                ? projectPreview.ResolveForMutation(document, operation)
                : ProjectContextCoordinator.GetOrCreate(document);
            var rollback = ProjectStateSnapshot.Capture(project);
            var sourceId = ObjectId.Null;
            var sourceHandle = string.Empty;
            ProjectElement? createdElement = null;
            var generatedHandle = string.Empty;

            try
            {
                sourceId = createSource();
                if (sourceId.IsNull || !sourceId.IsValid) throw new InvalidOperationException("Không tạo được CAD source cho Direct Draw P1.");
                sourceHandle = sourceId.Handle.ToString();
                document.Editor.SetImpliedSelection(new[] { sourceId });

                var captured = SemanticCaptureService.Capture(document, category);
                if (captured != 1) throw new InvalidOperationException("Direct Draw P1 cần capture đúng một semantic element, nhận được " + captured + ".");

                createdElement = project.Elements.SingleOrDefault(x =>
                    x.Category == category && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)));
                if (createdElement == null) throw new InvalidOperationException("Không tìm thấy semantic element vừa tạo cho source " + sourceHandle + ".");
                var createdElementId = createdElement.Id;
                configureElement(createdElement);

                // QS3DBUILD3D resolves the active document internally. Re-check immediately before
                // delegating so a document switch can never redirect this P1 operation to another DWG.
                EnsureActive(document, operation + " / QS3DBUILD3D");
                document.Editor.SetImpliedSelection(new[] { sourceId });
                new Build3DCommands().Build3D();
                EnsureActive(document, operation + " / post QS3DBUILD3D");

                // QS3DBUILD3D may restore its own ProjectState snapshot and report the failure at its
                // command surface instead of throwing to this wrapper. A restore replaces element
                // instances, so the pre-build reference is never authoritative after the nested call.
                createdElement = project.Elements.SingleOrDefault(x =>
                    string.Equals(x.Id, createdElementId, StringComparison.OrdinalIgnoreCase));
                if (createdElement == null)
                    throw new InvalidOperationException("Semantic element Direct Draw P1 không còn tồn tại sau QS3DBUILD3D; operation được rollback.");

                if (!createdElement.Properties.TryGetValue("GeneratedSolidHandle", out generatedHandle) || string.IsNullOrWhiteSpace(generatedHandle))
                    throw new InvalidOperationException("QS3DBUILD3D không ghi GeneratedSolidHandle cho Direct Draw P1 " + category + ".");
                var liveGenerated = CadHandleService.GetLiveHandles(document, new[] { generatedHandle });
                if (!liveGenerated.Contains(generatedHandle, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Generated solid của Direct Draw P1 không còn live sau QS3DBUILD3D: " + generatedHandle + ".");

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
                    catch (Exception ex) { ownershipDiscoveryError = ex; }
                }

                Exception? cleanupError = null;
                Exception? restoreError = null;
                try { EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles); }
                catch (Exception ex) { cleanupError = ex; }
                try { rollback.Restore(project); }
                catch (Exception ex) { restoreError = ex; }
                try { document.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); }
                catch { }

                if (ownershipDiscoveryError != null || cleanupError != null || restoreError != null)
                {
                    var errors = new List<Exception> { operationError };
                    if (ownershipDiscoveryError != null) errors.Add(ownershipDiscoveryError);
                    if (cleanupError != null) errors.Add(cleanupError);
                    if (restoreError != null) errors.Add(restoreError);
                    throw new InvalidOperationException("Direct Draw P1 thất bại và rollback không hoàn tất đầy đủ.", new AggregateException(errors));
                }
                throw;
            }

            FinalizeUi(document, createdElement!, sourceId, generatedHandle);
        }

        private static IReadOnlyList<Point3d>? AcquireFixedPath(Document document, string label, int count)
        {
            var editor = document.Editor;
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
            ValidatePlanView(document, points, label);
            return points;
        }

        private static IReadOnlyList<Point3d>? AcquirePath(Document document, string label, int minimumPoints, bool close)
        {
            var editor = document.Editor;
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
                if (points.Count > 0 && result.Value.DistanceTo(points[points.Count - 1]) <= 1e-9d) continue;
                points.Add(result.Value);
            }
            if (close && points.Count >= 3 && points[0].DistanceTo(points[points.Count - 1]) <= 1e-9d) points.RemoveAt(points.Count - 1);
            if (points.Count < minimumPoints) return null;
            ValidatePlanView(document, points, label);
            return points;
        }

        private static void ValidatePlanView(Document document, IReadOnlyList<Point3d> points, string label)
        {
            if (points.Count == 0) return;
            var z = CadGeometryGuard.Finite(points[0].Z, label + "/base Z");
            for (var index = 1; index < points.Count; index++)
            {
                var deltaDrawing = Math.Abs(CadGeometryGuard.Subtract(points[index].Z, z, label + "/delta Z"));
                var deltaM = Math.Abs(CadGeometryGuard.ToMeters(document, deltaDrawing, label + "/delta Z"));
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
                polyline.Elevation = CadGeometryGuard.Finite(points[0].Z, "Direct Draw P1 POLYLINE elevation");
                for (var index = 0; index < points.Count; index++)
                {
                    var x = CadGeometryGuard.Finite(points[index].X, "Direct Draw P1 POLYLINE X[" + index + "]");
                    var y = CadGeometryGuard.Finite(points[index].Y, "Direct Draw P1 POLYLINE Y[" + index + "]");
                    polyline.AddVertexAt(index, new Point2d(x, y), 0d, 0d, 0d);
                }
                polyline.Closed = closed;
                polyline.TransformBy(document.Editor.CurrentUserCoordinateSystem);
                var id = modelSpace.AppendEntity(polyline);
                transaction.AddNewlyCreatedDBObject(polyline, true);
                transaction.Commit();
                return id;
            }
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

        private static void RequirePromptContextUnchanged(
            Document document,
            QS3D.Core.Units.LengthUnit promptUnit,
            Matrix3d promptUcs,
            string operation)
        {
            EnsureActive(document, operation + " / prompt freshness");
            RequireModelSpace(document);
            if (!document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs))
                throw new InvalidOperationException(operation + " dừng vì Current UCS đã thay đổi trong lúc chờ nhập tham số. Hãy chạy lại lệnh.");
            if (CadUnitService.GetLengthUnit(document) != promptUnit)
                throw new InvalidOperationException(operation + " dừng vì drawing unit policy đã thay đổi trong lúc chờ nhập tham số. Hãy chạy lại lệnh.");
        }

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                if (!document.Database.CurrentSpaceId.Equals(blockTable[BlockTableRecord.ModelSpace]))
                    throw new InvalidOperationException("Direct Draw P1 hiện chỉ hỗ trợ Model Space. Chuyển sang tab Model trước khi vẽ.");
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
                throw new InvalidOperationException("Direct Draw P1 hiện chỉ hỗ trợ UCS có mặt phẳng XY song song WCS XY (có thể xoay/di chuyển trong mặt phẳng). UCS nghiêng/3D chưa được hỗ trợ.");
        }

        private static void EraseDirectDrawCad(Document document, ProjectState project, ProjectElement? createdElement, ObjectId sourceId, IEnumerable<string> generatedHandles)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = new HashSet<string>(
                (generatedHandles ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            if (normalized.Count > 0 && createdElement == null)
                throw new InvalidOperationException("Direct Draw P1 rollback found generated CAD without the newly-created semantic owner.");
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
                    if (entity == null) throw new InvalidOperationException("Direct Draw P1 rollback generated handle " + id.Handle + " không còn trỏ tới Entity hợp lệ.");
                    if (entity.IsErased) continue;
                    GeneratedGeometryService.RequireMatchingOwnership(entity, project, createdElement!, "rollback Direct Draw P1 generated CAD " + id.Handle);
                    entity.Erase(true);
                }
                transaction.Commit();
            }

            var remainingGenerated = CadHandleService.GetLiveHandles(document, normalized);
            if (remainingGenerated.Count > 0)
                throw new InvalidOperationException("Direct Draw P1 rollback còn generated CAD handle chưa xóa: " + string.Join(", ", remainingGenerated.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) + ".");
            if (!sourceId.IsNull && sourceId.IsValid)
            {
                var remainingSource = CadHandleService.GetLiveHandles(document, new[] { sourceId.Handle.ToString() });
                if (remainingSource.Count > 0)
                    throw new InvalidOperationException("Direct Draw P1 rollback còn source CAD chưa xóa: " + sourceId.Handle + ".");
            }
        }

        private static void FinalizeUi(Document document, ProjectElement element, ObjectId sourceId, string generatedHandle)
        {
            var status = "Direct Draw P1 " + element.Category + ": source + semantic + native 3D hoàn tất.";
            try
            {
                PaletteCoordinator.RefreshProject();
                if (!string.IsNullOrWhiteSpace(generatedHandle)) CadHandleService.Select(document, new[] { generatedHandle });
                else if (!sourceId.IsNull && sourceId.IsValid) document.Editor.SetImpliedSelection(new[] { sourceId });
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
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
