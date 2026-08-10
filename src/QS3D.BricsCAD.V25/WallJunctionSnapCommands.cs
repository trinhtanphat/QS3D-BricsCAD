using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class WallJunctionSnapCommands
    {
        private const string PreviewSignatureKey = "WallJunctionSnapPreviewSignature";
        private const string PreviewCountKey = "WallJunctionSnapPreviewCount";
        private const string PreviewUtcKey = "WallJunctionSnapPreviewUtc";

        private sealed class EditableSegment
        {
            public string Id { get; set; } = string.Empty;
            public ObjectId ObjectId { get; set; }
            public string SourceHandle { get; set; } = string.Empty;
            public bool IsLine { get; set; }
            public int StartVertex { get; set; }
            public int EndVertex { get; set; }
            public WallAxisSegment Axis { get; set; } = null!;
        }

        private sealed class VertexEdit
        {
            public string Key { get; set; } = string.Empty;
            public ObjectId ObjectId { get; set; }
            public string SourceHandle { get; set; } = string.Empty;
            public bool IsLine { get; set; }
            public WallEndpointKind LineEndpoint { get; set; }
            public int VertexIndex { get; set; }
            public Point2 From { get; set; }
            public Point2 Target { get; set; }
            public WallJunctionKind JunctionKind { get; set; }
            public double DistanceM { get; set; }
        }

        private sealed class SnapPlan
        {
            public IReadOnlyList<EditableSegment> Segments { get; set; } = Array.Empty<EditableSegment>();
            public IReadOnlyList<VertexEdit> Edits { get; set; } = Array.Empty<VertexEdit>();
            public string Signature { get; set; } = string.Empty;
            public double ToleranceM { get; set; }
            public double MovementEpsilonM { get; set; }
        }

        [CommandMethod("QS3DWALLSNAPPREVIEW", CommandFlags.UsePickSet)]
        public void Preview()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DWALLSNAPPREVIEW", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var plan = BuildPlan(document, project, true);
                if (plan.Segments.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3D Wall Snap: chọn semantic wall source LINE/open straight POLYLINE trước.");
                    return;
                }

                project.Metadata[PreviewSignatureKey] = plan.Signature;
                project.Metadata[PreviewCountKey] = plan.Edits.Count.ToString(CultureInfo.InvariantCulture);
                project.Metadata[PreviewUtcKey] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                project.Touch();
                AuditTrail.ForProject(project).Record("wall.junction.snap.preview", string.Empty,
                    plan.Edits.Count.ToString(CultureInfo.InvariantCulture) + " endpoint edit(s) • tolerance=" + plan.ToleranceM.ToString("R", CultureInfo.InvariantCulture));

                var summary = "Wall Snap preview: " + plan.Edits.Count + " endpoint(s) cần chỉnh trong tolerance " + plan.ToleranceM.ToString("0.###", CultureInfo.InvariantCulture) + " m.";
                PaletteCoordinator.SetStatus(summary);
                document.Editor.WriteMessage("\nQS3D " + summary);
                foreach (var edit in plan.Edits.Take(100))
                {
                    document.Editor.WriteMessage("\n  " + edit.SourceHandle + " • " + edit.JunctionKind + " • " + edit.DistanceM.ToString("0.###", CultureInfo.InvariantCulture) + " m → (" + edit.Target.X.ToString("0.###", CultureInfo.InvariantCulture) + ", " + edit.Target.Y.ToString("0.###", CultureInfo.InvariantCulture) + ")");
                }
                if (plan.Edits.Count > 100) document.Editor.WriteMessage("\n  … preview output truncated.");
                document.Editor.WriteMessage("\nChạy QS3DWALLSNAPAPPLY với cùng selection để áp dụng. Curved/bulged polyline không được tự sửa.");
            });
        }

        [CommandMethod("QS3DWALLSNAPAPPLY", CommandFlags.UsePickSet)]
        public void Apply()
        {
            var document = Active();
            if (document == null) return;
            Guard(document, "QS3DWALLSNAPAPPLY", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var plan = BuildPlan(document, project, true);
                if (plan.Segments.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3D Wall Snap: selection không có semantic wall source có thể chỉnh.");
                    return;
                }
                if (!project.Metadata.TryGetValue(PreviewSignatureKey, out var preview) || !string.Equals(preview, plan.Signature, StringComparison.Ordinal))
                    throw new InvalidOperationException("Preview không còn khớp selection/geometry hiện tại. Chạy QS3DWALLSNAPPREVIEW lại trước khi apply.");
                if (project.Metadata.TryGetValue(PreviewCountKey, out var countText) && int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var previewCount) && previewCount != plan.Edits.Count)
                    throw new InvalidOperationException("Số endpoint cần chỉnh đã thay đổi từ preview. Chạy preview lại.");
                if (plan.Edits.Count == 0)
                {
                    ClearPreview(project);
                    PaletteCoordinator.SetStatus("Wall Snap: geometry đã khớp junction; không có endpoint cần chỉnh.");
                    return;
                }

                var touchedHandles = new HashSet<string>(plan.Edits.Select(x => x.SourceHandle), StringComparer.OrdinalIgnoreCase);
                var touchedOwners = ResolveUniqueWallOwners(project, touchedHandles);
                var units = CadUnitService.GetPolicy(document);
                GeneratedGeometryInvalidation invalidation;
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    invalidation = GeneratedDependentGeometryInvalidator.Prepare(document, transaction, project, touchedOwners);
                    foreach (var edit in plan.Edits)
                    {
                        var entity = transaction.GetObject(edit.ObjectId, OpenMode.ForWrite, false) as Entity;
                        if (entity == null || entity.IsErased) throw new InvalidOperationException("Wall source không còn live: " + edit.SourceHandle);
                        var x = units.FromMeters(edit.Target.X);
                        var y = units.FromMeters(edit.Target.Y);
                        if (edit.IsLine)
                        {
                            var line = entity as Line ?? throw new InvalidOperationException("Wall source type đã đổi từ LINE: " + edit.SourceHandle);
                            if (edit.LineEndpoint == WallEndpointKind.Start) line.StartPoint = new Point3d(x, y, line.StartPoint.Z);
                            else line.EndPoint = new Point3d(x, y, line.EndPoint.Z);
                        }
                        else
                        {
                            var polyline = entity as Polyline ?? throw new InvalidOperationException("Wall source type đã đổi từ POLYLINE: " + edit.SourceHandle);
                            if (polyline.Closed || edit.VertexIndex < 0 || edit.VertexIndex >= polyline.NumberOfVertices) throw new InvalidOperationException("Polyline endpoint mapping không còn hợp lệ: " + edit.SourceHandle);
                            if (Math.Abs(polyline.GetBulgeAt(Math.Min(edit.VertexIndex, polyline.NumberOfVertices - 2))) > 1e-12d && edit.VertexIndex < polyline.NumberOfVertices - 1)
                                throw new InvalidOperationException("Không apply snap vào vertex thuộc bulged segment: " + edit.SourceHandle);
                            polyline.SetPointAt(edit.VertexIndex, new Point2d(x, y));
                        }
                    }
                    transaction.Commit();
                }

                invalidation.CommitMetadata();
                foreach (var element in touchedOwners)
                    element.MarkDirty(ElementDirtyFlags.Geometry | ElementDirtyFlags.Quantity);
                var owners = touchedOwners.Count;
                ClearPreview(project);
                project.Touch();
                AuditTrail.ForProject(project).Record("wall.junction.snap.apply", string.Empty,
                    plan.Edits.Count.ToString(CultureInfo.InvariantCulture) + " endpoint edit(s) • owners=" + owners.ToString(CultureInfo.InvariantCulture) + " • invalidated3d=" + invalidation.ElementCount.ToString(CultureInfo.InvariantCulture));
                document.Editor.Regen();
                PaletteCoordinator.RefreshProject();
                var summary = "Wall Snap applied: " + plan.Edits.Count + " endpoint(s) • " + owners + " semantic owner(s) • generated 3D/rebar invalidated. Rebuild 3D/Regenerate trước khi xuất BQ.";
                PaletteCoordinator.SetStatus(summary);
                document.Editor.WriteMessage("\nQS3D " + summary);
            });
        }

        private static SnapPlan BuildPlan(Document document, ProjectState project, bool promptIfEmpty)
        {
            var tolerance = MetadataNumber(project, "WallJunctionToleranceM", 0.005d, false);
            var epsilonFallback = Math.Min(0.000001d, tolerance / 10d);
            var movementEpsilon = MetadataNumber(project, "WallJunctionSnapEpsilonM", epsilonFallback, true);
            if (movementEpsilon >= tolerance) throw new InvalidOperationException("WallJunctionSnapEpsilonM phải nhỏ hơn WallJunctionToleranceM.");
            var planarityTolerance = MetadataNumber(project, "WallJunctionPlanarityToleranceM", tolerance, false);
            var segments = ReadEditableSelection(document, project, planarityTolerance, promptIfEmpty);
            if (segments.Count == 0) return new SnapPlan { ToleranceM = tolerance, MovementEpsilonM = movementEpsilon };
            var adjustmentPlan = new WallJunctionAdjustmentPlanner().Plan(segments.Select(x => x.Axis), tolerance, movementEpsilon);
            var edits = ConsolidateEdits(segments, adjustmentPlan.Adjustments, movementEpsilon);
            return new SnapPlan
            {
                Segments = segments,
                Edits = edits,
                Signature = BuildSignature(segments, edits, tolerance, movementEpsilon),
                ToleranceM = tolerance,
                MovementEpsilonM = movementEpsilon
            };
        }

        private static IReadOnlyList<EditableSegment> ReadEditableSelection(Document document, ProjectState project, double planarityToleranceM, bool promptIfEmpty)
        {
            var editor = document.Editor;
            var selection = editor.SelectImplied();
            if ((selection.Status != PromptStatus.OK || selection.Value == null) && promptIfEmpty)
            {
                selection = editor.GetSelection();
                if (selection.Status == PromptStatus.OK && selection.Value != null) editor.SetImpliedSelection(selection.Value.GetObjectIds());
            }
            if (selection.Status != PromptStatus.OK || selection.Value == null) return Array.Empty<EditableSegment>();

            var wallHandles = new HashSet<string>(project.Elements.Where(x => IsWall(x.Category)).SelectMany(x => x.SourceHandles), StringComparer.OrdinalIgnoreCase);
            var units = CadUnitService.GetPolicy(document);
            var result = new List<EditableSegment>();
            double? referenceElevationM = null;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in selection.Value.GetObjectIds())
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    var handle = entity.Handle.ToString();
                    if (!wallHandles.Contains(handle)) continue;
                    if (entity is Line line)
                    {
                        var startElevation = units.ToMeters(line.StartPoint.Z);
                        var endElevation = units.ToMeters(line.EndPoint.Z);
                        EnsureElevation(ref referenceElevationM, startElevation, planarityToleranceM, handle + "/start");
                        EnsureElevation(ref referenceElevationM, endElevation, planarityToleranceM, handle + "/end");
                        var axis = new WallAxisSegment("L:" + handle,
                            new Point2(units.ToMeters(line.StartPoint.X), units.ToMeters(line.StartPoint.Y)),
                            new Point2(units.ToMeters(line.EndPoint.X), units.ToMeters(line.EndPoint.Y)));
                        result.Add(new EditableSegment { Id = axis.Id, ObjectId = id, SourceHandle = handle, IsLine = true, StartVertex = -1, EndVertex = -1, Axis = axis });
                        continue;
                    }

                    if (!(entity is Polyline polyline)) continue;
                    if (polyline.Closed) throw new InvalidOperationException("Wall Snap chỉ hỗ trợ open POLYLINE: " + handle);
                    var normal = polyline.Normal;
                    if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || normal.Z < 1d - 1e-9d)
                        throw new InvalidOperationException("Wall Snap yêu cầu plan-view POLYLINE +Z: " + handle);
                    EnsureElevation(ref referenceElevationM, units.ToMeters(polyline.Elevation), planarityToleranceM, handle);
                    for (var index = 0; index < polyline.NumberOfVertices - 1; index++)
                    {
                        if (Math.Abs(polyline.GetBulgeAt(index)) > 1e-12d)
                            throw new InvalidOperationException("Wall Snap không tự chỉnh bulged/curved POLYLINE. Dùng QS3DWALLJUNCTIONS để review: " + handle);
                        var a = polyline.GetPoint2dAt(index);
                        var b = polyline.GetPoint2dAt(index + 1);
                        var axis = new WallAxisSegment("P:" + handle + ":" + index.ToString(CultureInfo.InvariantCulture),
                            new Point2(units.ToMeters(a.X), units.ToMeters(a.Y)),
                            new Point2(units.ToMeters(b.X), units.ToMeters(b.Y)));
                        result.Add(new EditableSegment { Id = axis.Id, ObjectId = id, SourceHandle = handle, IsLine = false, StartVertex = index, EndVertex = index + 1, Axis = axis });
                    }
                }
                transaction.Commit();
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<VertexEdit> ConsolidateEdits(IReadOnlyList<EditableSegment> segments, IReadOnlyList<WallEndpointAdjustment> adjustments, double epsilonM)
        {
            var bindings = segments.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var edits = new Dictionary<string, VertexEdit>(StringComparer.OrdinalIgnoreCase);
            foreach (var adjustment in adjustments)
            {
                if (!bindings.TryGetValue(adjustment.SegmentId, out var segment)) throw new InvalidOperationException("Wall Snap segment mapping bị mất: " + adjustment.SegmentId);
                var vertex = adjustment.Endpoint == WallEndpointKind.Start ? segment.StartVertex : segment.EndVertex;
                var key = segment.IsLine
                    ? segment.SourceHandle + "/" + adjustment.Endpoint
                    : segment.SourceHandle + "/V" + vertex.ToString(CultureInfo.InvariantCulture);
                var candidate = new VertexEdit
                {
                    Key = key,
                    ObjectId = segment.ObjectId,
                    SourceHandle = segment.SourceHandle,
                    IsLine = segment.IsLine,
                    LineEndpoint = adjustment.Endpoint,
                    VertexIndex = vertex,
                    From = adjustment.From,
                    Target = adjustment.To,
                    JunctionKind = adjustment.JunctionKind,
                    DistanceM = adjustment.Distance
                };
                if (edits.TryGetValue(key, out var existing))
                {
                    if (existing.Target.DistanceTo(candidate.Target) > epsilonM)
                        throw new InvalidOperationException("Một wall vertex có nhiều snap target khác nhau: " + key);
                    if (candidate.DistanceM < existing.DistanceM) edits[key] = candidate;
                }
                else edits[key] = candidate;
            }
            return edits.Values.OrderBy(x => x.SourceHandle, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static IReadOnlyList<ProjectElement> ResolveUniqueWallOwners(ProjectState project, ISet<string> touchedHandles)
        {
            var owners = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in touchedHandles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var matches = project.Elements
                    .Where(x => x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
                    .Take(3)
                    .ToList();
                if (matches.Count != 1)
                    throw new InvalidOperationException("Wall source " + handle + " phải có đúng một semantic owner trước khi snap; hiện có " + matches.Count + ".");
                var owner = matches[0];
                if (!IsWall(owner.Category))
                    throw new InvalidOperationException("Wall source " + handle + " đang thuộc semantic category không phải wall: " + owner.Category + ".");
                owners[owner.Id] = owner;
            }
            return owners.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static string BuildSignature(IReadOnlyList<EditableSegment> segments, IReadOnlyList<VertexEdit> edits, double tolerance, double epsilon)
        {
            var text = new StringBuilder();
            text.Append("tol=").Append(tolerance.ToString("R", CultureInfo.InvariantCulture)).Append("|eps=").Append(epsilon.ToString("R", CultureInfo.InvariantCulture));
            foreach (var segment in segments.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                text.Append("|S:").Append(segment.Id).Append(':')
                    .Append(segment.Axis.Start.X.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(segment.Axis.Start.Y.ToString("R", CultureInfo.InvariantCulture)).Append('>')
                    .Append(segment.Axis.End.X.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(segment.Axis.End.Y.ToString("R", CultureInfo.InvariantCulture));
            }
            foreach (var edit in edits)
            {
                text.Append("|E:").Append(edit.Key).Append(':')
                    .Append(edit.Target.X.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(edit.Target.Y.ToString("R", CultureInfo.InvariantCulture));
            }
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                var output = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) output.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return output.ToString();
            }
        }

        private static void ClearPreview(ProjectState project)
        {
            project.Metadata.Remove(PreviewSignatureKey);
            project.Metadata.Remove(PreviewCountKey);
            project.Metadata.Remove(PreviewUtcKey);
        }

        private static void EnsureElevation(ref double? referenceElevationM, double elevationM, double toleranceM, string label)
        {
            if (double.IsNaN(elevationM) || double.IsInfinity(elevationM)) throw new InvalidOperationException("Wall elevation không hữu hạn: " + label);
            if (!referenceElevationM.HasValue) { referenceElevationM = elevationM; return; }
            var delta = elevationM - referenceElevationM.Value;
            if (double.IsNaN(delta) || double.IsInfinity(delta) || Math.Abs(delta) > toleranceM)
                throw new InvalidOperationException("Wall Snap selection phải đồng phẳng theo Z: " + label);
        }

        private static double MetadataNumber(ProjectState project, string key, double fallback, bool allowZero)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || (allowZero ? value < 0d : value <= 0d))
                throw new InvalidOperationException(key + " không hợp lệ: " + raw);
            return value;
        }

        private static bool IsWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall || category == ElementCategory.GlassWall || category == ElementCategory.WallPier || category == ElementCategory.StructuralWall;

        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;

        private static void Guard(Document document, string operation, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                var message = operation + " lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
