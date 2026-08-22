using System;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SemanticTagRemovalCommands
    {
        [CommandMethod("QS3DTAGREMOVE", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void RemoveSemanticTag()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var selectedHandle = AcquireTagHandle(document);
                if (selectedHandle == null) return;

                var project = ExistingProjectMutationContext.Require(document, "Semantic Tag remove");
                var element = ResolveTagOwner(project, selectedHandle);

                var erased = SemanticTagRemovalService.Remove(document, project, element);
                var message = erased > 0
                    ? "Semantic Tag: đã xóa " + erased + " MText và bỏ generated tag ownership của " + element.Id + "."
                    : "Semantic Tag: " + element.Id + " không có generated tag cần xóa.";
                FinalizeUi(document, message);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DTAGREMOVE lỗi: " + ex.Message);
            }
        }

        private static string? AcquireTagHandle(Document document)
        {
            var implied = EntitySnapshotReader.ReadCurrentSelection(document);
            if (implied.Count > 1)
                throw new InvalidOperationException("Semantic Tag Remove PICKFIRST yêu cầu chọn đúng một generated tag hoặc authoritative source; bỏ bớt selection rồi chạy lại.");
            if (implied.Count == 1)
            {
                var handle = (implied[0].Handle ?? string.Empty).Trim();
                if (handle.Length == 0)
                    throw new InvalidOperationException("Semantic Tag Remove PICKFIRST không đọc được CAD handle hợp lệ từ selection.");
                return handle;
            }

            return PromptTagHandle(document);
        }

        private static string? PromptTagHandle(Document document)
        {
            var result = document.Editor.GetEntity(new PromptEntityOptions("\nChọn Semantic Tag MText hoặc authoritative CAD source cần bỏ tag: "));
            if (result.Status != PromptStatus.OK) return null;
            return result.ObjectId.Handle.ToString();
        }

        private static ProjectElement ResolveTagOwner(ProjectState project, string handle)
        {
            var generated = GeneratedHandleOwnershipIndex.Build(project);
            if (generated.TryFindOwner(handle, out var generatedOwner, out var generatedSlot) && generatedOwner != null)
            {
                if (!string.Equals(
                        GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(generatedSlot),
                        GeneratedSemanticTagHealthService.HandlesKey,
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Generated object được chọn thuộc " + generatedOwner.Id + "/" + generatedSlot +
                        ", không phải generated semantic tag.");
                return generatedOwner;
            }

            var matches = project.Elements
                .Where(x => x.SourceHandles.Any(h => string.Equals((h ?? string.Empty).Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                .Take(2)
                .ToList();
            if (matches.Count == 0)
                throw new InvalidOperationException("Đối tượng chọn không phải generated Semantic Tag và cũng không phải semantic source đang được QS3D theo dõi: " + handle + ".");
            if (matches.Count > 1)
                throw new InvalidOperationException("CAD source " + handle + " thuộc nhiều semantic element. Sửa source ownership trước khi remove tag.");
            if (!matches[0].Properties.TryGetValue(GeneratedSemanticTagHealthService.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Semantic element " + matches[0].Id + " chưa có GeneratedSemanticTagHandles.");
            return matches[0];
        }

        private static void FinalizeUi(Document document, string message)
        {
            try
            {
                document.Editor.Regen();
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                TryWrite(document, "\nQS3D " + message + " UI sync warning: " + ex.Message);
            }
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            TryWrite(document, "\nQS3D " + message);
        }

        private static void TryWrite(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }
    }
}
