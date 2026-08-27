using System;
using System.Globalization;
using System.IO;
using System.Text;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Export;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectInterchangeImportCommands
    {
        private enum CollisionPolicyChoice
        {
            KeepTarget = 0,
            UseSourceElement = 1,
            UseSourceCatalog = 2,
            UseSourceAll = 3
        }

        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        [CommandMethod("QS3DINTERCHANGEIMPORT", CommandFlags.Modal)]
        public void ImportSemanticSnapshotWithPolicy()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Nạp QS3D Semantic Snapshot — chọn policy theo collision",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var json = ReadGuardedSnapshotText(dialog.FileName);
                ProjectInterchangeValidatedSnapshotReader.Read(json);
                EnsureActive(document, "Interchange Import / preview");
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var previewChangeVersion = project.ChangeVersion;
                var preview = ProjectInterchangeImportPreview.Plan(project, json);
                if (!preview.Validation.IsValid)
                    throw new InvalidDataException("Snapshot không vượt qua strict validation/import preview.");

                var keepPlan = ProjectInterchangeKeepTargetImporter.Plan(project, json);
                if (preview.CollisionCount == 0)
                {
                    RunAppendOnly(document, project, previewChangeVersion, json);
                    return;
                }

                InterchangeUseSourceElementImportPlan? elementPlan = null;
                string elementBlock = string.Empty;
                try
                {
                    var candidate = InterchangeUseSourceElementImportService.Plan(project, json);
                    if (candidate.ElementsToReplace > 0) elementPlan = candidate;
                }
                catch (Exception ex)
                {
                    elementBlock = ex.Message;
                }

                InterchangeUseSourceCatalogImportPlan? catalogPlan = null;
                string catalogBlock = string.Empty;
                try
                {
                    var candidate = InterchangeUseSourceCatalogImportService.Plan(project, json);
                    if (candidate.ZonesToReplace + candidate.FloorsToReplace + candidate.FamiliesToReplace > 0)
                        catalogPlan = candidate;
                }
                catch (Exception ex)
                {
                    catalogBlock = ex.Message;
                }

                InterchangeUseSourceAllImportPlan? allPlan = null;
                string allBlock = string.Empty;
                try
                {
                    var candidate = InterchangeUseSourceAllImportService.Plan(project, json);
                    if (candidate.ZonesToReplace + candidate.FloorsToReplace + candidate.FamiliesToReplace + candidate.ElementsToReplace > 0)
                        allPlan = candidate;
                }
                catch (Exception ex)
                {
                    allBlock = ex.Message;
                }

                var choice = ChooseCollisionPolicy(
                    preview,
                    keepPlan,
                    elementPlan,
                    catalogPlan,
                    allPlan,
                    elementBlock,
                    catalogBlock,
                    allBlock);
                if (!choice.HasValue) return;

                var confirmedProject = InterchangeConfirmationGuard.RequireFresh(
                    document,
                    project,
                    previewChangeVersion,
                    "Interchange Import policy");

                switch (choice.Value)
                {
                    case CollisionPolicyChoice.KeepTarget:
                        RunKeepTarget(document, confirmedProject, json);
                        return;
                    case CollisionPolicyChoice.UseSourceElement:
                        RunUseSourceElement(document, confirmedProject, json);
                        return;
                    case CollisionPolicyChoice.UseSourceCatalog:
                        RunUseSourceCatalog(document, confirmedProject, json);
                        return;
                    case CollisionPolicyChoice.UseSourceAll:
                        RunUseSourceAll(document, confirmedProject, json);
                        return;
                    default:
                        throw new InvalidOperationException("Unsupported interchange collision policy choice.");
                }
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEIMPORT lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage("\nQS3DINTERCHANGEIMPORT error: " + ex.Message + " Import policy không được claim thành công nếu apply chưa hoàn tất.");
            }
        }

        private static CollisionPolicyChoice? ChooseCollisionPolicy(
            ProjectInterchangeImportPreviewResult preview,
            ProjectInterchangeKeepTargetImportPlan keepPlan,
            InterchangeUseSourceElementImportPlan? elementPlan,
            InterchangeUseSourceCatalogImportPlan? catalogPlan,
            InterchangeUseSourceAllImportPlan? allPlan,
            string elementBlock,
            string catalogBlock,
            string allBlock)
        {
            if (elementPlan == null && catalogPlan == null)
            {
                var keepOnly =
                    "Snapshot có " + preview.CollisionCount.ToString(CultureInfo.InvariantCulture) + " semantic ID collision(s), nhưng không có executable partial UseSource replacement.\n\n" +
                    "Policy khả dụng hiện tại: KEEP TARGET.\n" +
                    "Target identity trùng ID giữ nguyên; chỉ semantic identity mới được thêm. Incoming source handles bị discard.\n" +
                    BlockText("Element UseSource", elementBlock) +
                    BlockText("Catalog UseSource", catalogBlock) +
                    BlockText("ALL UseSource", allBlock) +
                    "\nTiếp tục KeepTarget?";
                return System.Windows.MessageBox.Show(
                           keepOnly,
                           "QS3D — Interchange Import / KeepTarget",
                           System.Windows.MessageBoxButton.YesNo,
                           System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes
                    ? CollisionPolicyChoice.KeepTarget
                    : (CollisionPolicyChoice?)null;
            }

            if (elementPlan != null && catalogPlan == null)
            {
                var choice = System.Windows.MessageBox.Show(
                    ElementVsKeepText(preview, keepPlan, elementPlan, catalogBlock),
                    "QS3D — Interchange Import Policy",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Warning);
                if (choice == System.Windows.MessageBoxResult.Cancel) return null;
                return choice == System.Windows.MessageBoxResult.Yes
                    ? CollisionPolicyChoice.UseSourceElement
                    : CollisionPolicyChoice.KeepTarget;
            }

            if (elementPlan == null && catalogPlan != null)
            {
                var choice = System.Windows.MessageBox.Show(
                    CatalogVsKeepText(preview, keepPlan, catalogPlan, elementBlock),
                    "QS3D — Interchange Import Policy",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Warning);
                if (choice == System.Windows.MessageBoxResult.Cancel) return null;
                return choice == System.Windows.MessageBoxResult.Yes
                    ? CollisionPolicyChoice.UseSourceCatalog
                    : CollisionPolicyChoice.KeepTarget;
            }

            var first = System.Windows.MessageBox.Show(
                "Snapshot có " + preview.CollisionCount.ToString(CultureInfo.InvariantCulture) + " semantic ID collision(s).\n\n" +
                "YES — chọn USE SOURCE policy ở bước tiếp theo.\n" +
                "NO — KEEP TARGET cho toàn bộ collisions (" + keepPlan.TotalSemanticIdentitiesToKeep.ToString(CultureInfo.InvariantCulture) + ").\n" +
                "CANCEL — không import.\n\n" +
                "Incoming source CAD handles không trở thành target ownership. Provenance retention là authorization riêng.",
                "QS3D — Interchange Import Policy",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Warning);
            if (first == System.Windows.MessageBoxResult.Cancel) return null;
            if (first == System.Windows.MessageBoxResult.No) return CollisionPolicyChoice.KeepTarget;

            if (allPlan != null)
            {
                var allOrPartial = System.Windows.MessageBox.Show(
                    "Chọn phạm vi USE SOURCE:\n\n" +
                    "YES — REPLACE ALL SEMANTIC (ATOMIC)\n" +
                    "• Zone: " + allPlan.ZonesToReplace.ToString(CultureInfo.InvariantCulture) +
                    " • Floor: " + allPlan.FloorsToReplace.ToString(CultureInfo.InvariantCulture) +
                    " • Family: " + allPlan.FamiliesToReplace.ToString(CultureInfo.InvariantCulture) +
                    " • Element: " + allPlan.ElementsToReplace.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "• Catalog + Element replacement dùng MỘT ProjectStateSnapshot và MỘT native CAD transaction.\n" +
                    "• Union generated-output closure invalidated ownership-safely; target source handles/fingerprint vẫn giữ nguyên.\n\n" +
                    "NO — chọn PARTIAL scope (Element hoặc Catalog) ở bước tiếp theo.\n" +
                    "CANCEL — không import.\n\n" +
                    "ALL không sequentially chạy hai importer partial, nên không tạo split transaction/partial-commit window.",
                    "QS3D — UseSource ALL hay Partial",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Warning);
                if (allOrPartial == System.Windows.MessageBoxResult.Cancel) return null;
                if (allOrPartial == System.Windows.MessageBoxResult.Yes) return CollisionPolicyChoice.UseSourceAll;
            }
            else if (!string.IsNullOrWhiteSpace(allBlock))
            {
                var continuePartial = System.Windows.MessageBox.Show(
                    "UseSource ALL một-transaction bị chặn:\n" + allBlock +
                    "\n\nHai partial policy vẫn có thể chọn riêng. Tiếp tục chọn partial?",
                    "QS3D — UseSource ALL unavailable",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (continuePartial != System.Windows.MessageBoxResult.Yes) return null;
            }

            var partial = System.Windows.MessageBox.Show(
                "Chọn PARTIAL USE SOURCE policy:\n\n" +
                "YES — REPLACE ELEMENT SEMANTIC\n" +
                "• Element same-category collisions: " + elementPlan!.ElementsToReplace.ToString(CultureInfo.InvariantCulture) + "\n" +
                "• Zone/Floor/Family collisions giữ target\n" +
                "• target SourceHandles/drawing fingerprint giữ nguyên\n\n" +
                "NO — REPLACE CATALOG SEMANTIC\n" +
                "• Zone: " + catalogPlan!.ZonesToReplace.ToString(CultureInfo.InvariantCulture) +
                " • Floor: " + catalogPlan.FloorsToReplace.ToString(CultureInfo.InvariantCulture) +
                " • Family: " + catalogPlan.FamiliesToReplace.ToString(CultureInfo.InvariantCulture) + "\n" +
                "• Element collisions giữ target\n\n" +
                "CANCEL — không import.\n\n" +
                "Mỗi partial path có transaction riêng của chính nó; selector chỉ chạy đúng một path được chọn và không sequence hai partial importer.",
                "QS3D — Chọn partial UseSource scope",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Warning);
            if (partial == System.Windows.MessageBoxResult.Cancel) return null;
            return partial == System.Windows.MessageBoxResult.Yes
                ? CollisionPolicyChoice.UseSourceElement
                : CollisionPolicyChoice.UseSourceCatalog;
        }

        private static string ElementVsKeepText(
            ProjectInterchangeImportPreviewResult preview,
            ProjectInterchangeKeepTargetImportPlan keepPlan,
            InterchangeUseSourceElementImportPlan plan,
            string catalogBlock)
        {
            return
                "Snapshot có " + preview.CollisionCount.ToString(CultureInfo.InvariantCulture) + " semantic ID collision(s). Chọn policy:\n\n" +
                "YES — REPLACE ELEMENT SEMANTIC\n" +
                "• replace same-category Element collisions: " + plan.ElementsToReplace.ToString(CultureInfo.InvariantCulture) + "\n" +
                "• giữ Zone/Floor/Family collision của target\n" +
                "• giữ target SourceHandles/drawing fingerprint\n" +
                "• xóa ownership-safe generated outputs của affected closure; rebuild explicit\n\n" +
                "NO — KEEP TARGET\n" +
                "• giữ toàn bộ target identities trùng ID: " + keepPlan.TotalSemanticIdentitiesToKeep.ToString(CultureInfo.InvariantCulture) + "\n" +
                "• chỉ append identities mới\n\n" +
                "CANCEL — không import." + BlockText("Catalog UseSource", catalogBlock) +
                "\nIncoming source CAD handles bị discard; không tự lưu .qsdb.";
        }

        private static string CatalogVsKeepText(
            ProjectInterchangeImportPreviewResult preview,
            ProjectInterchangeKeepTargetImportPlan keepPlan,
            InterchangeUseSourceCatalogImportPlan plan,
            string elementBlock)
        {
            return
                "Snapshot có " + preview.CollisionCount.ToString(CultureInfo.InvariantCulture) + " semantic ID collision(s). Chọn policy:\n\n" +
                "YES — REPLACE CATALOG SEMANTIC\n" +
                "• Zone: " + plan.ZonesToReplace.ToString(CultureInfo.InvariantCulture) +
                " • Floor: " + plan.FloorsToReplace.ToString(CultureInfo.InvariantCulture) +
                " • Family: " + plan.FamiliesToReplace.ToString(CultureInfo.InvariantCulture) + "\n" +
                "• Element collisions giữ target\n" +
                "• invalidates referencing elements/dependents in CAD transaction; rebuild explicit\n\n" +
                "NO — KEEP TARGET\n" +
                "• giữ toàn bộ target identities trùng ID: " + keepPlan.TotalSemanticIdentitiesToKeep.ToString(CultureInfo.InvariantCulture) + "\n" +
                "• chỉ append identities mới\n\n" +
                "CANCEL — không import." + BlockText("Element UseSource", elementBlock) +
                "\nIncoming source CAD handles bị discard; không tự lưu .qsdb.";
        }

        private static string BlockText(string label, string reason) =>
            string.IsNullOrWhiteSpace(reason) ? string.Empty : "\n\n" + label + " bị chặn: " + reason;

        private static void RunAppendOnly(
            Document document,
            QS3D.Core.Domain.ProjectState project,
            long reviewedChangeVersion,
            string json)
        {
            var appendPlan = ProjectInterchangeAppendOnlyImporter.Plan(project, json);
            var appendConfirm =
                "Snapshot không có semantic ID collision. Chạy APPEND-ONLY?\n\n" +
                "Source project: " + appendPlan.SourceProjectId + "\n" +
                "Semantic identity mới: " + appendPlan.TotalSemanticIdentitiesToAdd.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Incoming source handles discard: " + appendPlan.SourceHandlesToDiscard.ToString(CultureInfo.InvariantCulture) + "\n\n" +
                "Không merge/replace, không nhận CAD ownership từ source, không tự lưu .qsdb.";
            if (System.Windows.MessageBox.Show(
                    appendConfirm,
                    "QS3D — Interchange Import / Append-only",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes) return;

            var currentProject = InterchangeConfirmationGuard.RequireFresh(
                document,
                project,
                reviewedChangeVersion,
                "Interchange Import / Append-only");
            var result = ProjectInterchangeAppendOnlyImporter.Import(currentProject, json);
            FinishSemanticOnlyImport(
                document,
                "Interchange Import / Append-only: semantic +" +
                (result.ZonesAdded + result.FloorsAdded + result.FamiliesAdded + result.ElementsAdded).ToString(CultureInfo.InvariantCulture) +
                " • source handles discarded " + result.SourceHandlesDiscarded.ToString(CultureInfo.InvariantCulture) +
                ". Chưa tự lưu .qsdb.");
        }

        private static void RunKeepTarget(Document document, QS3D.Core.Domain.ProjectState project, string json)
        {
            EnsureActive(document, "Interchange KeepTarget import");
            var result = ProjectInterchangeKeepTargetImporter.Import(project, json);
            FinishSemanticOnlyImport(
                document,
                "Interchange Import / KeepTarget: semantic +" +
                (result.ZonesAdded + result.FloorsAdded + result.FamiliesAdded + result.ElementsAdded).ToString(CultureInfo.InvariantCulture) +
                " • target identities kept " + result.TargetIdentitiesKept.ToString(CultureInfo.InvariantCulture) +
                " • source handles discarded " + result.SourceHandlesDiscarded.ToString(CultureInfo.InvariantCulture) +
                ". Chưa tự lưu .qsdb.");
        }

        private static void RunUseSourceElement(
            Document document,
            QS3D.Core.Domain.ProjectState confirmedProject,
            string json)
        {
            EnsureActive(document, "Interchange UseSource element import");
            var result = InterchangeUseSourceElementImportService.Import(document, confirmedProject, json);
            var status =
                "Interchange Import / UseSource Element: replaced " + result.ElementsReplaced.ToString(CultureInfo.InvariantCulture) +
                " • Element +" + result.ElementsAdded.ToString(CultureInfo.InvariantCulture) +
                " • catalog +" + (result.ZonesAdded + result.FloorsAdded + result.FamiliesAdded).ToString(CultureInfo.InvariantCulture) +
                " • generated closure invalidated " + result.GeneratedElementsInvalidated.ToString(CultureInfo.InvariantCulture) +
                ". Rebuild explicit; chưa tự lưu .qsdb.";
            FinishSemanticOnlyImport(document, status);
        }

        private static void RunUseSourceCatalog(
            Document document,
            QS3D.Core.Domain.ProjectState confirmedProject,
            string json)
        {
            EnsureActive(document, "Interchange UseSource catalog import");
            var result = InterchangeUseSourceCatalogImportService.Import(document, confirmedProject, json);
            var status =
                "Interchange Import / UseSource Catalog: Zone " + result.ZonesReplaced.ToString(CultureInfo.InvariantCulture) +
                " • Floor " + result.FloorsReplaced.ToString(CultureInfo.InvariantCulture) +
                " • Family " + result.FamiliesReplaced.ToString(CultureInfo.InvariantCulture) +
                " replaced • Element collisions kept " + result.ElementCollisionsKept.ToString(CultureInfo.InvariantCulture) +
                " • generated closure invalidated " + result.GeneratedElementsInvalidated.ToString(CultureInfo.InvariantCulture) +
                ". Rebuild explicit; chưa tự lưu .qsdb.";
            FinishSemanticOnlyImport(document, status);
        }

        private static void RunUseSourceAll(
            Document document,
            QS3D.Core.Domain.ProjectState confirmedProject,
            string json)
        {
            EnsureActive(document, "Interchange UseSource all-scope import");
            var result = InterchangeUseSourceAllImportService.Import(document, confirmedProject, json);
            var status =
                "Interchange Import / UseSource ALL: Zone " + result.ZonesReplaced.ToString(CultureInfo.InvariantCulture) +
                " • Floor " + result.FloorsReplaced.ToString(CultureInfo.InvariantCulture) +
                " • Family " + result.FamiliesReplaced.ToString(CultureInfo.InvariantCulture) +
                " • Element " + result.ElementsReplaced.ToString(CultureInfo.InvariantCulture) +
                " replaced in one CAD transaction • generated closure invalidated " + result.GeneratedElementsInvalidated.ToString(CultureInfo.InvariantCulture) +
                ". Rebuild explicit; chưa tự lưu .qsdb.";
            FinishSemanticOnlyImport(document, status);
        }

        private static void FinishSemanticOnlyImport(Document document, string status)
        {
            InterchangePostMutationUi.RefreshProjectFailClosed(document);
            try { PaletteCoordinator.SetStatus(status); } catch { }
            document.Editor.WriteMessage("\nQS3D " + status);
        }

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " requires the DWG that started the operation to remain active.");
        }

        private static string ReadGuardedSnapshotText(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Interchange snapshot path is required.", nameof(path));
            var fullPath = Path.GetFullPath(path);
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > ProjectInterchangeJsonValidator.MaxFileBytes)
                    throw new InvalidDataException("Semantic snapshot exceeds the guarded " + ProjectInterchangeJsonValidator.MaxFileBytes.ToString(CultureInfo.InvariantCulture) + " byte limit.");
                var length = checked((int)stream.Length);
                var bytes = new byte[length];
                var offset = 0;
                while (offset < bytes.Length)
                {
                    var read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read <= 0) throw new EndOfStreamException("Semantic snapshot changed or ended while it was being read.");
                    offset += read;
                }
                if (stream.ReadByte() != -1)
                    throw new InvalidDataException("Semantic snapshot changed while it was being read; reopen the file and retry.");
                try { return StrictUtf8.GetString(bytes); }
                catch (DecoderFallbackException ex) { throw new InvalidDataException("Semantic snapshot is not valid UTF-8.", ex); }
            }
        }
    }
}
