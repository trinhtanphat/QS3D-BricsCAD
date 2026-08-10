using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class OpeningBooleanCutGuard
    {
        public static void RequireFreshGeneratedHosts(ProjectState project, IReadOnlyCollection<string>? openingIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var requested = openingIds == null
                ? null
                : new HashSet<string>(openingIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);

            var hostIds = project.Elements
                .Where(IsOpening)
                .Where(x => requested == null || requested.Contains(x.Id))
                .Select(x => x.Properties.TryGetValue("HostWallId", out var hostId) ? hostId : string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var hostId in hostIds)
            {
                var host = project.FindElement(hostId);
                if (host == null || !IsSupportedHost(host.Category)) continue;
                if (!host.Properties.TryGetValue("GeneratedSolidHandle", out var generatedHandle) || string.IsNullOrWhiteSpace(generatedHandle)) continue;
                if (host.IsGeneratedSolidStale())
                    throw new InvalidOperationException("Host " + host.Id + " có generated Solid3d stale. Hãy chạy QS3DBUILD3D lại host trước khi khoét Cửa/Lỗ Mở.");
            }
        }

        public static void RequireSelectedTargetsReady(Document document, ProjectState project, IReadOnlyCollection<string> openingIds)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (openingIds == null) throw new ArgumentNullException(nameof(openingIds));

            var requested = new HashSet<string>(
                openingIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            if (requested.Count == 0) throw new InvalidOperationException("Selected opening cut requires at least one Door/WallOpening id.");

            RequireFreshGeneratedHosts(project, requested);

            var hosts = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var openingId in requested.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var opening = project.FindElement(openingId) ?? throw new InvalidOperationException("Target opening not found: " + openingId);
                if (!IsOpening(opening)) throw new InvalidOperationException("Target element is not Door/WallOpening: " + openingId);
                if (!opening.Properties.TryGetValue("HostWallId", out var hostId) || string.IsNullOrWhiteSpace(hostId))
                    throw new InvalidOperationException("Target opening is not linked to a host: " + openingId);
                var host = project.FindElement(hostId) ?? throw new InvalidOperationException("Opening host not found: " + hostId);
                if (!IsSupportedHost(host.Category))
                    throw new InvalidOperationException("Target opening host category is not supported for physical cut: " + host.Id + " / " + host.Category);
                if (!host.Properties.TryGetValue("GeneratedSolidHandle", out var generatedHandle) || string.IsNullOrWhiteSpace(generatedHandle))
                    throw new InvalidOperationException("Host " + host.Id + " chưa có GeneratedSolidHandle. Hãy chạy QS3DBUILD3D trước khi khoét.");
                hosts[host.Id] = host;
            }

            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var host in hosts.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    var generatedHandle = host.Properties["GeneratedSolidHandle"].Trim();
                    var solidId = ResolveExactlyOne(document, transaction, new[] { generatedHandle }, "generated host solid " + host.Id);
                    var solid = transaction.GetObject(solidId, OpenMode.ForRead, false) as Solid3d;
                    if (solid == null || solid.IsErased)
                        throw new InvalidOperationException("GeneratedSolidHandle của host " + host.Id + " không trỏ tới live Solid3d.");
                    GeneratedGeometryService.RequireMatchingOwnership(solid, project, host, "validate physical opening cut host " + generatedHandle);

                    var sourceId = ResolveExactlyOne(document, transaction, host.SourceHandles, "host source " + host.Id);
                    var source = transaction.GetObject(sourceId, OpenMode.ForRead, false) as Entity;
                    if (source == null || source.IsErased)
                        throw new InvalidOperationException("Host source không còn live: " + host.Id);
                    if (source is Line) continue;
                    if (source is Polyline polyline)
                    {
                        if (!IsPolylineHost(host.Category))
                            throw new InvalidOperationException("Host " + host.Id + " không hỗ trợ physical cut từ POLYLINE cho category " + host.Category + ".");
                        if (polyline.Closed || polyline.NumberOfVertices < 2)
                            throw new InvalidOperationException("Physical opening cut yêu cầu wall centerline POLYLINE open có ít nhất 2 vertex: " + host.Id);
                        var normal = polyline.Normal;
                        if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || normal.Z < 1d - 1e-9d)
                            throw new InvalidOperationException("Physical opening cut yêu cầu wall POLYLINE plan-view +Z: " + host.Id);
                        for (var index = 0; index < polyline.NumberOfVertices - 1; index++)
                            if (Math.Abs(polyline.GetBulgeAt(index)) > 1e-12d)
                                throw new InvalidOperationException("Host " + host.Id + " là curved/bulged POLYLINE; dùng workflow curved opening riêng thay vì selected straight cut.");
                        continue;
                    }
                    throw new InvalidOperationException("Host source type chưa hỗ trợ physical opening cut: " + host.Id + " / " + source.GetType().Name);
                }
                transaction.Commit();
            }
        }

        private static ObjectId ResolveExactlyOne(Document document, Transaction transaction, IEnumerable<string> handles, string label)
        {
            var result = new List<ObjectId>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in handles ?? Array.Empty<string>())
            {
                var text = (raw ?? string.Empty).Trim();
                if (text.Length == 0 || !seen.Add(text) || !long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;
                ObjectId id;
                try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                catch { continue; }
                if (id.IsNull || !id.IsValid) continue;
                try
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity != null && !entity.IsErased) result.Add(id);
                }
                catch { }
            }
            if (result.Count == 0) throw new InvalidOperationException(label + " does not resolve to a live CAD object.");
            if (result.Count > 1) throw new InvalidOperationException(label + " resolves to multiple live CAD objects.");
            return result[0];
        }

        private static bool IsOpening(ProjectElement element) =>
            element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door;

        private static bool IsSupportedHost(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier ||
            category == ElementCategory.StructuralWall;

        private static bool IsPolylineHost(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier;
    }
}
