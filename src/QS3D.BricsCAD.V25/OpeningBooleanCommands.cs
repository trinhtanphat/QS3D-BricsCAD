using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class OpeningBooleanCommands
    {
        [CommandMethod("QS3DCUTOPENINGS", CommandFlags.Modal)]
        public void CutOpenings()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            Execute(document, null, "QS3DCUTOPENINGS", "Physical opening");
        }

        [CommandMethod("QS3DCUTSELECTEDOPENINGS", CommandFlags.UsePickSet)]
        public void CutSelectedOpenings()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0)
                {
                    FinalizeUi(document, "Physical opening chọn: chưa có CAD/semantic selection.");
                    return;
                }

                var handles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
                var openingIds = project.Elements
                    .Where(IsOpening)
                    .Where(x => SemanticReferenceHandles.MatchesSelection(x, handles))
                    .Select(x => x.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (openingIds.Count == 0)
                {
                    FinalizeUi(document, "Physical opening chọn: selection không resolve tới Door/WallOpening QS3D.");
                    return;
                }

                Execute(document, openingIds, "QS3DCUTSELECTEDOPENINGS", "Physical opening chọn");
            }
            catch (Exception ex)
            {
                FinalizeError(document, "QS3DCUTSELECTEDOPENINGS", ex);
            }
        }

        private static void Execute(Document document, IReadOnlyCollection<string>? openingIds, string operation, string label)
        {
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                if (openingIds == null)
                    OpeningBooleanCutGuard.RequireFreshGeneratedHosts(project, null);
                else
                    OpeningBooleanCutGuard.RequireSelectedTargetsReady(document, project, openingIds);

                var count = openingIds == null
                    ? OpeningBooleanService.CutLinkedOpenings(document, project)
                    : OpeningBooleanService.CutLinkedOpenings(document, project, openingIds);

                var liveNote = string.Empty;
                try
                {
                    var stamped = PhysicalOpeningCutLiveStateService.StampStraight(document, project, openingIds);
                    if (stamped > 0) liveNote = " • live-fingerprint=" + stamped;
                }
                catch (Exception stampError)
                {
                    liveNote = " • cảnh báo live-health metadata: " + stampError.Message;
                }

                var message = count == 0
                    ? openingIds == null
                        ? label + ": không có linked opening mới cần khoét, host chưa có generated solid tương thích hoặc fingerprint hiện tại đã khớp."
                        : label + ": target set đã ở đúng physical-cut fingerprint; không cần khoét lại."
                    : openingIds == null
                        ? label + ": đã khoét " + count + " Cửa/Lỗ Mở vào generated host solid."
                        : label + ": đã thực hiện " + count + " phép khoét mới; target có fingerprint đã khớp được giữ nguyên.";
                FinalizeUi(document, message + liveNote);
            }
            catch (Exception ex)
            {
                FinalizeError(document, operation, ex);
            }
        }

        private static bool IsOpening(ProjectElement element) =>
            element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door;

        private static void FinalizeUi(Document document, string message)
        {
            try { PaletteCoordinator.RefreshProject(); }
            catch { }
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
            TryWriteMessage(document, "\nQS3D " + message);
        }

        private static void FinalizeError(Document document, string operation, Exception ex)
        {
            var message = operation + " lỗi: " + ex.Message;
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
            TryWriteMessage(document, "\n" + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); }
            catch { }
        }
    }
}
