using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class AutoHostLinkCommands
    {
        private const string OpeningReadFailure = "bỏ qua — không thể đọc/đánh giá source CAD của opening.";
        private const string OperationFailure = "QS3DAUTOLINKHOSTS lỗi: không thể hoàn tất thao tác. Vui lòng thử lại.";
        private const string PostCommitUiWarning = "[QS3D] Auto Host đã commit nhưng đồng bộ giao diện chưa hoàn tất. Hãy refresh giao diện.";

        private sealed class PlannedLink
        {
            public ProjectElement Opening { get; set; } = null!;
            public string HostId { get; set; } = string.Empty;
            public double GapM { get; set; }
        }

        private sealed class OpeningLocation
        {
            public Point2 Plan { get; set; }
            public double ReferenceElevationM { get; set; }
            public double? TopElevationM { get; set; }
            public bool UsesLevelPlacement { get; set; }
        }

        [CommandMethod("QS3DAUTOLINKHOSTS", CommandFlags.UsePickSet)]
        public void AutoLinkHosts()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var selected = ReadSelectedHandles(document);
                if (selected.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DAUTOLINKHOSTS: chọn Cửa/Lỗ Mở đã được QS3D capture.");
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    document.Editor.WriteMessage("\nQS3DAUTOLINKHOSTS: cần một QS3D project hiện hữu; Auto Host không tạo project mới.");
                    return;
                }

                var previewOpenings = ResolveSelectedOpenings(previewProject, selected);
                if (previewOpenings.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DAUTOLINKHOSTS: selection không chứa semantic Cửa/Lỗ Mở.");
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var expectedOpeningIds = new HashSet<string>(
                    previewOpenings.Select(x => x.Id),
                    StringComparer.OrdinalIgnoreCase);

                if (!ExistingProjectMutationContext.TryGet(document, out var project))
                {
                    document.Editor.WriteMessage("\nQS3DAUTOLINKHOSTS: QS3D project không còn khả dụng cho mutation; hãy chạy lại trên project hiện hành.");
                    return;
                }
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("Auto Host: QS3D project đã thay đổi sau khi đọc selection; hãy chọn lại Cửa/Lỗ Mở target.");

                var openings = ResolveSelectedOpenings(project, selected);
                if (!expectedOpeningIds.SetEquals(openings.Select(x => x.Id)))
                    throw new InvalidOperationException("Auto Host: Door/WallOpening target set đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                var maxGapM = MetadataNumber(project, "AutoHostMaxGapM", 0.25d, allowZero: true);
                var ambiguityM = MetadataNumber(project, "AutoHostAmbiguityM", 0.02d, allowZero: true);
                var elevationToleranceM = MetadataNumber(project, "AutoHostElevationToleranceM", 0.25d, allowZero: true);
                var sagittaM = MetadataNumber(project, "WallArcSagittaM", 0.002d, allowZero: false);
                var matcher = new OpeningHostMatcher();
                var planned = new List<PlannedLink>();
                var ambiguous = 0;
                var unmatched = 0;
                var invalid = 0;

                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (var opening in openings)
                    {
                        try
                        {
                            var location = ReadOpeningLocation(document, transaction, project, opening);
                            var candidates = ReadHostSegments(document, transaction, project, opening, location, elevationToleranceM, sagittaM);
                            var result = matcher.Match(location.Plan, candidates, maxGapM, ambiguityM);
                            if (result.Status == OpeningHostMatchStatus.Ambiguous)
                            {
                                ambiguous++;
                                document.Editor.WriteMessage("\n  " + opening.Id + ": ambiguous " + result.HostElementId + " / " + result.SecondaryHostElementId + " (gap " + result.GapM.ToString("0.###", CultureInfo.InvariantCulture) + " / " + result.SecondaryGapM.ToString("0.###", CultureInfo.InvariantCulture) + " m)");
                                continue;
                            }
                            if (result.Status != OpeningHostMatchStatus.Matched)
                            {
                                unmatched++;
                                document.Editor.WriteMessage("\n  " + opening.Id + ": không có host cùng scope/elevation trong phạm vi " + maxGapM.ToString("0.###", CultureInfo.InvariantCulture) + " m.");
                                continue;
                            }
                            planned.Add(new PlannedLink { Opening = opening, HostId = result.HostElementId, GapM = result.GapM });
                        }
                        catch (System.Exception)
                        {
                            invalid++;
                            document.Editor.WriteMessage("\n  " + opening.Id + ": " + OpeningReadFailure);
                        }
                    }
                    transaction.Commit();
                }

                var service = new HostLinkService();
                var linked = 0;
                var unchanged = 0;
                var regenerated = 0;
                if (planned.Count > 0)
                {
                    var rollback = ProjectStateSnapshot.Capture(project);
                    try
                    {
                        var regenerationTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var item in planned)
                        {
                            var existing = item.Opening.Properties.TryGetValue("HostWallId", out var hostId) ? hostId : string.Empty;
                            var previousHostId = (existing ?? string.Empty).Trim();
                            if (HasCanonicalHostLink(item.Opening, item.HostId))
                            {
                                if (UpdateAutoHostMetadata(item.Opening, item.GapM)) project.Touch();
                                unchanged++;
                                continue;
                            }

                            regenerationTargets.Add(item.Opening.Id);
                            regenerationTargets.Add(item.HostId);
                            if (previousHostId.Length > 0 && project.FindElement(previousHostId) != null)
                                regenerationTargets.Add(previousHostId);

                            service.LinkOpening(project, item.Opening.Id, item.HostId);
                            if (UpdateAutoHostMetadata(item.Opening, item.GapM)) project.Touch();
                            linked++;
                        }

                        regenerated = linked > 0 ? Regenerate(project, regenerationTargets) : 0;
                    }
                    catch (System.Exception operationError)
                    {
                        try { rollback.Restore(project); }
                        catch (System.Exception restoreError)
                        {
                            throw new InvalidOperationException(
                                "Auto Host batch failed and project rollback also failed.",
                                new AggregateException(operationError, restoreError));
                        }
                        throw;
                    }
                }

                var summary = "Auto Host: linked=" + linked + " • unchanged=" + unchanged + " • ambiguous=" + ambiguous + " • unmatched=" + unmatched + " • invalid=" + invalid;
                if (regenerated > 0) summary += " • regen=" + regenerated;
                FinalizeAutoHostUi(document, summary);
            }
            catch (System.Exception)
            {
                ReportAutoHostError(document);
            }
        }

        private static void FinalizeAutoHostUi(Document document, string summary)
        {
            var warning = false;
            try { PaletteCoordinator.RefreshProject(); }
            catch (System.Exception) { warning = true; }
            try { PaletteCoordinator.SetStatus(summary); }
            catch (System.Exception) { warning = true; }
            try { document.Editor.WriteMessage("\nQS3D " + summary + ". Chạy QS3DCUTOPENINGS khi muốn áp physical boolean."); }
            catch (System.Exception) { warning = true; }
            if (!warning) return;
            try { document.Editor.WriteMessage("\n" + PostCommitUiWarning); }
            catch { }
        }

        private static void ReportAutoHostError(Document document)
        {
            var message = OperationFailure;
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
            try { document.Editor.WriteMessage("\n" + message); }
            catch { }
        }

        internal static string LinkSingleOpening(Document document, ProjectState project, string openingId)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(openingId)) throw new ArgumentException("Opening id is required.", nameof(openingId));
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("Auto Host single-opening mutation requires the DWG that started authoring to remain active.");
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject) || !ReferenceEquals(currentProject, project))
                throw new InvalidOperationException("Auto Host single-opening mutation requires the exact canonical project authorized by the authoring command.");

            var opening = project.FindElement(openingId) ??
                throw new InvalidOperationException("Opening element not found: " + openingId);
            if (opening.Category != ElementCategory.Door && opening.Category != ElementCategory.WallOpening)
                throw new InvalidOperationException("Element is not an opening/door: " + opening.Id);

            var maxGapM = MetadataNumber(project, "AutoHostMaxGapM", 0.25d, allowZero: true);
            var ambiguityM = MetadataNumber(project, "AutoHostAmbiguityM", 0.02d, allowZero: true);
            var elevationToleranceM = MetadataNumber(project, "AutoHostElevationToleranceM", 0.25d, allowZero: true);
            var sagittaM = MetadataNumber(project, "WallArcSagittaM", 0.002d, allowZero: false);
            OpeningHostMatchResult match;

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var location = ReadOpeningLocation(document, transaction, project, opening);
                var candidates = ReadHostSegments(document, transaction, project, opening, location, elevationToleranceM, sagittaM);
                match = new OpeningHostMatcher().Match(location.Plan, candidates, maxGapM, ambiguityM);
                transaction.Commit();
            }

            if (match.Status == OpeningHostMatchStatus.Ambiguous)
                throw new InvalidOperationException(
                    "Opening " + opening.Id + " has ambiguous Auto Host candidates " + match.HostElementId + " / " +
                    match.SecondaryHostElementId + ". Refusing to guess a host.");
            if (match.Status != OpeningHostMatchStatus.Matched || string.IsNullOrWhiteSpace(match.HostElementId))
                throw new InvalidOperationException(
                    "Opening " + opening.Id + " has no unique compatible host within " +
                    maxGapM.ToString("0.###", CultureInfo.InvariantCulture) + " m.");

            new HostLinkService().LinkOpening(project, opening.Id, match.HostElementId);
            if (UpdateAutoHostMetadata(opening, match.GapM)) project.Touch();
            return match.HostElementId;
        }

        private static HashSet<string> ReadSelectedHandles(Document document)
        {
            var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
            return new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
        }

        private static List<ProjectElement> ResolveSelectedOpenings(ProjectState project, HashSet<string> selected) =>
            project.Elements
                .Where(x => (x.Category == ElementCategory.Door || x.Category == ElementCategory.WallOpening) && x.SourceHandles.Any(selected.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static OpeningLocation ReadOpeningLocation(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement opening)
        {
            var ids = ResolveLiveIds(document, transaction, opening.SourceHandles);
            if (ids.Count != 1) throw new InvalidOperationException("Opening cần đúng một live CAD source để tự xác định host.");
            var entity = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Entity;
            if (entity == null || entity.IsErased) throw new InvalidOperationException("Opening source không còn tồn tại.");
            var extents = entity.GeometricExtents;
            var units = CadUnitService.GetPolicy(document);
            var usesLevelPlacement = CadElementVerticalPlacement.HasAnyLevelConfiguration(opening);
            var verticalPlacement = usesLevelPlacement
                ? CadElementVerticalPlacement.Resolve(
                    document,
                    project,
                    opening,
                    project.FindFamily(opening.FamilyId),
                    extents.MinPoint.Z,
                    "HeightM",
                    2.2d,
                    "SillHeightM",
                    opening.Category == ElementCategory.Door ? 0d : 0.9d)
                : null;
            return new OpeningLocation
            {
                Plan = new Point2(
                    units.ToMeters(Midpoint(extents.MinPoint.X, extents.MaxPoint.X)),
                    units.ToMeters(Midpoint(extents.MinPoint.Y, extents.MaxPoint.Y))),
                ReferenceElevationM = verticalPlacement?.BottomElevationM ?? units.ToMeters(extents.MinPoint.Z),
                TopElevationM = verticalPlacement?.TopElevationM,
                UsesLevelPlacement = usesLevelPlacement
            };
        }

        private static IReadOnlyList<OpeningHostSegment> ReadHostSegments(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement opening,
            OpeningLocation openingLocation,
            double elevationToleranceM,
            double sagittaM)
        {
            var result = new List<OpeningHostSegment>();
            var units = CadUnitService.GetPolicy(document);
            foreach (var wall in project.Elements.Where(x => IsWall(x.Category) && ScopeCompatible(opening, x)))
            {
                var family = project.FindFamily(wall.FamilyId);
                var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(wall, family, "ThicknessM", 0.2d), wall.Id + "/ThicknessM");
                var hostUsesLevelPlacement = CadElementVerticalPlacement.HasAnyLevelConfiguration(wall);
                foreach (var id in ResolveLiveIds(document, transaction, wall.SourceHandles))
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    if (entity is Line line)
                    {
                        var startZM = units.ToMeters(line.StartPoint.Z);
                        var endZM = units.ToMeters(line.EndPoint.Z);
                        if (Math.Abs(startZM - endZM) > elevationToleranceM) continue;
                        var lineHostPlacement = openingLocation.UsesLevelPlacement || hostUsesLevelPlacement
                            ? CadElementVerticalPlacement.Resolve(
                                document,
                                project,
                                wall,
                                family,
                                line.StartPoint.Z,
                                "HeightM",
                                3.6d)
                            : null;
                        if (!VerticalMatch(openingLocation, lineHostPlacement, Midpoint(startZM, endZM), elevationToleranceM)) continue;
                        result.Add(new OpeningHostSegment(wall.Id,
                            new Point2(units.ToMeters(line.StartPoint.X), units.ToMeters(line.StartPoint.Y)),
                            new Point2(units.ToMeters(line.EndPoint.X), units.ToMeters(line.EndPoint.Y)), thicknessM));
                        continue;
                    }
                    if (!(entity is Polyline polyline) || polyline.Closed || polyline.NumberOfVertices < 2) continue;
                    var normal = polyline.Normal;
                    if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || normal.Z < 1d - 1e-9d) continue;
                    var pathHostPlacement = openingLocation.UsesLevelPlacement || hostUsesLevelPlacement
                        ? CadElementVerticalPlacement.Resolve(
                            document,
                            project,
                            wall,
                            family,
                            polyline.Elevation,
                            "HeightM",
                            3.6d)
                        : null;
                    if (!VerticalMatch(openingLocation, pathHostPlacement, units.ToMeters(polyline.Elevation), elevationToleranceM)) continue;
                    for (var index = 0; index < polyline.NumberOfVertices - 1; index++)
                    {
                        var a = polyline.GetPoint2dAt(index);
                        var b = polyline.GetPoint2dAt(index + 1);
                        var start = new Point2(units.ToMeters(a.X), units.ToMeters(a.Y));
                        var end = new Point2(units.ToMeters(b.X), units.ToMeters(b.Y));
                        var bulge = CadGeometryGuard.Finite(polyline.GetBulgeAt(index), wall.Id + "/bulge");
                        IReadOnlyList<Point2> points = Math.Abs(bulge) <= 1e-12d
                            ? new[] { start, end }
                            : BulgeArcTessellator.Tessellate(start, end, bulge, sagittaM);
                        for (var part = 1; part < points.Count; part++)
                            result.Add(new OpeningHostSegment(wall.Id, points[part - 1], points[part], thicknessM));
                    }
                }
            }
            return result.AsReadOnly();
        }

        private static bool VerticalMatch(
            OpeningLocation opening,
            CadElementVerticalPlacement? host,
            double legacyHostElevationM,
            double toleranceM)
        {
            if (opening.UsesLevelPlacement)
            {
                if (host == null || !opening.TopElevationM.HasValue)
                    throw new InvalidOperationException("Level-aware Auto Host requires complete opening and host vertical ranges.");
                return opening.ReferenceElevationM >= host.BottomElevationM - toleranceM &&
                       opening.TopElevationM.Value <= host.TopElevationM + toleranceM;
            }

            var hostBottomM = host?.BottomElevationM ?? legacyHostElevationM;
            return Math.Abs(hostBottomM - opening.ReferenceElevationM) <= toleranceM;
        }

        private static IReadOnlyList<ObjectId> ResolveLiveIds(Document document, Transaction transaction, IEnumerable<string> handles)
        {
            var result = new List<ObjectId>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in handles)
            {
                var text = (raw ?? string.Empty).Trim();
                if (text.Length == 0 || !seen.Add(text) || !long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;
                try
                {
                    var id = document.Database.GetObjectId(false, new Handle(value), 0);
                    if (id.IsNull || !id.IsValid) continue;
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity != null && !entity.IsErased) result.Add(id);
                }
                catch { }
            }
            return result.AsReadOnly();
        }

        private static bool ScopeCompatible(ProjectElement opening, ProjectElement wall)
        {
            if (!string.IsNullOrWhiteSpace(opening.FloorId) && !string.Equals(opening.FloorId, wall.FloorId, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(opening.ZoneId) && !string.Equals(opening.ZoneId, wall.ZoneId, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static bool IsWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier ||
            category == ElementCategory.StructuralWall;

        private static bool HasCanonicalHostLink(ProjectElement opening, string hostId)
        {
            if (!opening.Properties.TryGetValue("HostWallId", out var rawHostId) ||
                !string.Equals(rawHostId, hostId, StringComparison.Ordinal))
                return false;

            var matches = 0;
            foreach (var dependency in opening.DependsOn)
            {
                if (!string.Equals((dependency ?? string.Empty).Trim(), hostId, StringComparison.OrdinalIgnoreCase)) continue;
                matches++;
                if (!string.Equals(dependency, hostId, StringComparison.Ordinal)) return false;
            }
            return matches == 1;
        }

        private static bool UpdateAutoHostMetadata(ProjectElement opening, double gapM)
        {
            var gap = gapM.ToString("R", CultureInfo.InvariantCulture);
            var gapChanged = !opening.Properties.TryGetValue("AutoHostGapM", out var existingGap) ||
                !string.Equals(existingGap, gap, StringComparison.Ordinal);
            var matchedChanged = !opening.Properties.TryGetValue("AutoHostMatched", out var existingMatched) ||
                !string.Equals(existingMatched, "true", StringComparison.OrdinalIgnoreCase);
            if (!gapChanged && !matchedChanged) return false;

            opening.Properties["AutoHostGapM"] = gap;
            opening.Properties["AutoHostMatched"] = "true";
            return true;
        }

        private static double MetadataNumber(ProjectState project, string key, double fallback, bool allowZero)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || (allowZero ? value < 0d : value <= 0d))
                throw new InvalidOperationException(key + " không hợp lệ: " + raw);
            return value;
        }

        private static double Midpoint(double left, double right)
        {
            if (double.IsNaN(left) || double.IsInfinity(left) || double.IsNaN(right) || double.IsInfinity(right)) throw new InvalidOperationException("Midpoint input chứa giá trị không hữu hạn.");
            var value = left / 2d + right / 2d;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException("Midpoint overflowed.");
            return value;
        }

        private static int Regenerate(ProjectState project, IEnumerable<string> elementIds) =>
            new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirtySubset(project, elementIds);
    }
}
