using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Mep;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Units;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class MepTbqCommands
    {
        private const string DefaultRegion = "DRAWING";
        private const int MaxReportRows = 200;
        private static MepRecognitionProfile RecognitionProfile => MepRecognitionProfileProvider.Current;

        [CommandMethod("QS3DMEPTBQIMPORT", CommandFlags.UsePickSet)]
        public void ImportSelectionIntoTbq()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ExistingProjectMutationContext.Require(document, "MEP -> TBQ Import");
                var workspace = ProjectTbqWorkspace.Open(project);
                var current = workspace.Current ?? throw new InvalidOperationException(
                    "MEP -> TBQ Import requires an existing project-bound TBQ workspace; it will not invent one.");
                ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "MEP -> TBQ Import");

                var captured = CaptureSelection(document, out var skipped);
                if (captured.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DMEPTBQIMPORT: no clearly recognized MEP entities; project data was not changed.");
                    return;
                }

                var groups = new MepQuantityService().Aggregate(captured);
                var projection = new MepTbqProjectionService().Project(current, groups);
                var snapshot = ProjectStateSnapshot.Capture(project);
                string path;
                try
                {
                    workspace.Replace(projection.State);
                    path = ProjectContextCoordinator.Save(document);
                }
                catch (Exception saveFailure)
                {
                    try
                    {
                        snapshot.Restore(project);
                    }
                    catch (Exception rollbackFailure)
                    {
                        ProjectContextCoordinator.Forget(document);
                        throw new InvalidOperationException(
                            "MEP -> TBQ Import failed to save and the in-memory snapshot could not be restored. Reload the project.",
                            new AggregateException(saveFailure, rollbackFailure));
                    }
                    ProjectContextCoordinator.Forget(document);
                    throw new InvalidOperationException(
                        "MEP -> TBQ Import was rolled back because canonical project save failed; reload before retrying.",
                        saveFailure);
                }

                document.Editor.WriteMessage(
                    "\nQS3DMEPTBQIMPORT: recognized=" + captured.Count.ToString(CultureInfo.InvariantCulture) +
                    " • skipped=" + skipped.ToString(CultureInfo.InvariantCulture) +
                    " • groups=" + groups.Count.ToString(CultureInfo.InvariantCulture) +
                    " • MEP BQ rows=" + projection.ProjectedBillItemCount.ToString(CultureInfo.InvariantCulture) +
                    " • saved=" + path + ".");
            }
            catch (Exception ex)
            {
                WriteFailure(document, "QS3DMEPTBQIMPORT", ex);
            }
        }

        [CommandMethod("QS3DMEPTBQREPORT", CommandFlags.UsePickSet)]
        public void ReportSelection()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var captured = CaptureSelection(document, out var skipped);
                if (captured.Count == 0)
                {
                    document.Editor.WriteMessage("\nQS3DMEPTBQREPORT: no clearly recognized MEP entities; skipped=" + skipped + ".");
                    return;
                }

                var groups = new MepQuantityService().Aggregate(captured);
                var rows = new MepTbqProjectionService().BuildReport(groups);
                document.Editor.WriteMessage(
                    "\nQS3DMEPTBQREPORT: recognized=" + captured.Count + " • groups=" + rows.Count + " • skipped=" + skipped +
                    (rows.Count > MaxReportRows ? " • showing first " + MaxReportRows + "." : "."));
                for (var i = 0; i < rows.Count && i < MaxReportRows; i++) WriteReportRow(document, rows[i]);
            }
            catch (Exception ex)
            {
                WriteFailure(document, "QS3DMEPTBQREPORT", ex);
            }
        }

        private static IReadOnlyList<MepElement> CaptureSelection(Document document, out int skipped)
        {
            var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
            var units = CadUnitService.GetPolicy(document);
            var captured = new List<MepElement>();
            skipped = 0;
            for (var i = 0; i < snapshots.Count; i++)
            {
                if (TryCreateMepElement(snapshots[i], units, out var element)) captured.Add(element);
                else skipped++;
            }
            return captured;
        }

        private static bool TryCreateMepElement(EntitySnapshot snapshot, ProjectUnitPolicy units, out MepElement element)
        {
            snapshot.Metadata.TryGetValue("BlockName", out var blockName);
            var recognition = RecognitionProfile.Recognize(snapshot.Layer, blockName);
            if (recognition.Status != MepRecognitionStatus.Matched ||
                recognition.Discipline != MepRecognitionDiscipline.Mep ||
                !recognition.MepKind.HasValue)
            {
                element = null!;
                return false;
            }

            var kind = recognition.MepKind.Value;
            var lengthM = snapshot.LengthDrawingUnits.HasValue ? units.ToMeters(snapshot.LengthDrawingUnits.Value) : 0d;
            var areaSource = snapshot.SurfaceAreaDrawingUnitsSquared ?? snapshot.AreaDrawingUnitsSquared;
            var areaM2 = areaSource.HasValue ? units.AreaToSquareMeters(areaSource.Value) : 0d;
            var volumeM3 = snapshot.VolumeDrawingUnitsCubed.HasValue ? units.VolumeToCubicMeters(snapshot.VolumeDrawingUnitsCubed.Value) : 0d;
            var system = CanonicalOrFallback(snapshot.Layer, recognition.Category ?? kind.ToString());
            var specification = SnapshotSpecification(snapshot);
            element = new MepElement(snapshot.Handle, kind, system, specification, DefaultRegion, 1, lengthM, areaM2, volumeM3);
            return true;
        }

        private static string SnapshotSpecification(EntitySnapshot snapshot)
        {
            if (snapshot.Metadata.TryGetValue("BlockName", out var blockName) && !string.IsNullOrWhiteSpace(blockName)) return blockName.Trim();
            return CanonicalOrFallback(snapshot.EntityType, "Entity");
        }

        private static string CanonicalOrFallback(string? value, string fallback)
        {
            var text = (value ?? string.Empty).Trim();
            return text.Length == 0 ? fallback : text;
        }

        private static void WriteReportRow(Document document, MepTbqReportRow row)
        {
            document.Editor.WriteMessage(
                "\n  " + row.Region + " • " + row.System + " • " + row.Specification + " • " + row.Kind +
                " • entities=" + row.ElementCount + " • count=" + row.QuantityCount +
                " • L=" + Format(row.LengthM) + " m • A=" + Format(row.AreaM2) + " m² • V=" + Format(row.VolumeM3) + " m³");
        }

        private static string Format(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);

        private static void WriteFailure(Document document, string commandName, Exception error)
        {
            try { document.Editor.WriteMessage("\n" + commandName + " error: " + error.Message); }
            catch { }
        }
    }
}
