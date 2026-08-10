using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ColumnTieQuantityCommands
    {
        [CommandMethod("QS3DREBARTIEQTY", CommandFlags.UsePickSet)]
        public void CalculateSelectedColumnTies()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var selection = document.Editor.SelectImplied();
                if (selection.Status != PromptStatus.OK || selection.Value == null)
                {
                    selection = document.Editor.GetSelection();
                    if (selection.Status != PromptStatus.OK || selection.Value == null) return;
                }
                var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var id in selection.Value.GetObjectIds())
                {
                    try { selected.Add(id.Handle.ToString()); }
                    catch { }
                }
                if (selected.Count == 0) return;

                var project = ProjectContextCoordinator.GetOrCreate(document);
                var targets = project.Elements
                    .Where(x => x.Category == ElementCategory.Column && x.SourceHandles.Any(selected.Contains))
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (targets.Count == 0)
                {
                    PaletteCoordinator.SetStatus("Tie QTY: selection không chứa Column semantic.");
                    return;
                }

                var snapshot = ProjectStateSnapshot.Capture(project);
                try
                {
                    foreach (var element in targets)
                    {
                        var quantity = ColumnTieProjectQuantityService.Calculate(element, project.FindFamily(element.FamilyId));
                        element.Quantities["TieRebarCount"] = quantity.Count;
                        element.Quantities["TieRebarCutLengthM"] = quantity.CuttingLengthPerTieM;
                        element.Quantities["TieRebarTotalLengthM"] = quantity.TotalLengthM;
                        element.Quantities["TieRebarKgPerM"] = quantity.KgPerMeter;
                        element.Quantities["TieRebarWeightKg"] = quantity.TotalWeightKg;
                        AuditTrail.ForProject(project).Record("quantity.rebar.column.tie", element.Id,
                            "count=" + quantity.Count.ToString(CultureInfo.InvariantCulture) +
                            ";cutLengthM=" + quantity.CuttingLengthPerTieM.ToString("R", CultureInfo.InvariantCulture) +
                            ";totalLengthM=" + quantity.TotalLengthM.ToString("R", CultureInfo.InvariantCulture) +
                            ";weightKg=" + quantity.TotalWeightKg.ToString("R", CultureInfo.InvariantCulture));
                    }
                    project.Touch();
                }
                catch
                {
                    snapshot.Restore(project);
                    throw;
                }

                PaletteCoordinator.RefreshProject();
                var message = "Tie QTY: đã cập nhật " + targets.Count.ToString(CultureInfo.InvariantCulture) + " Column.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                var message = "QS3DREBARTIEQTY lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
