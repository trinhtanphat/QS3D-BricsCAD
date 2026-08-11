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
    /// BLT-style reference-driven Architectural Wall authoring.
    /// The selected reference LINE is read-only: QS3D derives direction/center from it,
    /// creates a new source LINE using the requested length, captures one semantic wall,
    /// regenerates it, and commits one owned native solid or rolls the whole operation back.
    /// </summary>
    public sealed class DirectDrawReferenceWallCommands
    {
        private const double PlanarityToleranceM = 0.005d;

        [CommandMethod("QS3DDRAWWALLREF", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void DrawWallFromReference() => DrawWallFromReferenceCore(promptParameters: false, operation: "QS3DDRAWWALLREF");

        [CommandMethod("QS3DDRAWWALLREFADV", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void DrawWallFromReferenceAdvanced() => DrawWallFromReferenceCore(promptParameters: true, operation: "QS3DDRAWWALLREFADV");

        private static void DrawWallFromReferenceCore(bool promptParameters, string operation)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            Guard(document, operation, () =>
            {
                RequireModelSpace(document);
                var reference = AcquireReferenceLine(document);
                if (reference == null) return;

                EnsureActive(document, operation + " / parameters");
                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var lengthM = reference.LengthM;
                var thicknessM = hasDefaultsProject
                    ? FamilyNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "ThicknessM", 0.2d)
                    : 0.2d;
                var heightM = hasDefaultsProject
                    ? FamilyNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "HeightM", 3.6d)
                    : 3.6d;
                var bottomOffsetM = hasDefaultsProject
                    ? FamilyFiniteNumber(defaultsProject!, ElementCategory.ArchitecturalWall, "BottomOffsetM", 0d)
                    : 0d;

                if (promptParameters)
                {
                    var promptedLength = PromptPositiveMeters(document.Editor, "Chiều dài Tường (m)", lengthM);
                    if (!promptedLength.HasValue) return;
                    lengthM = promptedLength.Value;

                    var promptedThickness = PromptPositiveMeters(document.Editor, "Bề dày Tường (m)", thicknessM);
                    if (!promptedThickness.HasValue) return;
                    thicknessM = promptedThickness.Value;

                    var promptedHeight = PromptPositiveMeters(document.Editor, "Chiều cao Tường (m)", heightM);
                    if (!promptedHeight.HasValue) return;
                    heightM = promptedHeight.Value;

                    var promptedBottomOffset = PromptFiniteMeters(document.Editor, "Offset đáy Tường so với Z tham chiếu (m)", bottomOffsetM);
                    if (!promptedBottomOffset.HasValue) return;
                    bottomOffsetM = promptedBottomOffset.Value;
                }
                else
                {
                    document.Editor.WriteMessage(
                        "\nQS3D Tường theo tham chiếu nhanh: giữ chiều dài LINE tham chiếu " +
                        lengthM.ToString("0.###", CultureInfo.InvariantCulture) + " m, dùng Family hiện tại (dày " +
                        thicknessM.ToString("0.###", CultureInfo.InvariantCulture) + " m, cao " +
                        heightM.ToString("0.###", CultureInfo.InvariantCulture) + " m, offset " +
                        bottomOffsetM.ToString("0.###", CultureInfo.InvariantCulture) +
                        " m). Dùng QS3DDRAWWALLREFADV khi cần đổi chiều dài hoặc tham số riêng.");
                }

                var endpoints = reference.CreateCenteredEndpoints(document, lengthM);
                EnsureActive(document, operation + " / execute boundary");
                var project = projectPreview.ResolveForMutation(document, operation);
                Execute(
                    document,
                    project,
                    () => CreateWcsLine(document, endpoints.Start, endpoints.End),
                    element =>
                    {
                        element.SetProperty("ThicknessM", thicknessM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("HeightM", heightM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("BottomOffsetM", bottomOffsetM.ToString("R", CultureInfo.InvariantCulture));
                        element.SetProperty("QS3D.DirectDraw.Mode", "ReferenceLine");
                    });
            });
        }

        private static ReferenceLinePlan? AcquireReferenceLine(Document document)
        {
            EnsureActive(document, "QS3DDRAWWALLREF / reference");

            var implied = document.Editor.SelectImplied();
            if (implied.Status == PromptStatus.OK)
            {
                var objectIds = implied.Value.GetObjectIds();
                if (objectIds.Length == 1)
                {
                    var impliedReference = ReadReferenceLine(document, objectIds[0], failIfNotLine: false);
                    if (impliedReference != null)
                    {
                        document.Editor.WriteMessage("\nQS3D Tường theo tham chiếu: dùng LINE đã chọn sẵn.");
                        return impliedReference;
                    }
                }
            }

            var options = new PromptEntityOptions("\nChọn LINE tham chiếu cho Tường KT: ");
            var result = document.Editor.GetEntity(options);
            if (result.Status != PromptStatus.OK) return null;
            return ReadReferenceLine(document, result.ObjectId, failIfNotLine: true);
        }

        private static ReferenceLinePlan? ReadReferenceLine(Document document, ObjectId objectId, bool failIfNotLine)
        {
            if (objectId.IsNull || !objectId.IsValid)
            {
                if (failIfNotLine)
                    throw new InvalidOperationException("Tham chiếu Tường KT không còn là CAD object hợp lệ.");
                return null;
            }

            Point3d start;
            Point3d end;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var line = transaction.GetObject(objectId, OpenMode.ForRead) as Line;
                if (line == null)
                {
                    if (failIfNotLine)
                        throw new InvalidOperationException("Tham chiếu Tường KT phải là LINE. POLYLINE/ARC chưa được dùng làm reference cho lệnh này.");
                    return null;
                }
                start = line.StartPoint;
                end = line.EndPoint;
                transaction.Commit();
            }

            return ReferenceLinePlan.Create(document, start, end);
        }

        private static void Execute(
            Document document,
            ProjectState project,
            Func<ObjectId> createSource,
            Action<ProjectElement> configureElement)
        {
            EnsureActive(document, "QS3DDRAWWALLREF / execute");
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (createSource == null) throw new ArgumentNullException(nameof(createSource));
            if (configureElement == null) throw new ArgumentNullException(nameof(configureElement));

            var rollback = ProjectStateSnapshot.Capture(project);
            var sourceId = ObjectId.Null;
            var sourceHandle = string.Empty;
            var createdElementId = string.Empty;
            ProjectElement? createdElement = null;
            var regenerated = 0;
            var solids = 0;

            try
            {
                EnsureActive(document, "QS3DDRAWWALLREF / create source");
                sourceId = createSource();
                if (sourceId.IsNull || !sourceId.IsValid)
                    throw new InvalidOperationException("Không tạo được CAD source LINE cho Tường KT theo tham chiếu.");
                sourceHandle = sourceId.Handle.ToString();
                document.Editor.SetImpliedSelection(new[] { sourceId });

                var captured = SemanticCaptureService.Capture(document, ElementCategory.ArchitecturalWall);
                if (captured != 1)
                    throw new InvalidOperationException("Tường theo tham chiếu cần capture đúng một semantic element, nhận được " + captured + ".");

                createdElement = project.Elements.SingleOrDefault(x =>
                    x.Category == ElementCategory.ArchitecturalWall &&
                    x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)));
                if (createdElement == null)
                    throw new InvalidOperationException("Không tìm thấy semantic Tường vừa tạo cho source " + sourceHandle + ".");

                createdElementId = createdElement.Id;
                configureElement(createdElement);

                regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault())
                    .RegenerateDirtySubset(project, new[] { createdElementId });
                createdElement = project.Elements.SingleOrDefault(x =>
                    string.Equals(x.Id, createdElementId, StringComparison.OrdinalIgnoreCase));
                if (createdElement == null)
                    throw new InvalidOperationException("Semantic Tường theo tham chiếu không còn tồn tại sau regenerate; operation được rollback.");

                EnsureActive(document, "QS3DDRAWWALLREF / build solid");
                document.Editor.SetImpliedSelection(new[] { sourceId });
                solids = WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall);
                if (solids != 1)
                    throw new InvalidOperationException("Native wall builder phải tạo đúng một solid cho Tường theo tham chiếu, nhận được " + solids + ".");

                createdElement = project.Elements.SingleOrDefault(x =>
                    string.Equals(x.Id, createdElementId, StringComparison.OrdinalIgnoreCase));
                if (createdElement == null)
                    throw new InvalidOperationException("Semantic Tường theo tham chiếu bị mất sau native build.");
                if (!createdElement.Properties.TryGetValue("GeneratedSolidHandle", out var generatedHandle) || string.IsNullOrWhiteSpace(generatedHandle))
                    throw new InvalidOperationException("Native wall builder không ghi GeneratedSolidHandle cho Tường theo tham chiếu.");
                var liveGenerated = CadHandleService.GetLiveHandles(document, new[] { generatedHandle });
                if (!liveGenerated.Contains(generatedHandle, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Generated solid của Tường theo tham chiếu không còn live sau build: " + generatedHandle + ".");

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
                        foreach (var handle in GeneratedGeometryService.FindMatchingOwnedHandles(
                            document,
                            project.ProjectId,
                            createdElement.Id,
                            createdElement.Category))
                        {
                            if (!string.IsNullOrWhiteSpace(handle)) generatedHandles.Add(handle.Trim());
                        }
                    }
                    catch (Exception ex)
                    {
                        ownershipDiscoveryError = ex;
                    }
                }

                Exception? cleanupError = null;
                Exception? restoreError = null;
                try { EraseCreatedCad(document, project, createdElement, sourceId, generatedHandles); }
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
                    throw new InvalidOperationException(
                        "Tường theo tham chiếu thất bại và rollback không hoàn tất đầy đủ.",
                        new AggregateException(errors));
                }
                throw;
            }

            FinalizeUi(document, createdElement!, sourceId, solids, regenerated);
        }

        private static ObjectId CreateWcsLine(Document document, Point3d start, Point3d end)
        {
            if (start.DistanceTo(end) <= 1e-9d)
                throw new InvalidOperationException("LINE Tường theo tham chiếu quá ngắn.");

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

        private static void EraseCreatedCad(
            Document document,
            ProjectState project,
            ProjectElement? createdElement,
            ObjectId sourceId,
            IEnumerable<string> generatedHandles)
        {
            var normalized = new HashSet<string>(
                (generatedHandles ?? Array.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            if (normalized.Count > 0 && createdElement == null)
                throw new InvalidOperationException("Rollback Tường theo tham chiếu tìm thấy generated CAD nhưng không còn semantic owner.");

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
                    if (entity == null)
                        throw new InvalidOperationException("Rollback Tường theo tham chiếu: generated handle " + id.Handle + " không còn là Entity hợp lệ.");
                    if (entity.IsErased) continue;
                    GeneratedGeometryService.RequireMatchingOwnership(
                        entity,
                        project,
                        createdElement!,
                        "rollback reference wall generated CAD " + id.Handle);
                    entity.Erase(true);
                }
                transaction.Commit();
            }

            var remainingGenerated = CadHandleService.GetLiveHandles(document, normalized);
            if (remainingGenerated.Count > 0)
                throw new InvalidOperationException("Rollback Tường theo tham chiếu còn generated CAD: " + string.Join(", ", remainingGenerated.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) + ".");
            if (!sourceId.IsNull && sourceId.IsValid)
            {
                var remainingSource = CadHandleService.GetLiveHandles(document, new[] { sourceId.Handle.ToString() });
                if (remainingSource.Count > 0)
                    throw new InvalidOperationException("Rollback Tường theo tham chiếu còn source CAD: " + sourceId.Handle + ".");
            }
        }

        private static void FinalizeUi(Document document, ProjectElement element, ObjectId sourceId, int solids, int regenerated)
        {
            var status = "Tường theo tham chiếu: 1 semantic • " + solids + " solid • regenerate " + regenerated + ".";
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

        private static double? PromptPositiveMeters(Editor editor, string label, double defaultValue)
        {
            var safeDefault = CadGeometryGuard.Positive(defaultValue, label + " default");
            var options = new PromptDoubleOptions("\n" + label + ": ")
            {
                AllowNegative = false,
                AllowZero = false,
                AllowNone = true,
                DefaultValue = safeDefault,
                UseDefaultValue = true
            };
            var result = editor.GetDouble(options);
            if (result.Status == PromptStatus.Cancel) return null;
            if (result.Status != PromptStatus.OK && result.Status != PromptStatus.None) return null;
            var value = result.Status == PromptStatus.OK ? result.Value : safeDefault;
            return CadGeometryGuard.Positive(value, label);
        }

        private static double? PromptFiniteMeters(Editor editor, string label, double defaultValue)
        {
            var safeDefault = CadGeometryGuard.Finite(defaultValue, label + " default");
            var options = new PromptDoubleOptions("\n" + label + ": ")
            {
                AllowNegative = true,
                AllowZero = true,
                AllowNone = true,
                DefaultValue = safeDefault,
                UseDefaultValue = true
            };
            var result = editor.GetDouble(options);
            if (result.Status == PromptStatus.Cancel) return null;
            if (result.Status != PromptStatus.OK && result.Status != PromptStatus.None) return null;
            var value = result.Status == PromptStatus.OK ? result.Value : safeDefault;
            return CadGeometryGuard.Finite(value, label);
        }

        private static double FamilyNumber(ProjectState project, ElementCategory category, string key, double fallback)
        {
            var value = FamilyFiniteNumber(project, category, key, fallback);
            if (!(value > 0d))
                throw new InvalidOperationException("Family " + category + "/" + key + " phải là số hữu hạn > 0 trước khi vẽ theo tham chiếu.");
            return value;
        }

        private static double FamilyFiniteNumber(ProjectState project, ElementCategory category, string key, double fallback)
        {
            var family = PreferredFamily(project, category);
            if (family == null || !family.Properties.TryGetValue(key, out var raw)) return fallback;
            if (string.IsNullOrWhiteSpace(raw) ||
                !double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new InvalidOperationException(
                    "Family '" + family.Name + "' (" + category + ") có " + key + " không hợp lệ: '" + (raw ?? string.Empty) + "'. Sửa Family trước khi vẽ.");
            }
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

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                if (!document.Database.CurrentSpaceId.Equals(modelSpaceId))
                    throw new InvalidOperationException("Tường theo tham chiếu hiện chỉ hỗ trợ Model Space. Chuyển sang tab Model trước khi vẽ.");
                transaction.Commit();
            }
        }

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " yêu cầu đúng DWG đã bắt đầu lệnh vẫn là bản vẽ active.");
        }

        private static void Guard(Document document, string operation, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message);
                PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message);
            }
        }

        private sealed class ReferenceLinePlan
        {
            private ReferenceLinePlan(Point3d center, double unitX, double unitY, double lengthM)
            {
                Center = center;
                UnitX = unitX;
                UnitY = unitY;
                LengthM = lengthM;
            }

            public Point3d Center { get; }
            public double UnitX { get; }
            public double UnitY { get; }
            public double LengthM { get; }

            public static ReferenceLinePlan Create(Document document, Point3d start, Point3d end)
            {
                var dx = CadGeometryGuard.Subtract(end.X, start.X, "Reference wall / dx");
                var dy = CadGeometryGuard.Subtract(end.Y, start.Y, "Reference wall / dy");
                var dz = CadGeometryGuard.Subtract(end.Z, start.Z, "Reference wall / dz");
                var dzM = Math.Abs(CadGeometryGuard.ToMeters(document, dz, "Reference wall / dz"));
                if (dzM > PlanarityToleranceM)
                    throw new InvalidOperationException("LINE tham chiếu phải nằm trong plan-view, |ΔZ| <= 0.005 m.");

                var planarLength = CadGeometryGuard.Hypot(dx, dy, "Reference wall / planar length drawing units");
                planarLength = CadGeometryGuard.Positive(planarLength, "Reference wall / planar length drawing units");
                var lengthM = CadGeometryGuard.Positive(
                    CadGeometryGuard.ToMeters(document, planarLength, "Reference wall / length"),
                    "Reference wall / length meters");
                var center = new Point3d(
                    CadGeometryGuard.Add(start.X, dx / 2d, "Reference wall / center X"),
                    CadGeometryGuard.Add(start.Y, dy / 2d, "Reference wall / center Y"),
                    CadGeometryGuard.Add(start.Z, dz / 2d, "Reference wall / center Z"));
                return new ReferenceLinePlan(center, dx / planarLength, dy / planarLength, lengthM);
            }

            public Endpoints CreateCenteredEndpoints(Document document, double lengthM)
            {
                var drawingLength = CadGeometryGuard.Positive(
                    CadGeometryGuard.ToDrawingUnits(document, lengthM, "Reference wall requested length"),
                    "Reference wall requested length drawing units");
                var half = drawingLength / 2d;
                var dx = UnitX * half;
                var dy = UnitY * half;
                return new Endpoints(
                    new Point3d(
                        CadGeometryGuard.Subtract(Center.X, dx, "Reference wall start X"),
                        CadGeometryGuard.Subtract(Center.Y, dy, "Reference wall start Y"),
                        Center.Z),
                    new Point3d(
                        CadGeometryGuard.Add(Center.X, dx, "Reference wall end X"),
                        CadGeometryGuard.Add(Center.Y, dy, "Reference wall end Y"),
                        Center.Z));
            }
        }

        private sealed class Endpoints
        {
            public Endpoints(Point3d start, Point3d end)
            {
                Start = start;
                End = end;
            }

            public Point3d Start { get; }
            public Point3d End { get; }
        }
    }
}