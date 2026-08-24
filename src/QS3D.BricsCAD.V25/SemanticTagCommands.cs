using System;
using System.Collections.Generic;
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
        private const int MaxBatchSources = 256;

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

        [CommandMethod("QS3DTAGLEADER", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void PlaceSemanticMLeader()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var sourceHandle = AcquireSourceHandle(document, "\nChọn authoritative CAD source của semantic element cần MLeader: ");
                if (sourceHandle == null) return;
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                    throw new InvalidOperationException("Semantic MLeader yêu cầu QS3D project hiện hữu; lệnh không tạo project mới.");
                var previewElement = ResolveSourceElement(previewProject, sourceHandle);
                var expectedProjectId = previewProject.ProjectId;
                var expectedElementId = previewElement.Id;

                var placement = PromptLeaderPlacement(document);
                if (placement == null) return;

                var project = ExistingProjectMutationContext.Require(document, "Semantic MLeader");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("QS3D project đã thay đổi trong lúc đặt Semantic MLeader. Hãy chạy lại lệnh.");
                var element = ResolveSourceElement(project, sourceHandle);
                if (!string.Equals(element.Id, expectedElementId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Semantic source đã đổi owner trong lúc đặt MLeader. Hãy chạy lại lệnh.");

                var handle = SemanticMLeaderBuilder.Build(document, project, element, placement.Value.TargetPoint, placement.Value.TextPoint);
                FinalizeUi(document, "Semantic Tag: đã tạo/cập nhật MLeader " + handle + " cho " + element.Id + ".");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DTAGLEADER lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DTAGLEADERBATCH", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void PlaceSemanticMLeaderBatch()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                RequireSupportedUcs(document);
                var handles = AcquireSourceHandles(document);
                if (handles.Count == 0) return;
                if (handles.Count > MaxBatchSources)
                    throw new InvalidOperationException("Semantic MLeader batch supports at most " + MaxBatchSources + " selected source objects.");

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                    throw new InvalidOperationException("Semantic MLeader batch yêu cầu QS3D project hiện hữu; lệnh không tạo project mới.");
                var expectedProjectId = previewProject.ProjectId;
                var expectedElementIds = handles.Select(handle => ResolveSourceElement(previewProject, handle).Id).ToArray();
                if (expectedElementIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != expectedElementIds.Length)
                    throw new InvalidOperationException("Semantic MLeader batch selection chứa nhiều CAD source cùng map về một semantic element; P0 yêu cầu one authoritative source/element.");

                var project = ExistingProjectMutationContext.Require(document, "Semantic MLeader batch");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("QS3D project đã thay đổi trong lúc chuẩn bị Semantic MLeader batch. Hãy chạy lại lệnh.");

                var offset = CadGeometryGuard.Positive(
                    CadGeometryGuard.ToDrawingUnits(document, 0.35d, "semantic MLeader batch offset"),
                    "semantic MLeader batch offset drawing");
                var items = new List<SemanticMLeaderBatchItem>(handles.Count);
                for (var i = 0; i < handles.Count; i++)
                {
                    var element = ResolveSourceElement(project, handles[i]);
                    if (!string.Equals(element.Id, expectedElementIds[i], StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Semantic source ownership changed while preparing batch for handle " + handles[i] + ".");
                    var target = SemanticMLeaderBuilder.ReadSourceAnchor(document, handles[i]);
                    var column = i % 4;
                    var row = i / 4;
                    var text = new Point3d(
                        target.X + offset * (1.5d + column * 0.25d),
                        target.Y + offset * (0.75d + row * 0.35d),
                        target.Z);
                    items.Add(new SemanticMLeaderBatchItem(element, target, text));
                }

                if (!ConfirmBatchReplacement(document, project, items)) return;
                var generated = SemanticMLeaderBuilder.BuildBatch(document, project, items);
                FinalizeUi(document, "Semantic Tag: batch đã tạo/cập nhật " + generated.Count + " MLeader artifact(s) atomically.");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DTAGLEADERBATCH lỗi: " + ex.Message);
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
                    throw new InvalidOperationException("Element " + element.Id + " chưa có generated semantic tag. Dùng QS3DTAG/QS3DTAGLEADER để đặt tag trước.");

                var kind = Property(element, GeneratedSemanticTagHealthService.ArtifactKindKey);
                if (string.Equals(kind, GeneratedSemanticTagHealthService.MLeaderArtifactKind, StringComparison.Ordinal))
                {
                    var target = SemanticMLeaderBuilder.StoredTargetWorldPosition(element);
                    var text = SemanticMLeaderBuilder.StoredTextWorldPosition(element);
                    var leaderHandle = SemanticMLeaderBuilder.Build(document, project, element, target, text);
                    FinalizeUi(document, "Semantic Tag: đã refresh MLeader " + leaderHandle + " cho " + element.Id + " từ associative metadata.");
                    return;
                }
                if (kind.Length > 0 && !string.Equals(kind, GeneratedSemanticTagHealthService.MTextArtifactKind, StringComparison.Ordinal))
                    throw new InvalidOperationException("Unsupported GeneratedSemanticTagArtifactKind: " + kind + ".");

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

        [CommandMethod("QS3DTAGLEADERREFRESH", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void RefreshSemanticMLeader()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var sourceHandle = AcquireSourceHandle(document, "\nChọn authoritative CAD source của semantic MLeader cần refresh: ");
                if (sourceHandle == null) return;
                var project = ExistingProjectMutationContext.Require(document, "Semantic MLeader refresh");
                var element = ResolveSourceElement(project, sourceHandle);
                if (!string.Equals(Property(element, GeneratedSemanticTagHealthService.ArtifactKindKey), GeneratedSemanticTagHealthService.MLeaderArtifactKind, StringComparison.Ordinal))
                    throw new InvalidOperationException("Element " + element.Id + " chưa có source-ready semantic MLeader metadata.");
                var handle = SemanticMLeaderBuilder.Build(
                    document,
                    project,
                    element,
                    SemanticMLeaderBuilder.StoredTargetWorldPosition(element),
                    SemanticMLeaderBuilder.StoredTextWorldPosition(element));
                FinalizeUi(document, "Semantic Tag: đã refresh MLeader " + handle + " cho " + element.Id + ".");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DTAGLEADERREFRESH lỗi: " + ex.Message);
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

        private static IReadOnlyList<string> AcquireSourceHandles(Document document)
        {
            var implied = EntitySnapshotReader.ReadCurrentSelection(document);
            IEnumerable<string> rawHandles;
            if (implied.Count > 0)
            {
                rawHandles = implied.Select(x => x.Handle ?? string.Empty);
            }
            else
            {
                var selection = document.Editor.GetSelection();
                if (selection.Status != PromptStatus.OK || selection.Value == null) return Array.Empty<string>();
                rawHandles = selection.Value.GetObjectIds().Select(x => x.Handle.ToString());
            }

            var handles = rawHandles
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .Take(MaxBatchSources + 1)
                .ToArray();
            if (handles.Length > MaxBatchSources)
                throw new InvalidOperationException("Semantic MLeader batch selection exceeds " + MaxBatchSources + " unique source handles.");
            return handles;
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

        private static LeaderPlacement? PromptLeaderPlacement(Document document)
        {
            RequireSupportedUcs(document);
            var targetResult = document.Editor.GetPoint("\nChọn điểm mũi tên MLeader trên authoritative source: ");
            if (targetResult.Status != PromptStatus.OK) return null;
            var target = targetResult.Value.TransformBy(document.Editor.CurrentUserCoordinateSystem);

            var options = new PromptPointOptions("\nChọn vị trí text MLeader: ") { BasePoint = targetResult.Value, UseBasePoint = true };
            var textResult = document.Editor.GetPoint(options);
            if (textResult.Status != PromptStatus.OK) return null;
            var text = textResult.Value.TransformBy(document.Editor.CurrentUserCoordinateSystem);
            return new LeaderPlacement(target, text);
        }

        private static bool ConfirmBatchReplacement(Document document, ProjectState project, IReadOnlyList<SemanticMLeaderBatchItem> items)
        {
            var replacements = items.Count(x => x.Element.Properties.TryGetValue(GeneratedSemanticTagHealthService.HandlesKey, out var raw) && !string.IsNullOrWhiteSpace(raw));
            if (replacements == 0) return true;
            var options = new PromptKeywordOptions("\nBatch sẽ replace " + replacements + " generated semantic tag(s) sau khi validate ownership. Tiếp tục?")
            {
                AllowNone = false
            };
            options.Keywords.Add("Yes");
            options.Keywords.Add("No");
            options.Keywords.Default = "No";
            var result = document.Editor.GetKeywords(options);
            return result.Status == PromptStatus.OK && string.Equals(result.StringResult, "Yes", StringComparison.OrdinalIgnoreCase);
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

        private static string Property(ProjectElement element, string key) =>
            element.Properties.TryGetValue(key, out var raw) ? (raw ?? string.Empty).Trim() : string.Empty;

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

        private readonly struct LeaderPlacement
        {
            public LeaderPlacement(Point3d targetPoint, Point3d textPoint)
            {
                TargetPoint = targetPoint;
                TextPoint = textPoint;
            }

            public Point3d TargetPoint { get; }
            public Point3d TextPoint { get; }
        }
    }
}
