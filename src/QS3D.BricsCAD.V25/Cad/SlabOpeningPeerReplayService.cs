using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class SlabOpeningPeerReplayService
    {
        public static IReadOnlyList<string> CaptureAppliedOpeningIds(
            ProjectState project,
            ProjectElement host,
            string retiringSolidHandle)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (host.Category != ElementCategory.Slab || string.IsNullOrWhiteSpace(retiringSolidHandle))
                return Array.Empty<string>();

            var normalized = retiringSolidHandle.Trim();
            return project.Elements
                .Where(element =>
                    SlabOpeningContract.IsSlabOpening(project, element) &&
                    SlabOpeningContract.TryGetHostSlabId(element, out var hostId) &&
                    string.Equals(hostId, host.Id, StringComparison.OrdinalIgnoreCase) &&
                    element.Properties.TryGetValue(SlabOpeningContract.AppliedSolidHandleKey, out var appliedHandle) &&
                    string.Equals((appliedHandle ?? string.Empty).Trim(), normalized, StringComparison.OrdinalIgnoreCase))
                .Select(element => element.Id)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static int ReplayAppliedOpenings(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement host,
            Polyline hostSource,
            Solid3d hostSolid,
            string retiringSolidHandle,
            IReadOnlyList<string> openingIds)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (hostSource == null) throw new ArgumentNullException(nameof(hostSource));
            if (hostSolid == null) throw new ArgumentNullException(nameof(hostSolid));
            if (openingIds == null) throw new ArgumentNullException(nameof(openingIds));
            if (openingIds.Count == 0) return 0;
            if (host.Category != ElementCategory.Slab)
                throw new InvalidOperationException("slabOpen replay host must remain a semantic Slab: " + host.Id);
            if (hostSource.IsErased || !hostSource.Closed)
                throw new InvalidOperationException("slabOpen replay host source must remain one live closed POLYLINE: " + host.Id);
            if (hostSolid.IsErased)
                throw new InvalidOperationException("slabOpen replay host Solid3d was erased before peer replay: " + host.Id);

            GeneratedGeometryService.RequireMatchingOwnership(
                hostSolid,
                project,
                host,
                "replay slabOpen peers into rebuilt Slab " + host.Id);

            var currentSolidHandle = hostSolid.Handle.ToString();
            if (!host.Properties.TryGetValue("GeneratedSolidHandle", out var semanticHandle) ||
                !string.Equals((semanticHandle ?? string.Empty).Trim(), currentSolidHandle, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Rebuilt Slab semantic/generated handle mismatch before slabOpen peer replay: " + host.Id);

            var hostFamily = project.FindFamily(host.FamilyId);
            var hostPlacement = CadElementVerticalPlacement.Resolve(
                document,
                project,
                host,
                hostFamily,
                hostSource.Elevation,
                "ThicknessM",
                0.12d);
            var replayed = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var openingId in openingIds)
            {
                if (string.IsNullOrWhiteSpace(openingId) || !seen.Add(openingId.Trim()))
                    throw new InvalidOperationException("slabOpen peer replay contains a blank or duplicate opening identity for host " + host.Id + ".");

                var opening = project.FindElement(openingId.Trim())
                    ?? throw new InvalidOperationException("Previously applied slabOpen disappeared before host rebuild replay: " + openingId);
                if (!SlabOpeningContract.IsSlabOpening(project, opening))
                    throw new InvalidOperationException("Previously applied peer is no longer an exact slabOpen: " + opening.Id);
                var linkedHostId = SlabOpeningContract.RequireHostSlabId(opening);
                if (!string.Equals(linkedHostId, host.Id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Previously applied slabOpen changed host before rebuild replay: " + opening.Id);
                if (!opening.Properties.TryGetValue(SlabOpeningContract.AppliedSolidHandleKey, out var appliedHandle) ||
                    !string.Equals((appliedHandle ?? string.Empty).Trim(), (retiringSolidHandle ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Previously applied slabOpen no longer proves the retiring host Solid3d: " + opening.Id);

                var openingSourceId = ResolveExactlyOne(document, transaction, opening.SourceHandles, "slabOpen replay source " + opening.Id);
                var openingSource = transaction.GetObject(openingSourceId, OpenMode.ForRead, false) as Polyline;
                if (openingSource == null || openingSource.IsErased || !openingSource.Closed)
                    throw new InvalidOperationException("slabOpen replay source must be one live closed POLYLINE: " + opening.Id);

                var openingFamily = project.FindFamily(opening.FamilyId);
                var clearanceM = CadGeometryGuard.Positive(
                    CadGeometryGuard.Number(opening, openingFamily, SlabOpeningContract.BooleanClearanceMKey, 0.01d),
                    opening.Id + "/" + SlabOpeningContract.BooleanClearanceMKey);
                var plan = SlabOpeningCutPlanner.Plan(new SlabOpeningCutInput
                {
                    HostBottomM = hostPlacement.BottomElevationM,
                    HostThicknessM = hostPlacement.HeightM,
                    ClearanceM = clearanceM
                });
                var sourceGeometryFingerprint = PolylineFingerprint(openingSource);
                var fingerprint = Fingerprint(
                    opening,
                    host,
                    currentSolidHandle,
                    openingSourceId.Handle.ToString(),
                    sourceGeometryFingerprint,
                    plan);

                var cutterTopDrawing = CadGeometryGuard.ToDrawingUnits(document, plan.CutterTopM, opening.Id + "/replay cutter top");
                var sourceLiftDrawing = CadGeometryGuard.Subtract(
                    cutterTopDrawing,
                    openingSource.Elevation,
                    opening.Id + "/replay cutter top lift");
                var extrusionDrawing = CadGeometryGuard.ToDrawingUnits(document, plan.ExtrusionZM, opening.Id + "/replay negative-Z extrusion");
                if (!(extrusionDrawing < 0d))
                    throw new InvalidOperationException("slabOpen replay cutter extrusion must remain negative in drawing units: " + opening.Id);

                using (var cutterProfile = (Polyline)openingSource.Clone())
                using (var cutter = new Solid3d())
                {
                    cutterProfile.TransformBy(Matrix3d.Displacement(new Vector3d(0d, 0d, sourceLiftDrawing)));
                    cutter.SetDatabaseDefaults(document.Database);
                    cutter.CreateExtrudedSolid(cutterProfile, new Vector3d(0d, 0d, extrusionDrawing), new SweepOptions());
                    hostSolid.BooleanOperation(BooleanOperationType.BoolSubtract, cutter);
                }

                opening.Properties[SlabOpeningContract.AppliedSolidHandleKey] = currentSolidHandle;
                opening.Properties[SlabOpeningContract.AppliedFingerprintKey] = fingerprint;
                replayed++;
            }

            if (replayed != openingIds.Count)
                throw new InvalidOperationException("Not every previously applied slabOpen was replayed onto rebuilt Slab " + host.Id + ".");

            host.Properties["SlabOpeningCutCount"] = CountAppliedOpenings(project, host.Id, currentSolidHandle)
                .ToString(CultureInfo.InvariantCulture);
            return replayed;
        }

        private static ObjectId ResolveExactlyOne(
            Document document,
            Transaction transaction,
            IEnumerable<string> handles,
            string label)
        {
            var ids = new List<ObjectId>();
            var seen = new HashSet<ObjectId>();
            foreach (var handleText in handles ?? Array.Empty<string>())
            {
                var normalized = CadHandleService.NormalizeHexHandle(handleText);
                if (normalized == null || !long.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                    continue;
                ObjectId id;
                try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                catch { continue; }
                if (id.IsNull || !id.IsValid || !seen.Add(id)) continue;
                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                if (entity != null && !entity.IsErased) ids.Add(id);
            }

            if (ids.Count != 1)
                throw new InvalidOperationException(label + " must resolve to exactly one live CAD entity; resolved " + ids.Count + ".");
            return ids[0];
        }

        private static int CountAppliedOpenings(ProjectState project, string hostId, string solidHandle)
        {
            return project.Elements.Count(element =>
                SlabOpeningContract.IsSlabOpening(project, element) &&
                SlabOpeningContract.TryGetHostSlabId(element, out var linkedHostId) &&
                string.Equals(linkedHostId, hostId, StringComparison.OrdinalIgnoreCase) &&
                element.Properties.TryGetValue(SlabOpeningContract.AppliedSolidHandleKey, out var appliedHandle) &&
                string.Equals((appliedHandle ?? string.Empty).Trim(), solidHandle, StringComparison.OrdinalIgnoreCase));
        }

        private static string PolylineFingerprint(Polyline polyline)
        {
            var parts = new List<string>
            {
                "polyline-v1",
                polyline.Closed ? "closed" : "open",
                polyline.NumberOfVertices.ToString(CultureInfo.InvariantCulture),
                Number(polyline.Elevation),
                Number(polyline.Normal.X),
                Number(polyline.Normal.Y),
                Number(polyline.Normal.Z)
            };
            for (var index = 0; index < polyline.NumberOfVertices; index++)
            {
                var point = polyline.GetPoint2dAt(index);
                parts.Add(index.ToString(CultureInfo.InvariantCulture) + ":" + Number(point.X) + ":" + Number(point.Y) + ":" + Number(polyline.GetBulgeAt(index)));
            }
            return string.Join(";", parts);
        }

        private static string Fingerprint(
            ProjectElement opening,
            ProjectElement host,
            string solidHandle,
            string sourceHandle,
            string sourceGeometryFingerprint,
            SlabOpeningCutPlan plan)
        {
            return string.Join("|", new[]
            {
                "slabOpen-v2",
                opening.Id,
                host.Id,
                solidHandle,
                sourceHandle,
                sourceGeometryFingerprint,
                Number(plan.CutterTopM),
                Number(plan.CutterBottomM),
                Number(plan.CutterHeightM),
                Number(plan.ExtrusionZM)
            });
        }

        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
