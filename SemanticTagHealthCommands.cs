using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SemanticTagHealthCommands
    {
        [CommandMethod("QS3DTAGHEALTH", CommandFlags.Modal)]
        public void CheckSemanticTags()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    var blocked = "Semantic Tag Health: BLOCKED • chưa có QS3D project state/sidecar; lệnh kiểm tra không tạo project mới.";
                    PaletteCoordinator.SetStatus(blocked);
                    document.Editor.WriteMessage("\nQS3D " + blocked);
                    return;
                }

                var persisted = new GeneratedSemanticTagHealthService().Inspect(project);
                var runtime = GeneratedSemanticTagRuntimeHealthService.Inspect(document, project);
                var issues = persisted.Concat(runtime)
                    .GroupBy(x => (x.Code ?? string.Empty) + "\n" + (x.ElementId ?? string.Empty) + "\n" + (x.Message ?? string.Empty), StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.First())
                    .ToList();

                if (issues.Count == 0)
                {
                    var ok = "Semantic Tag Health: PASS.";
                    PaletteCoordinator.SetStatus(ok);
                    document.Editor.WriteMessage("\nQS3D " + ok);
                    return;
                }

                foreach (var issue in issues.Take(100))
                    document.Editor.WriteMessage("\nQS3D TAG " + issue.Severity + " " + issue.Code + " [" + issue.ElementId + "]: " + issue.Message);
                if (issues.Count > 100)
                    document.Editor.WriteMessage("\nQS3D TAG ... còn " + (issues.Count - 100) + " issue(s). Dùng Health All / Support Bundle để tiếp tục chẩn đoán.");

                Locate(document, project, issues);
                var errors = issues.Count(x => x.Severity == HealthSeverity.Error);
                var warnings = issues.Count - errors;
                var status = "Semantic Tag Health: " + errors + " error(s), " + warnings + " warning(s).";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                var message = "QS3DTAGHEALTH lỗi: " + ex.Message;
                try { PaletteCoordinator.SetStatus(message); } catch { }
                try { document.Editor.WriteMessage("\nQS3D " + message); } catch { }
            }
        }

        private static void Locate(Document document, ProjectState project, IEnumerable<ModelHealthIssue> issues)
        {
            var handles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var issue in issues)
            {
                if (string.IsNullOrWhiteSpace(issue.ElementId)) continue;
                var element = project.FindElement(issue.ElementId);
                if (element == null) continue;
                if (!element.Properties.TryGetValue(GeneratedSemanticTagHealthService.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    if (!string.IsNullOrWhiteSpace(handle)) handles.Add(handle.Trim());
            }

            if (handles.Count == 0) return;
            CadHandleService.SelectIfAny(document, handles);
        }
    }
}
