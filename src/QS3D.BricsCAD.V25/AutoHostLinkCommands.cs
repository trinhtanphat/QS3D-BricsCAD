using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class AutoHostLinkCommands
    {
        private sealed class PlannedLink
        {
            public ProjectElement Opening { get; set; } = null!;
            public string HostId { get; set; } = string.Empty;
            public double GapM { get; set; }
        }

        [CommandMethod("QS3DAUTOLINKHOSTS", CommandFlags.UsePickSet)]
        public void AutoLinkHosts()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var selected = ReadSelectedHandles(document);
                if (selected.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DAUTOLINKHOSTS: chọn Cửa/Lỗ Mở đã được QS3D capture.");
                    return;
                }

                var openings = project.Elements
                    .Where(x => (x.Category == ElementCategory.Door || x.Category == ElementCategory.WallOpening) && x.SourceHandles.Any(selected.Contains))
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (openings.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DAUTOLINKHOSTS: selection không chứa semantic Cửa/Lỗ Mở.");
                    return;
                }

                var maxGapM = MetadataNumber(project, "AutoHostMaxGapM", 0.25d, allowZero: true);
                var ambiguityM = MetadataNumber(project, "AutoHostAmbiguityM", 0.02d, allowZero: true);
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
                            var center = ReadOpeningCenter(document, transaction, opening);
                            var candidates = ReadHostSegments(document, transaction, project, opening, sagittaM);
                            var result = matcher.Match(center, candidates, maxGapM, ambiguityM);
                            if (result.Status == OpeningHostMatchStatus.Ambiguous)
                            {
                                ambiguous++;
                                document.Editor.WriteMessage("\n  " + opening.Id + ": ambiguous " + result.HostElementId + " / " + result.SecondaryHostElementId + " (gap " + result.GapM.ToString("0.###", CultureInfo.InvariantCulture) + " / " + result.SecondaryGapM.ToString("0.###", CultureInfo.InvariantCulture) + " m)");
                                continue;
                            }
                            if (result.Status != OpeningHostMatchStatus.Matched)
                            {
                                unmatched++;
                                document.Editor.WriteMessage("\n  " + opening.Id + ": không có host trong phạm vi " + maxGapM.ToString("0.###", CultureInfo.InvariantCulture) + " m.");
                                continue;
                            }
                            planned.Add(new PlannedLink { Opening = opening, HostId = result.HostElementId, GapM = result.GapM });
                        }
                        catch (Exception ex)
                        {
                            invalid++;
                            document.Editor.WriteMessage("\n  " + opening.Id + ": bỏ qua — " + ex.Message);
                        }
                    }
                    transaction.Commit();
                }

                var service = new HostLinkService();
                var linked = 0;
                var unchanged = 0;
                foreach (var item in planned)
                {
                    var existing = item.Opening.Properties.TryGetValue("HostWallId", out var hostId) ? hostId : string.Empty;
                    if (string.Equals(existing?.Trim(), item.HostId, StringComparison.OrdinalIgnoreCase))
                    {
                        unchanged++;
                        continue;
                    }
                    service.LinkOpening(project, item.Opening.Id, item.HostId);
                    item.Opening.Properties["AutoHostGapM"] = item.GapM.ToString("R", CultureInfo.InvariantCulture);
                    item.Opening.Properties["AutoHostMatched"] = "true";
                    linked++;
                }

                var regenerated = linked > 0 ? Regenerate(project) : 0;
                PaletteCoordinator.RefreshProject();
                var summary = "Auto Host: linked=" + linked + " • unchanged=" + unchanged + " • ambiguous=" + ambiguous + " • unmatched=" + unmatched + " • invalid=" + invalid;
                if (regenerated > 0) summary += " • regen=" + regenerated;
                PaletteCoordinator.SetStatus(summary);
                document.Editor.WriteMessage("\nQS3D " + summary + ". Chạy QS3DCUTOPENINGS khi muốn áp physical boolean.");
            }
            catch (Exception ex)
            {
                var message = "QS3DAUTOLINKHOSTS lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static HashSet<string> ReadSelectedHandles(Document document)
        {
            var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
            return new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
        }

        private static Point2 ReadOpeningCenter(Document document, Transaction transaction, ProjectElement opening)
        {
            var ids = CadHandleService.Resolve(document, opening.SourceHandles);
            if (ids.Count != 1) throw new InvalidOperationException("Opening cần đúng một live CAD source để tự xác định host.");
            var entity = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Entity;
            if (entity == null || entity.IsErased) throw new InvalidOperationException("Opening source không còn tồn tại.");
            var extents = entity.GeometricExtents;
            var units = CadUnitService.GetPolicy(document);
            return new Point2(
                units.ToMeters(Midpoint(extents.MinPoint.X, extents.MaxPoint.X)),
                units.ToMeters(Midpoint(extents.MinPoint.Y, extents.MaxPoint.Y)));
        }

        private static IReadOnlyList<OpeningHostSegment> ReadHostSegments(Document document, Transaction transaction, ProjectState project, ProjectElement opening, double sagittaM)
        {
            var result = new List<OpeningHostSegment>();
            var units = CadUnitService.GetPolicy(document);
            foreach (var wall in project.Elements.Where(x => IsWall(x.Category) && ScopeCompatible(opening, x)))
            {
                var family = project.FindFamily(wall.FamilyId);
                var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(wall, family, "ThicknessM", 0.2d), wall.Id + "/ThicknessM");
                foreach (var id in CadHandleService.Resolve(document, wall.SourceHandles))
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    if (entity is Line line)
                    {
                        result.Add(new OpeningHostSegment(wall.Id,
                            new Point2(units.ToMeters(line.StartPoint.X), units.ToMeters(line.StartPoint.Y)),
                            new Point2(units.ToMeters(line.EndPoint.X), units.ToMeters(line.EndPoint.Y)), thicknessM));
                        continue;
                    }
                    if (!(entity is Polyline polyline) || polyline.Closed || polyline.NumberOfVertices < 2) continue;
                    var normal = polyline.Normal;
                    if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || normal.Z < 1d - 1e-9d) continue;
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

        private static double MetadataNumber(ProjectState project, string key, double fallback, bool allowZero)
        {
            if (!project.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || (allowZero ? value < 0d : value <= 0d))
                throw new InvalidOperationException(key + " không hợp lệ: " + raw);
            return value;
        }

        private static double Midpoint(double left, double right)
        {
            if (double.IsNaN(left) || double.IsInfinity(left) || double.IsNaN(right) || double.IsInfinity(right)) throw new InvalidOperationException("Entity extents chứa tọa độ không hữu hạn.");
            var value = left / 2d + right / 2d;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException("Entity extent midpoint overflowed.");
            return value;
        }

        private static int Regenerate(ProjectState project) => new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
    }
}
