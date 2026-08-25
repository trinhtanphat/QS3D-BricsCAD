using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Export;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectInterchangeFieldMergeCommands
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        [CommandMethod("QS3DINTERCHANGEFIELDMERGE", CommandFlags.Modal)]
        public void MergeReviewedFields()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Nạp QS3D Snapshot — field-level semantic merge",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var json = ReadGuardedSnapshotText(dialog.FileName);
                ProjectInterchangeValidatedSnapshotReader.Read(json);
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var reviewedProject))
                    throw new InvalidOperationException("Interchange field merge cần một QS3D project hiện hữu; preview không tạo hoặc bind project mới.");

                if (!TryChoosePolicy(out var policy)) return;

                var reviewedPlan = InterchangeFieldMergeImportService.Plan(reviewedProject, json, policy);
                var plan = reviewedPlan.CorePlan;
                if (!plan.CanExecute)
                    throw new InvalidOperationException(BuildBlockedText(plan));

                if (plan.FieldPlan.Decisions.Count == 0)
                {
                    const string none = "Interchange FieldMerge: source/target collisions không có field semantic khác nhau để merge.";
                    try { PaletteCoordinator.SetStatus(none); } catch { }
                    document.Editor.WriteMessage("\nQS3D " + none);
                    return;
                }

                if (plan.FieldPlan.SourceChoiceCount == 0)
                {
                    const string keepOnly = "Interchange FieldMerge: mọi field khác nhau đều chọn KeepTarget; không có semantic mutation cần thực hiện.";
                    try { PaletteCoordinator.SetStatus(keepOnly); } catch { }
                    document.Editor.WriteMessage("\nQS3D " + keepOnly);
                    return;
                }

                WriteReviewToEditor(document, plan);
                var confirm = BuildConfirmation(plan);
                if (System.Windows.MessageBox.Show(
                        confirm,
                        "QS3D — Interchange Field Merge",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;

                var currentProject = ExistingProjectMutationContext.Require(document, "Interchange field merge");
                if (!string.Equals(currentProject.ProjectId, plan.TargetProjectId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(currentProject.DrawingFingerprint ?? string.Empty, plan.TargetDrawingFingerprint ?? string.Empty, StringComparison.Ordinal) ||
                    currentProject.ChangeVersion != plan.TargetChangeVersion)
                    throw new InvalidOperationException(
                        "Interchange FieldMerge target semantic project changed after preview or canonical binding normalized its identity. " +
                        "No field merge was applied; run the command again to review a fresh canonical plan.");

                var result = InterchangeFieldMergeImportService.Import(document, json, policy, reviewedPlan);
                try { PaletteCoordinator.RefreshProject(); } catch { }

                var status =
                    "Interchange FieldMerge: source fields " + result.CoreResult.SourceFieldsApplied.ToString(CultureInfo.InvariantCulture) +
                    " • target fields kept " + result.CoreResult.TargetFieldsKept.ToString(CultureInfo.InvariantCulture) +
                    " • affected elements " + result.CoreResult.AffectedTargetElementsMarkedDirty.ToString(CultureInfo.InvariantCulture) +
                    " • native invalidated " + result.GeneratedElementsInvalidated.ToString(CultureInfo.InvariantCulture) +
                    " • native rebuilt " + result.NativeGeometryRebuilt.ToString(CultureInfo.InvariantCulture) +
                    " • semantic regenerated " + result.SemanticElementsRegenerated.ToString(CultureInfo.InvariantCulture) +
                    " • authorized handles " + result.CoreResult.NativeCleanupHandlesRequired.ToString(CultureInfo.InvariantCulture) +
                    ". Native+quantity rebuild committed atomically; Workbook/Trace/export/save remain explicit.";
                try { PaletteCoordinator.SetStatus(status); } catch { }
                document.Editor.WriteMessage("\nQS3D " + status);
                document.Editor.WriteMessage("\nQS3D chạy QS3DHEALTHALL trước khi phát hành bản vẽ; không cần rebuild lại native/quantity nếu FieldMerge đã báo thành công.");
            }
            catch (Exception ex)
            {
                try { PaletteCoordinator.SetStatus("QS3DINTERCHANGEFIELDMERGE lỗi: " + ex.Message); } catch { }
                document.Editor.WriteMessage(
                    "\nQS3DINTERCHANGEFIELDMERGE error: " + ex.Message +
                    " Không claim field merge thành công nếu native/semantic apply + rebuild chưa commit.");
            }
        }

        private static bool TryChoosePolicy(out ProjectInterchangeFieldMergePolicy policy)
        {
            policy = new ProjectInterchangeFieldMergePolicy();
            if (!TryChoose("Zone / Name", out var zoneName)) return false;
            if (!TryChoose("Floor / Name", out var floorName)) return false;
            if (!TryChoose("Floor / Elevation", out var floorElevation)) return false;
            if (!TryChoose("Family / Name", out var familyName)) return false;
            if (!TryChoose("Family / Properties", out var familyProperties)) return false;
            if (!TryChoose("Element / FamilyId", out var elementFamily)) return false;
            if (!TryChoose("Element / FloorId", out var elementFloor)) return false;
            if (!TryChoose("Element / ZoneId", out var elementZone)) return false;
            if (!TryChoose("Element / Dependencies", out var elementDependencies)) return false;
            if (!TryChoose("Element / Properties", out var elementProperties)) return false;
            if (!TryChoose("Element / Quantities", out var elementQuantities)) return false;

            policy.ZoneName = zoneName;
            policy.FloorName = floorName;
            policy.FloorElevation = floorElevation;
            policy.FamilyName = familyName;
            policy.FamilyProperties = familyProperties;
            policy.ElementFamily = elementFamily;
            policy.ElementFloor = elementFloor;
            policy.ElementZone = elementZone;
            policy.ElementDependencies = elementDependencies;
            policy.ElementProperties = elementProperties;
            policy.ElementQuantities = elementQuantities;
            return true;
        }

        private static bool TryChoose(string fieldGroup, out InterchangeFieldPrecedenceChoice choice)
        {
            var answer = System.Windows.MessageBox.Show(
                "Chọn precedence cho " + fieldGroup + ":\n\n" +
                "YES — UseSource khi field thực sự khác.\n" +
                "NO — KeepTarget khi field thực sự khác.\n" +
                "CANCEL — hủy toàn bộ FieldMerge trước khi plan/mutation.",
                "QS3D — Field Merge / " + fieldGroup,
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);
            if (answer == System.Windows.MessageBoxResult.Cancel)
            {
                choice = InterchangeFieldPrecedenceChoice.Unspecified;
                return false;
            }

            choice = answer == System.Windows.MessageBoxResult.Yes
                ? InterchangeFieldPrecedenceChoice.UseSource
                : InterchangeFieldPrecedenceChoice.KeepTarget;
            return true;
        }

        private static void WriteReviewToEditor(Document document, ProjectInterchangeFieldMergeExecutionPlan plan)
        {
            document.Editor.WriteMessage(
                "\nQS3D FieldMerge REVIEW — source=" + plan.FieldPlan.SourceProjectId +
                " target=" + plan.TargetProjectId +
                " differingFields=" + plan.FieldPlan.Decisions.Count.ToString(CultureInfo.InvariantCulture) +
                " sourceChoices=" + plan.FieldPlan.SourceChoiceCount.ToString(CultureInfo.InvariantCulture) +
                " targetChoices=" + plan.FieldPlan.TargetChoiceCount.ToString(CultureInfo.InvariantCulture) + ".");

            var groups = plan.FieldPlan.Decisions
                .GroupBy(x => x.Kind + " / " + FieldGroup(x.Field) + " / " + ChoiceLabel(x.Choice), StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            foreach (var group in groups)
                document.Editor.WriteMessage(
                    "\n  " + group.Key + ": " + group.Count().ToString(CultureInfo.InvariantCulture) + " differing field(s)");

            var examples = plan.FieldPlan.Decisions.Take(12).ToArray();
            if (examples.Length > 0)
            {
                document.Editor.WriteMessage("\nQS3D FieldMerge REVIEW examples (policy is uniform per field-group; no hidden per-ID override):");
                foreach (var decision in examples)
                {
                    document.Editor.WriteMessage(
                        "\n  " + decision.Kind + " " + decision.Id + " " + decision.Field +
                        " => " + ChoiceLabel(decision.Choice) +
                        " | target=" + DisplayValue(decision.TargetHasValue, decision.TargetValue) +
                        " | source=" + DisplayValue(decision.SourceHasValue, decision.SourceValue));
                }
                if (plan.FieldPlan.Decisions.Count > examples.Length)
                    document.Editor.WriteMessage(
                        "\n  … " + (plan.FieldPlan.Decisions.Count - examples.Length).ToString(CultureInfo.InvariantCulture) +
                        " additional differing field(s); group counts above are authoritative for the reviewed policy.");
            }
        }

        private static string BuildConfirmation(ProjectInterchangeFieldMergeExecutionPlan plan)
        {
            var groups = plan.FieldPlan.Decisions
                .GroupBy(x => x.Kind + "/" + FieldGroup(x.Field) + "=" + ChoiceLabel(x.Choice), StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => "• " + x.Key + ": " + x.Count().ToString(CultureInfo.InvariantCulture))
                .ToArray();

            return
                "Áp dụng FIELD-LEVEL semantic precedence + bounded generated rebuild đã review trong MỘT CAD TRANSACTION?\n\n" +
                "Source project: " + plan.FieldPlan.SourceProjectId + "\n" +
                "Colliding identities: " + plan.FieldPlan.CollidingIdentityCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                "Differing fields: " + plan.FieldPlan.Decisions.Count.ToString(CultureInfo.InvariantCulture) +
                " • UseSource: " + plan.FieldPlan.SourceChoiceCount.ToString(CultureInfo.InvariantCulture) +
                " • KeepTarget: " + plan.FieldPlan.TargetChoiceCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                string.Join("\n", groups) + "\n\n" +
                "Affected target elements: " + plan.AffectedTargetElementIds.Count.ToString(CultureInfo.InvariantCulture) +
                " • native cleanup elements: " + plan.NativeCleanupRequirements.Count.ToString(CultureInfo.InvariantCulture) +
                " • exact target generated handles: " + plan.TargetGeneratedHandlesToClean.ToString(CultureInfo.InvariantCulture) + "\n\n" +
                "SAFETY:\n" +
                "• FieldMerge chỉ xử lý same-ID collisions; source-only identity làm plan bị block.\n" +
                "• Incoming source CAD ownership không được nhận vào target.\n" +
                "• Authorization bind ProjectId + drawing fingerprint + ChangeVersion + source snapshot + decisions + exact generated handles.\n" +
                "• Trước mutation, QS3D preflight bounded rebuild; specialized physical-cut/rebar/curtain/grid/tag/unknown owner slot sẽ fail-closed.\n" +
                "• Sau Core apply, supported GeneratedSolid được rebuild bằng production builder và affected semantic/quantity được regenerate trước CAD commit.\n" +
                "• Một native/semantic Undo transition bao trùm invalidate + apply + rebuild; lỗi trước outer commit sẽ abort DWG và rollback ProjectState.\n" +
                "• Workbook/Trace/export và save không tự chạy; các output đó vẫn explicit.";
        }

        private static string BuildBlockedText(ProjectInterchangeFieldMergeExecutionPlan plan)
        {
            var reasons = plan.FieldPlan.Blockers
                .Concat(plan.ExecutionBlockers)
                .Concat(plan.FieldPlan.Decisions
                    .Where(x => !x.IsResolved)
                    .Select(x => x.Kind + " " + x.Id + " " + x.Field + ": precedence unresolved"))
                .Take(12)
                .ToArray();
            return "FieldMerge plan bị chặn" + (reasons.Length == 0 ? "." : ": " + string.Join("; ", reasons));
        }

        private static string FieldGroup(string field)
        {
            var normalized = field ?? string.Empty;
            var dot = normalized.IndexOf('.');
            return dot > 0 ? normalized.Substring(0, dot) : normalized;
        }

        private static string ChoiceLabel(InterchangeFieldPrecedenceChoice choice) =>
            choice == InterchangeFieldPrecedenceChoice.UseSource ? "UseSource" :
            choice == InterchangeFieldPrecedenceChoice.KeepTarget ? "KeepTarget" : "Unspecified";

        private static string DisplayValue(bool hasValue, string value)
        {
            if (!hasValue) return "<missing>";
            var normalized = (value ?? string.Empty).Replace("\r", "\\r").Replace("\n", "\\n");
            const int max = 120;
            return normalized.Length <= max ? "'" + normalized + "'" :
                "'" + normalized.Substring(0, max) + "…' (len=" + normalized.Length.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private static string ReadGuardedSnapshotText(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Interchange snapshot path is required.", nameof(path));
            var fullPath = Path.GetFullPath(path);
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > ProjectInterchangeJsonValidator.MaxFileBytes)
                    throw new InvalidDataException(
                        "Semantic snapshot exceeds the guarded " +
                        ProjectInterchangeJsonValidator.MaxFileBytes.ToString(CultureInfo.InvariantCulture) + " byte limit.");
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
