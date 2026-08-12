using System;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SemanticTagCommands
    {
        private const double UcsAxisTolerance = 1e-9d;

        [CommandMethod("QS3DTAG", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void PlaceSemanticTag()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var sourceHandle = AcquireSourceHandle(document, "\nChọn authoritative CAD source của semantic element cần tag: ");
                if (sourceHandle == null) return;

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                    throw new InvalidOperationException("Semantic Tag yêu cầu QS3D project hiện hữu; lệnh không tạo project mới.");
                var previewElement = ResolveSourceElement(previewProject, sourceHandle);
                var expectedProjectId = previewProject.ProjectId;
                var expectedElementId = previewElement.Id;

                var placement = PromptPlacement(document);
                if (placement == null) return;

                var project = ExistingProjectMutationContext.Require(document, "Semantic Tag");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("QS3D project đã thay đổi trong lúc đặt Semantic Tag. Hãy chạy lại lệnh.");
                var element = ResolveSourceElement(project, sourceHandle);
                if (!string.Equals(element.Id, expectedElementId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Semantic source đã đổi owner trong lúc đặt tag. Hãy chạy lại lệnh.");

                var handle = SemanticTagBuilder.Build(document, project, element, placement.Value.Position, placement.Value.RotationRadians);
                FinalizeUi(document, "Semantic Tag: đã tạo/cập nhật MText " + handle + " cho " + element.Id + ".");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DTAG lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DTAGREFRESH", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void RefreshSemanticTag()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var sourceHandle = AcquireSourceHandle(document, "\nChọn authoritative CAD source của semantic element cần refresh tag: ");
                if (sourceHandle == null) return;

                var project = ExistingProjectMutationContext.Require(document, "Semantic Tag refresh");
                var element = ResolveSourceElement(project, sourceHandle);
                if (!element.Properties.TryGetValue(GeneratedSemanticTagHealthService.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("Element " + element.Id + " chưa có generated semantic tag. Dùng QS3DTAG để đặt tag trước.");

                var position = SemanticTagBuilder.StoredWorldPosition(element);
                var rotation = SemanticTagBuilder.StoredRotation(element);
                var handle = SemanticTagBuilder.Build(document, project, element, position, rotation);
                FinalizeUi(document, "Semantic Tag: đã refresh MText " + handle + " cho " + element.Id + " tại vị trí đã lưu.");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DTAGREFRESH lỗi: " + ex.Message);
            }
        }

        private static string? AcquireSourceHandle(Document document, string message)
        {
            var implied = EntitySnapshotReader.ReadCurrentSelection(document);
            if (implied.Count > 1)
                throw new InvalidOperationException("Semantic Tag PICKFIRST yêu cầu chọn đúng một authoritative CAD source; bỏ bớt selection rồi chạy lại.");
            if (implied.Count == 1)
            {
                var handle = (implied[0].Handle ?? string.Empty).Trim();
                if (handle.Length == 0)
                    throw new InvalidOperationException("Semantic Tag PICKFIRST không đọc được CAD handle hợp lệ từ selection.");
                return handle;
            }

            return PromptEntityHandle(document, message);
        }

        private static string? PromptEntityHandle(Document document, string message)
        {
            var result = document.Editor.GetEntity(new PromptEntityOptions(message));
            if (result.Status != PromptStatus.OK) return null;
            return result.ObjectId.Handle.ToString();
        }

        private static ProjectElement ResolveSourceElement(ProjectState project, string handle)
        {
            var generated = GeneratedHandleOwnershipIndex.Build(project);
            if (generated.TryFindOwner(handle, out var generatedOwner, out var generatedSlot) && generatedOwner != null)
                throw new InvalidOperationException("Đối tượng chọn là QS3D-generated output của " + generatedOwner.Id + "/" + generatedSlot + ". Hãy chọn CAD source gốc.");

            var matches = project.Elements
                .Where(x => x.SourceHandles.Any(h => string.Equals((h ?? string.Empty).Trim(), handle, StringComparison.OrdinalIgnoreCase)))
                .Take(2)
                .ToList();
            if (matches.Count == 0)
                throw new InvalidOperationException("CAD source " + handle + " chưa được QS3D theo dõi. Capture/Direct Draw semantic element trước khi đặt tag.");
            if (matches.Count > 1)
                throw new InvalidOperationException("CAD source " + handle + " thuộc nhiều semantic element. Sửa source ownership trước khi đặt tag.");
            if (matches[0].SourceHandles.Count != 1)
                throw new InvalidOperationException("Semantic Tag P0 chỉ hỗ trợ element có đúng một authoritative source handle: " + matches[0].Id + ".");
            return matches[0];
        }

        private static TagPlacement? PromptPlacement(Document document)
        {
            RequireSupportedUcs(document);
            var result = document.Editor.GetPoint("\nChọn điểm đặt Semantic Tag: ");
            if (result.Status != PromptStatus.OK) return null;

            var world = result.Value.TransformBy(document.Editor.CurrentUserCoordinateSystem);
            var coordinateSystem = document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
            var xAxis = coordinateSystem.Xaxis;
            var xLength = xAxis.Length;
            if (double.IsNaN(xLength) || double.IsInfinity(xLength) || !(xLength > 0d))
                throw new InvalidOperationException("Current UCS có X axis không hợp lệ.");
            var rotation = Math.Atan2(xAxis.Y / xLength, xAxis.X / xLength);
            return new TagPlacement(world, rotation);
        }

        private static void RequireSupportedUcs(Document document)
        {
            var coordinateSystem = document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
            var zAxis = coordinateSystem.Zaxis;
            var length = zAxis.Length;
            if (double.IsNaN(length) || double.IsInfinity(length) || !(length > 0d))
                throw new InvalidOperationException("Current UCS có Z axis không hợp lệ.");
            var x = zAxis.X / length;
            var y = zAxis.Y / length;
            var z = zAxis.Z / length;
            if (Math.Abs(x) > UcsAxisTolerance || Math.Abs(y) > UcsAxisTolerance || Math.Abs(z - 1d) > UcsAxisTolerance)
                throw new InvalidOperationException("Semantic Tag P0 chỉ hỗ trợ UCS có mặt phẳng XY song song WCS XY. UCS nghiêng/3D chưa được hỗ trợ.");
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

        private readonly struct TagPlacement
        {
            public TagPlacement(Point3d position, double rotationRadians)
            {
                Position = position;
                RotationRadians = rotationRadians;
            }

            public Point3d Position { get; }
            public double RotationRadians { get; }
        }
    }
}
