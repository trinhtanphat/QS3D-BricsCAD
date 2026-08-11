using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SemanticScheduleNativeTableCommands
    {
        private const double UcsAxisTolerance = 1e-9d;

        [CommandMethod("QS3DSCHEDULETABLE", CommandFlags.Modal)]
        public void Build()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                RequireModelSpace(document);
                RequireSupportedUcs(document);
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, "Custom Schedule Table: BLOCKED • chưa có QS3D project state/sidecar; đặt Table không tạo project mới.");
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var definition = PromptDefinition(document, previewProject);
                if (definition == null) return;
                var scheduleId = definition.Id;

                var point = document.Editor.GetPoint("\nChọn điểm đặt QS3D Custom Schedule Table: ");
                if (point.Status != PromptStatus.OK) return;
                var world = point.Value.TransformBy(document.Editor.CurrentUserCoordinateSystem);

                var project = RequireExistingProject(document, "Custom Schedule Table");
                RequireFresh(project, expectedProjectId, expectedChangeVersion, "Custom Schedule Table");
                var currentDefinition = SemanticScheduleNativeTableBuilder.ResolveDefinition(project, scheduleId);
                var handle = SemanticScheduleNativeTableBuilder.Build(document, project, currentDefinition, world);
                FinalizeUi(document, "Custom Schedule Table “" + currentDefinition.Name + "”: đã tạo/cập nhật native Table " + handle + ".");
            }
            catch (Exception ex) { Report(document, "QS3DSCHEDULETABLE lỗi: " + ex.Message); }
        }

        [CommandMethod("QS3DSCHEDULETABLEREFRESH", CommandFlags.Modal)]
        public void Refresh()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                RequireModelSpace(document);
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, "Custom Schedule Table refresh: BLOCKED • chưa có QS3D project state/sidecar; refresh không tạo project mới.");
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var definition = PromptDefinition(document, previewProject);
                if (definition == null) return;
                var scheduleId = definition.Id;

                var project = RequireExistingProject(document, "Custom Schedule Table refresh");
                RequireFresh(project, expectedProjectId, expectedChangeVersion, "Custom Schedule Table refresh");
                var currentDefinition = SemanticScheduleNativeTableBuilder.ResolveDefinition(project, scheduleId);
                var position = SemanticScheduleNativeTableBuilder.StoredPosition(project, currentDefinition.Id);
                var handle = SemanticScheduleNativeTableBuilder.Build(document, project, currentDefinition, position);
                FinalizeUi(document, "Custom Schedule Table “" + currentDefinition.Name + "”: đã refresh native Table " + handle + ".");
            }
            catch (Exception ex) { Report(document, "QS3DSCHEDULETABLEREFRESH lỗi: " + ex.Message); }
        }

        [CommandMethod("QS3DSCHEDULETABLEREMOVE", CommandFlags.Modal)]
        public void Remove()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                RequireModelSpace(document);
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, "Custom Schedule Table remove: BLOCKED • chưa có QS3D project state/sidecar; remove không tạo project mới.");
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var scheduleId = PromptRemovableScheduleId(document, previewProject);
                if (scheduleId == null) return;

                var project = RequireExistingProject(document, "Custom Schedule Table remove");
                RequireFresh(project, expectedProjectId, expectedChangeVersion, "Custom Schedule Table remove");
                SemanticScheduleNativeTableBuilder.Remove(document, project, scheduleId);
                FinalizeUi(document, "Custom Schedule Table “" + scheduleId + "”: đã xóa generated Table/metadata thuộc project (nếu có).");
            }
            catch (Exception ex) { Report(document, "QS3DSCHEDULETABLEREMOVE lỗi: " + ex.Message); }
        }

        [CommandMethod("QS3DSCHEDULETABLEHEALTH", CommandFlags.Modal)]
        public void Health()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Custom Schedule Table health: BLOCKED • chưa có QS3D project state/sidecar; health check không tạo project mới.");
                    return;
                }

                var issues = SemanticScheduleNativeTableBuilder.Inspect(document, project);
                if (issues.Count == 0)
                {
                    Report(document, "Custom Schedule Table health: không phát hiện persisted/native ownership/content drift issue.");
                    return;
                }

                var visible = issues.Take(100)
                    .Select(x => x.Code + (string.IsNullOrWhiteSpace(x.ElementId) ? string.Empty : " [" + x.ElementId + "]") + ": " + x.Message)
                    .ToArray();
                var suffix = issues.Count > visible.Length ? "\n- … +" + (issues.Count - visible.Length) + " issue(s)" : string.Empty;
                Report(document, "Custom Schedule Table health: " + issues.Count + " issue(s).\n- " + string.Join("\n- ", visible) + suffix);
            }
            catch (Exception ex) { Report(document, "QS3DSCHEDULETABLEHEALTH lỗi: " + ex.Message); }
        }

        private static SemanticScheduleDefinition? PromptDefinition(Document document, ProjectState project)
        {
            var definitions = SemanticScheduleCatalog.Load(project);
            if (definitions.Count == 0)
            {
                Report(document, "Custom Schedule Table: project chưa có persisted semantic schedule definition.");
                return null;
            }

            WriteOptions(document, definitions.Select(x => x.Id + " = " + x.Name));
            var result = document.Editor.GetString(new PromptStringOptions("\nNhập ID hoặc tên custom semantic schedule: ") { AllowSpaces = true });
            if (result.Status != PromptStatus.OK) return null;
            var input = (result.StringResult ?? string.Empty).Trim();
            if (input.Length == 0) return null;
            var matches = definitions
                .Where(x => string.Equals(x.Id, input, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Name, input, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException(matches.Count == 0
                    ? "Không tìm thấy custom semantic schedule có ID/name “" + input + "”."
                    : "ID/name “" + input + "” khớp nhiều custom semantic schedule; hãy nhập stable schedule ID.");
            return matches[0];
        }

        private static string? PromptRemovableScheduleId(Document document, ProjectState project)
        {
            var definitions = SemanticScheduleCatalog.Load(project);
            var persisted = SemanticScheduleNativeTableBuilder.PersistedScheduleIds(project);
            var options = definitions.Select(x => x.Id + " = " + x.Name)
                .Concat(persisted.Where(id => definitions.All(x => !string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase))).Select(id => id + " = <definition removed>"))
                .ToArray();
            if (options.Length == 0)
            {
                Report(document, "Custom Schedule Table remove: không có schedule definition hoặc native owner slot nào để xóa.");
                return null;
            }

            WriteOptions(document, options);
            var result = document.Editor.GetString(new PromptStringOptions("\nNhập ID hoặc tên custom schedule cần xóa native Table: ") { AllowSpaces = true });
            if (result.Status != PromptStatus.OK) return null;
            var input = (result.StringResult ?? string.Empty).Trim();
            if (input.Length == 0) return null;

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitions)
                if (string.Equals(definition.Id, input, StringComparison.OrdinalIgnoreCase) || string.Equals(definition.Name, input, StringComparison.OrdinalIgnoreCase))
                    ids.Add(definition.Id);
            foreach (var id in persisted)
                if (string.Equals(id, input, StringComparison.OrdinalIgnoreCase)) ids.Add(id);
            if (ids.Count != 1)
                throw new InvalidOperationException(ids.Count == 0
                    ? "Không tìm thấy custom schedule/native owner slot “" + input + "”."
                    : "Tên “" + input + "” không xác định duy nhất; hãy nhập stable schedule ID.");
            return ids.First();
        }

        private static void WriteOptions(Document document, IEnumerable<string> values)
        {
            var visible = values.Take(24).ToArray();
            document.Editor.WriteMessage("\nQS3D custom schedules:\n- " + string.Join("\n- ", visible));
            var extra = values.Skip(24).Take(1).Any();
            if (extra) document.Editor.WriteMessage("\n- … danh sách còn thêm schedule; nhập stable ID để chọn.");
        }

        private static ProjectState RequireExistingProject(Document document, string operation) =>
            ExistingProjectMutationContext.Require(document, operation);

        private static void RequireFresh(ProjectState project, string expectedProjectId, long expectedChangeVersion, string operation)
        {
            if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) || project.ChangeVersion != expectedChangeVersion)
                throw new InvalidOperationException(operation + ": QS3D project/schedule state đã thay đổi trong lúc review/chọn. Hãy chạy lại lệnh.");
        }

        private static void RequireModelSpace(Document document)
        {
            if (!document.Database.TileMode)
                throw new InvalidOperationException("Custom Schedule Table P0 chỉ hỗ trợ ModelSpace. PaperSpace/Layout thuộc sheet lifecycle riêng.");
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
                throw new InvalidOperationException("Custom Schedule Table P0 chỉ hỗ trợ UCS có mặt phẳng XY song song WCS XY.");
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
            catch (Exception ex) { TryWrite(document, "\nQS3D " + message + " UI sync warning: " + ex.Message); }
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
