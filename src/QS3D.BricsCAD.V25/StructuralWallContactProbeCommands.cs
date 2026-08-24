using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Reporting;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    internal sealed class StructuralWallContactProbeCommands
    {
        [CommandMethod("QS3DWALLCONTACTPROBE", CommandFlags.UsePickSet)]
        public void Probe()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Wall-contact probe requires an existing QS3D project.");

                var snapshots = EntitySnapshotReader.ReadImpliedSelection(document);
                if (snapshots.Count == 0)
                    snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0) return;

                var handles = new HashSet<string>(
                    snapshots.Select(snapshot => (snapshot.Handle ?? string.Empty).Trim())
                        .Where(handle => handle.Length > 0),
                    StringComparer.OrdinalIgnoreCase);
                var walls = project.Elements
                    .Where(element => element.Category == ElementCategory.StructuralWall)
                    .Where(element => SemanticReferenceHandles.MatchesSelection(element, handles))
                    .ToList();
                if (walls.Count != 1)
                    throw new InvalidOperationException("Wall-contact probe requires selection resolving to exactly one StructuralWall.");

                var available = StructuralWallConcreteContactService.TryMeasureM2(
                    document,
                    project,
                    walls[0],
                    out var deductionM2,
                    out var diagnostics);

                var message = string.Join(" ", new[]
                {
                    "available=" + (available ? "true" : "false"),
                    "target_solids=" + diagnostics.TargetSolidCount.ToString(CultureInfo.InvariantCulture),
                    "candidates=" + diagnostics.CandidateSolidCount.ToString(CultureInfo.InvariantCulture),
                    "face_seeds=" + diagnostics.VerticalFaceSeedCount.ToString(CultureInfo.InvariantCulture),
                    "volume_cuts=" + diagnostics.PositiveVolumeCutCount.ToString(CultureInfo.InvariantCulture),
                    "contact_cuts=" + diagnostics.ContactProbeCutCount.ToString(CultureInfo.InvariantCulture),
                    "failed_native=" + diagnostics.FailedNativeCutCount.ToString(CultureInfo.InvariantCulture),
                    "gross_m2=" + diagnostics.GrossVerticalAreaM2.ToString("R", CultureInfo.InvariantCulture),
                    "residual_m2=" + diagnostics.ResidualVerticalAreaM2.ToString("R", CultureInfo.InvariantCulture),
                    "deduction_m2=" + deductionM2.ToString("R", CultureInfo.InvariantCulture)
                });

                document.Editor.WriteMessage("\nQS3D WALLCONTACT PROBE " + message + ".");
            }
            catch (Exception error)
            {
                try { document.Editor.WriteMessage("\nQS3D WALLCONTACT PROBE ERROR: " + error.Message); } catch { }
            }
        }
    }
}
