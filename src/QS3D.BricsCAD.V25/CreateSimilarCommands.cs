using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// BLT-style Create Similar entry points. The selected QS3D source/generated object is used
    /// only to resolve an existing semantic owner and Family; actual authoring remains delegated
    /// to the existing Active Family Quick/Advanced Direct Draw workflow.
    /// </summary>
    public sealed class CreateSimilarCommands
    {
        [CommandMethod("QS3DCREATESIMILAR", CommandFlags.Modal)]
        public void CreateSimilar() => CreateSimilarCore(advanced: false, operation: "QS3DCREATESIMILAR");

        [CommandMethod("QS3DCREATESIMILARADV", CommandFlags.Modal)]
        public void CreateSimilarAdvanced() => CreateSimilarCore(advanced: true, operation: "QS3DCREATESIMILARADV");

        private static void CreateSimilarCore(bool advanced, string operation)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                // Selection is completed before any canonical mutation bind. ESC therefore cannot
                // create/cache-bind a project or change Active Family merely by invoking Create Similar.
                var selectedHandle = PromptEntityHandle(document, advanced
                    ? "\nChọn cấu kiện QS3D làm mẫu cho Vẽ Tương Tự tùy chỉnh: "
                    : "\nChọn cấu kiện QS3D làm mẫu cho Vẽ Tương Tự: ");
                if (selectedHandle == null) return;

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                    throw new InvalidOperationException(operation + ": bản vẽ chưa có QS3D project hiện hữu; lệnh không tạo project mới.");

                var previewOwner = ResolveOwner(previewProject, selectedHandle);
                var previewFamily = ResolveFamily(previewProject, previewOwner.Element);
                if (!ActiveFamilyQuickDrawCommands.SupportsFamily(previewFamily))
                    throw new InvalidOperationException(
                        operation + ": Family mẫu '" + previewFamily.Name + "' thuộc " + previewFamily.Category +
                        " chưa có Direct Draw Quick/Advanced an toàn. Active Family chưa bị thay đổi.");

                // Freeze primitives, not mutable ProjectState/ProjectElement/ProjectFamily references.
                // A cached read-only probe can expose the canonical object, which modeless UI may mutate in-place.
                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var expectedOwnerId = previewOwner.Element.Id;
                var expectedFamilyId = previewFamily.Id;
                var expectedCategory = previewFamily.Category;
                var expectedOwnerKind = previewOwner.Kind;
                var expectedOwnerSlot = previewOwner.OwnerSlot;

                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                    throw new InvalidOperationException(operation + ": DWG active đã thay đổi sau khi chọn mẫu. Hãy chạy lại trên bản vẽ hiện hành.");

                var project = ExistingProjectMutationContext.Require(document, operation);
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException(
                        operation + ": QS3D project đã thay đổi sau khi chọn mẫu. Không đổi Active Family; hãy chạy lại lệnh.");

                var currentOwner = ResolveOwner(project, selectedHandle);
                if (!string.Equals(currentOwner.Element.Id, expectedOwnerId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(currentOwner.Kind, expectedOwnerKind, StringComparison.Ordinal) ||
                    !string.Equals(currentOwner.OwnerSlot, expectedOwnerSlot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        operation + ": ownership của đối tượng mẫu đã thay đổi trước khi kích hoạt Family. Hãy chọn lại mẫu.");

                var currentFamily = ResolveFamily(project, currentOwner.Element);
                if (!string.Equals(currentFamily.Id, expectedFamilyId, StringComparison.OrdinalIgnoreCase) ||
                    currentFamily.Category != expectedCategory ||
                    currentOwner.Element.Category != expectedCategory)
                    throw new InvalidOperationException(
                        operation + ": Family/category của cấu kiện mẫu đã thay đổi. Không dispatch từ trạng thái stale.");
                if (!ActiveFamilyQuickDrawCommands.SupportsFamily(currentFamily))
                    throw new InvalidOperationException(
                        operation + ": Family mẫu hiện tại không còn thuộc Direct Draw Quick/Advanced được hỗ trợ.");

                // This is the only intentional semantic mutation owned by Create Similar. Geometry,
                // prompts, preview freshness, source creation, regeneration and rollback remain owned
                // by ActiveFamilyQuickDrawCommands and its existing category-specific target command.
                ProjectFamilyActivationService.SetActive(project, currentFamily.Id);
                Report(
                    document,
                    (advanced ? "Vẽ Tương Tự tùy chỉnh → " : "Vẽ Tương Tự → ") +
                    currentFamily.Name + " • " + currentFamily.Category +
                    (string.Equals(currentOwner.Kind, "generated", StringComparison.Ordinal) ? " • từ generated owner" : " • từ semantic source"));

                var dispatcher = new ActiveFamilyQuickDrawCommands();
                if (advanced) dispatcher.DrawActiveFamilyAdvanced();
                else dispatcher.DrawActiveFamily();
            }
            catch (Exception ex)
            {
                Report(document, operation + " lỗi: " + ex.Message);
            }
        }

        private static string? PromptEntityHandle(Document document, string message)
        {
            var result = document.Editor.GetEntity(new PromptEntityOptions(message));
            if (result.Status != PromptStatus.OK) return null;
            return result.ObjectId.Handle.ToString();
        }

        private static OwnerResolution ResolveOwner(ProjectState project, string handle)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = (handle ?? string.Empty).Trim();
            if (normalized.Length == 0)
                throw new InvalidOperationException("Đối tượng mẫu không có CAD handle hợp lệ.");

            // The shared generated-ownership policy validates duplicate semantic IDs and throws on
            // ambiguous generated claims. Do not reproduce Generated*Handle parsing here.
            var candidates = new List<OwnerResolution>();
            if (GeneratedHandleOwnershipPolicy.TryFindOwner(project, normalized, out var generatedOwner, out var generatedSlot) &&
                generatedOwner != null)
            {
                candidates.Add(new OwnerResolution(
                    generatedOwner,
                    "generated",
                    GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(generatedSlot)));
            }

            foreach (var element in project.Elements)
            {
                if (!element.SourceHandles.Any(source =>
                        string.Equals((source ?? string.Empty).Trim(), normalized, StringComparison.OrdinalIgnoreCase))) continue;
                if (candidates.Any(candidate => string.Equals(candidate.Element.Id, element.Id, StringComparison.OrdinalIgnoreCase)))
                    continue;
                candidates.Add(new OwnerResolution(element, "source", string.Empty));
                if (candidates.Count > 1) break;
            }

            if (candidates.Count == 0)
                throw new InvalidOperationException(
                    "Đối tượng được chọn không phải semantic source hoặc QS3D-generated output có owner xác định.");
            if (candidates.Count != 1)
                throw new InvalidOperationException(
                    "Đối tượng được chọn khớp nhiều semantic owner; Create Similar đã fail-closed để tránh dùng nhầm Family.");
            return candidates[0];
        }

        private static ProjectFamily ResolveFamily(ProjectState project, ProjectElement element)
        {
            var familyId = (element.FamilyId ?? string.Empty).Trim();
            if (familyId.Length == 0)
                throw new InvalidOperationException("Cấu kiện mẫu " + element.Id + " chưa gắn Family / Type.");
            var family = project.FindFamily(familyId)
                ?? throw new InvalidOperationException("Cấu kiện mẫu tham chiếu Family không còn tồn tại: " + familyId + ".");
            if (family.Category != element.Category)
                throw new InvalidOperationException(
                    "Cấu kiện mẫu và Family khác category (" + element.Category + " / " + family.Category + "); hãy sửa semantic data trước.");
            return family;
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + message); } catch { }
        }

        private readonly struct OwnerResolution
        {
            public OwnerResolution(ProjectElement element, string kind, string ownerSlot)
            {
                Element = element ?? throw new ArgumentNullException(nameof(element));
                Kind = kind ?? string.Empty;
                OwnerSlot = ownerSlot ?? string.Empty;
            }

            public ProjectElement Element { get; }
            public string Kind { get; }
            public string OwnerSlot { get; }
        }
    }
}
