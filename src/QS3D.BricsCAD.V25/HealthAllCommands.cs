using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class HealthAllCommands
    {
        [CommandMethod("QS3DHEALTHALL", CommandFlags.Modal)]
        public void HealthAll()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var sourceHandles = project.Elements
                    .SelectMany(x => x.SourceHandles)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var mainHandles = PropertyHandles(project, "GeneratedSolidHandle");
                var longitudinalHandles = PropertyHandles(project, "GeneratedRebarHandles");
                var shapeHandles = PropertyHandles(project, "GeneratedShapeRebarHandles");
                var tieHandles = PropertyHandles(project, "GeneratedTieRebarHandles");
                var stirrupHandles = PropertyHandles(project, "GeneratedBeamStirrupHandles");

                var liveSources = CadHandleService.GetLiveHandles(document, sourceHandles);
                var liveMain = CadHandleService.GetLiveSolidHandles(document, mainHandles);
                var liveLongitudinal = CadHandleService.GetLiveSolidHandles(document, longitudinalHandles);
                var liveShape = CadHandleService.GetLiveSolidHandles(document, shapeHandles);
                var liveTies = CadHandleService.GetLiveSolidHandles(document, tieHandles);
                var liveStirrups = CadHandleService.GetLiveSolidHandles(document, stirrupHandles);

                var combined = new List<ModelHealthIssue>();
                combined.AddRange(new ModelHealthService().Inspect(project, liveSources, liveMain));
                combined.AddRange(new GeneratedGeometryStaleHealthService().Inspect(project));
                combined.AddRange(new GeneratedRebarHealthService().InspectAll(project, liveLongitudinal, liveShape));
                combined.AddRange(new GeneratedTieRebarHealthService().Inspect(project, liveTies));
                combined.AddRange(new GeneratedBeamStirrupHealthService().Inspect(project, liveStirrups));

                var issues = combined
                    .GroupBy(x => x.Severity + "|" + x.Code + "|" + x.ElementId + "|" + x.Message, StringComparer.Ordinal)
                    .Select(x => x.First())
                    .OrderByDescending(x => x.Severity)
                    .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var summary = new HealthSummary(issues);
                var message = "Health All: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);

                var window = new ModelHealthWindow(issues, issue =>
                {
                    var element = project.FindElement(issue.ElementId);
                    if (element == null) return;
                    var handles = LocateHandles(element, issue.Code).ToArray();
                    if (handles.Length == 0) handles = element.SourceHandles.ToArray();
                    var count = CadHandleService.Select(document, handles);
                    PaletteCoordinator.SetStatus("Health All Locate " + element.Id + " • " + count + " CAD object");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                });
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
            }
            catch (System.Exception ex)
            {
                var message = "QS3DHEALTHALL lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static string[] PropertyHandles(ProjectState project, string key)
        {
            return project.Elements
                .SelectMany(x => SplitPropertyHandles(x, key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IEnumerable<string> LocateHandles(ProjectElement element, string code)
        {
            var normalized = (code ?? string.Empty).ToUpperInvariant();
            if (normalized.Contains("BEAM_STIRRUP")) return SplitPropertyHandles(element, "GeneratedBeamStirrupHandles");
            if (normalized.Contains("TIE_REBAR")) return SplitPropertyHandles(element, "GeneratedTieRebarHandles");
            if (normalized.Contains("SHAPE_REBAR")) return SplitPropertyHandles(element, "GeneratedShapeRebarHandles");
            if (normalized.Contains("REBAR_GENERATED") || normalized.Contains("GENERATED_REBAR")) return SplitPropertyHandles(element, "GeneratedRebarHandles");
            if (normalized.Contains("GENERATED_SOLID") || normalized.Contains("GENERATED_HANDLE"))
                return SplitPropertyHandles(element, "GeneratedSolidHandle");
            return Array.Empty<string>();
        }

        private static IEnumerable<string> SplitPropertyHandles(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
