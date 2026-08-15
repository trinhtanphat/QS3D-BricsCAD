using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class SlabOpeningBooleanService
    {
        public static int CutLinkedOpening(Document document, ProjectState project, ProjectElement opening)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (opening == null) throw new ArgumentNullException(nameof(opening));
            if (!SlabOpeningContract.IsSlabOpening(project, opening))
                throw new InvalidOperationException("Target element is not an exact slabOpen semantic opening.");

            var hostId = SlabOpeningContract.RequireHostSlabId(opening);
            var host = project.FindElement(hostId) ?? throw new InvalidOperationException("slabOpen host Slab not found: " + hostId);
            if (host.Category != ElementCategory.Slab)
                throw new InvalidOperationException("slabOpen HostSlabId must resolve to a semantic Slab: " + hostId);
            if (host.IsGeneratedSolidStale())
                throw new InvalidOperationException("Host Slab " + host.Id + " has stale generated geometry. Build 3D again before slabOpen subtraction.");
            if (!host.Properties.TryGetValue("GeneratedSolidHandle", out var generatedHandle) || string.IsNullOrWhiteSpace(generatedHandle))
                throw new InvalidOperationException("Host Slab " + host.Id + " has no generated Solid3d. Build 3D before slabOpen subtraction.");

            var openingSourceId = ResolveExactlyOne(document, opening.SourceHandles, "slabOpen source " + opening.Id);
            var hostSourceId = ResolveExactlyOne(document, host.SourceHandles, "Slab source " + host.Id);
            var hostSolidId = ResolveExactlyOne(document, new[] { generatedHandle }, "generated Slab solid " + host.Id);
            var currentSolidHandle = hostSolidId.Handle.ToString();
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;

            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var openingSource = transaction.GetObject(openingSourceId, OpenMode.ForRead, false) as Polyline;
                    if (openingSource == null || openingSource.IsErased || !openingSource.Closed)
                        throw new InvalidOperationException("slabOpen source must be one live closed POLYLINE: " + opening.Id);
                    var hostSource = transaction.GetObject(hostSourceId, OpenMode.ForRead, false) as Polyline;
                    if (hostSource == null || hostSource.IsErased || !hostSource.Closed)
                        throw new InvalidOperationException("Host Slab source must be one live closed POLYLINE: " + host.Id);
                    var hostSolid = transaction.GetObject(hostSolidId, OpenMode.ForWrite, false) as Solid3d;
                    if (hostSolid == null || hostSolid.IsErased)
                        throw new InvalidOperationException("Generated Slab handle is not a live Solid3d: " + currentSolidHandle);

                    GeneratedGeometryService.RequireMatchingOwnership(
                        hostSolid,
                        project,
                        host,
                        "slabOpen boolean host solid " + currentSolidHandle);

                    var hostFamily = project.FindFamily(host.FamilyId);
                    var hostPlacement = CadElementVerticalPlacement.Resolve(
                        document,
                        project,
                        host,
                        hostFamily,
                        hostSource.Elevation,
                        "ThicknessM",
                        0.12d);
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

                    if (opening.Properties.TryGetValue(SlabOpeningContract.AppliedSolidHandleKey, out var appliedHandle) &&
                        !string.IsNullOrWhiteSpace(appliedHandle) &&
                        string.Equals(appliedHandle.Trim(), currentSolidHandle, StringComparison.OrdinalIgnoreCase))
                    {
                        if (opening.Properties.TryGetValue(SlabOpeningContract.AppliedFingerprintKey, out var appliedFingerprint) &&
                            string.Equals(appliedFingerprint, fingerprint, StringComparison.Ordinal))
                        {
                            transaction.Commit();
                            cadCommitted = true;
                            return 0;
                        }

                        throw new InvalidOperationException(
                            "slabOpen " + opening.Id + " changed after it was subtracted from the current Slab solid. " +
                            "Build 3D host Slab again before applying the changed opening.");
                    }

                    var cutterTopDrawing = CadGeometryGuard.ToDrawingUnits(document, plan.CutterTopM, opening.Id + "/cutter top");
                    var sourceLiftDrawing = CadGeometryGuard.Subtract(
                        cutterTopDrawing,
                        openingSource.Elevation,
                        opening.Id + "/cutter top lift");
                    var extrusionDrawing = CadGeometryGuard.ToDrawingUnits(document, plan.ExtrusionZM, opening.Id + "/negative-Z extrusion");
                    if (!(extrusionDrawing < 0d))
                        throw new InvalidOperationException("slabOpen cutter extrusion must remain negative in drawing units.");

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
                    host.Properties["SlabOpeningCutCount"] = CountAppliedOpenings(project, host.Id, currentSolidHandle)
                        .ToString(CultureInfo.InvariantCulture);
                    project.Touch();
                    transaction.Commit();
                    cadCommitted = true;
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "slabOpen Boolean subtraction failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            try { document.Editor.Regen(); }
            catch { }
            return 1;
        }

        private static ObjectId ResolveExactlyOne(Document document, IEnumerable<string> handles, string label)
        {
            var ids = CadHandleService.Resolve(document, handles).Distinct().ToList();
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
            if (polyline == null) throw new ArgumentNullException(nameof(polyline));
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
                parts.Add(
                    index.ToString(CultureInfo.InvariantCulture) + ":" +
                    Number(point.X) + ":" +
                    Number(point.Y) + ":" +
                    Number(polyline.GetBulgeAt(index)));
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

        private static string Number(double value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
    }
}
