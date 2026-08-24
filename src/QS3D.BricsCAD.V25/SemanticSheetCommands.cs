using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SemanticSheetCommands
    {
        private const string GeneratedSolidHandleKey = "GeneratedSolidHandle";
        private const double A3WidthMm = 420d;
        private const double A3HeightMm = 297d;
        private const double MarginMm = 15d;

        [CommandMethod("QS3DSHEETBUILD", CommandFlags.Modal)]
        public void BuildSemanticSheet()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var input = PromptSheetInput(document);
                if (input == null) return;
                var project = ExistingProjectMutationContext.Require(document, "Semantic Sheet build");
                var planned = BuildOverviewPlan(project, input.Value);
                var layoutName = SemanticSheetArtifactService.Build(document, project, planned.Sheet, planned.Views, DefaultTitleBlockMappings());
                FinalizeUi(document, "Semantic Sheet: đã tạo Layout " + layoutName + " từ SemanticSheetPlan.");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DSHEETBUILD lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DSHEETREFRESH", CommandFlags.Modal)]
        public void RefreshSemanticSheet()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var input = PromptSheetInput(document);
                if (input == null) return;
                var project = ExistingProjectMutationContext.Require(document, "Semantic Sheet refresh");
                var planned = BuildOverviewPlan(project, input.Value);
                if (!Confirm(document, "Refresh sẽ thay thế toàn bộ QS3D-owned Viewport/title-block trong " + SemanticSheetArtifactService.LayoutNameFor(planned.Sheet) + ". Tiếp tục?")) return;
                var layoutName = SemanticSheetArtifactService.Refresh(document, project, planned.Sheet, planned.Views, DefaultTitleBlockMappings());
                FinalizeUi(document, "Semantic Sheet: đã refresh Layout " + layoutName + ". Unowned PaperSpace content được giữ nguyên.");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DSHEETREFRESH lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DSHEETREMOVE", CommandFlags.Modal)]
        public void RemoveSemanticSheet()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var number = PromptRequiredString(document, "\nSheet number cần remove: ");
                if (number == null) return;
                var project = ExistingProjectMutationContext.Require(document, "Semantic Sheet remove");
                var sheetId = SheetId(number);
                var layoutName = SemanticSheetArtifactService.LayoutNameForNumber(number);
                if (!Confirm(document, "Remove sẽ xóa Layout " + layoutName + " chỉ khi toàn bộ live PaperSpace content đều QS3D-owned. Tiếp tục?")) return;
                SemanticSheetArtifactService.Remove(document, project, sheetId, layoutName);
                FinalizeUi(document, "Semantic Sheet: đã remove owned Layout " + layoutName + ".");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DSHEETREMOVE lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DSHEETHEALTH", CommandFlags.Modal)]
        public void InspectSemanticSheetHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var input = PromptSheetInput(document);
                if (input == null) return;
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Semantic Sheet health yêu cầu QS3D project hiện hữu; lệnh không tạo project mới.");
                var planned = BuildOverviewPlan(project, input.Value);
                var issues = SemanticSheetRuntimeHealthService.Inspect(document, project, planned.Sheet, planned.Views, DefaultTitleBlockMappings());
                if (issues.Count == 0)
                {
                    FinalizeUi(document, "Semantic Sheet health: không phát hiện drift cho " + planned.Sheet.Number + ".");
                    return;
                }

                document.Editor.WriteMessage("\nQS3D Semantic Sheet health " + planned.Sheet.Number + ": " + issues.Count + " issue(s)");
                foreach (var issue in issues.Take(100))
                    document.Editor.WriteMessage("\n - [" + issue.Severity + "] " + issue.Code + ": " + issue.Message);
                if (issues.Count > 100) document.Editor.WriteMessage("\n - ... truncated after 100 issues");
                try { PaletteCoordinator.SetStatus("Semantic Sheet health: " + issues.Count + " issue(s) cho " + planned.Sheet.Number + "."); } catch { }
            }
            catch (Exception ex)
            {
                Report(document, "QS3DSHEETHEALTH lỗi: " + ex.Message);
            }
        }

        private static PlannedSheet BuildOverviewPlan(ProjectState project, SheetInput input)
        {
            var geometryElementIds = project.Elements
                .Where(HasAuthoritativeGeometry)
                .Select(x => x.Id)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToArray();
            if (geometryElementIds.Length == 0)
                throw new InvalidOperationException("Project không có semantic element với authoritative CAD geometry để tạo sheet viewport.");

            var sheetId = SheetId(input.Number);
            var view = SemanticViewPlanner.Build(
                project,
                new SemanticViewDefinition(
                    sheetId + "-overview",
                    input.Name + " Overview",
                    SemanticViewKind.Plan,
                    includeElementIds: geometryElementIds));
            var placement = new SemanticSheetPlacementDefinition(
                view.Id,
                MarginMm,
                MarginMm,
                A3WidthMm - MarginMm * 2d,
                A3HeightMm - MarginMm * 2d);
            var definition = new SemanticSheetDefinition(
                sheetId,
                input.Number,
                input.Name,
                A3WidthMm,
                A3HeightMm,
                new[] { placement },
                input.TitleBlockName);
            var sheet = SemanticSheetPlanner.Build(definition, new[] { view });
            return new PlannedSheet(sheet, new[] { view });
        }

        private static bool HasAuthoritativeGeometry(ProjectElement element)
        {
            if (element == null) return false;
            if (element.SourceHandles.Any(x => !string.IsNullOrWhiteSpace(x))) return true;
            return element.Properties.TryGetValue(GeneratedSolidHandleKey, out var generated) && !string.IsNullOrWhiteSpace(generated);
        }

        private static IReadOnlyList<SemanticTitleBlockParameterDefinition> DefaultTitleBlockMappings()
        {
            return new[]
            {
                new SemanticTitleBlockParameterDefinition("SHEET_ID", SemanticTitleBlockSheetField.SheetId),
                new SemanticTitleBlockParameterDefinition("SHEET_NUMBER", SemanticTitleBlockSheetField.SheetNumber),
                new SemanticTitleBlockParameterDefinition("SHEET_NAME", SemanticTitleBlockSheetField.SheetName),
                new SemanticTitleBlockParameterDefinition("TITLE_BLOCK_NAME", SemanticTitleBlockSheetField.TitleBlockName),
                new SemanticTitleBlockParameterDefinition("PLACED_VIEW_COUNT", SemanticTitleBlockSheetField.PlacedViewCount)
            };
        }

        private static SheetInput? PromptSheetInput(Document document)
        {
            var number = PromptRequiredString(document, "\nSheet number (vd A-101): ");
            if (number == null) return null;
            var name = PromptRequiredString(document, "\nSheet name: ");
            if (name == null) return null;
            var titleBlock = PromptOptionalString(document, "\nTitle-block block name <none>: ");
            return new SheetInput(number, name, titleBlock);
        }

        private static string? PromptRequiredString(Document document, string message)
        {
            var options = new PromptStringOptions(message) { AllowSpaces = true };
            var result = document.Editor.GetString(options);
            if (result.Status != PromptStatus.OK) return null;
            var value = (result.StringResult ?? string.Empty).Trim();
            if (value.Length == 0) throw new InvalidOperationException("Giá trị bắt buộc không được rỗng.");
            return value;
        }

        private static string? PromptOptionalString(Document document, string message)
        {
            var options = new PromptStringOptions(message) { AllowSpaces = true };
            var result = document.Editor.GetString(options);
            if (result.Status == PromptStatus.None || result.Status == PromptStatus.Cancel) return null;
            if (result.Status != PromptStatus.OK) return null;
            var value = (result.StringResult ?? string.Empty).Trim();
            return value.Length == 0 ? null : value;
        }

        private static bool Confirm(Document document, string message)
        {
            var options = new PromptKeywordOptions("\n" + message) { AllowNone = false };
            options.Keywords.Add("Yes");
            options.Keywords.Add("No");
            options.Keywords.Default = "No";
            var result = document.Editor.GetKeywords(options);
            return result.Status == PromptStatus.OK && string.Equals(result.StringResult, "Yes", StringComparison.OrdinalIgnoreCase);
        }

        private static string SheetId(string number)
        {
            var normalized = StableComponent(number);
            if (normalized.Length == 0) throw new InvalidOperationException("Sheet number cannot produce a semantic sheet id.");
            return "sheet-" + normalized;
        }

        private static string StableComponent(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var builder = new StringBuilder(value.Length);
            var pendingDash = false;
            foreach (var ch in value.Trim())
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-')
                {
                    if (pendingDash && builder.Length > 0) builder.Append('-');
                    builder.Append(char.ToLowerInvariant(ch));
                    pendingDash = false;
                }
                else pendingDash = true;
                if (builder.Length >= 80) break;
            }
            return builder.ToString().Trim('-');
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

        private readonly struct SheetInput
        {
            public SheetInput(string number, string name, string? titleBlockName)
            {
                Number = number;
                Name = name;
                TitleBlockName = titleBlockName;
            }
            public string Number { get; }
            public string Name { get; }
            public string? TitleBlockName { get; }
        }

        private readonly struct PlannedSheet
        {
            public PlannedSheet(SemanticSheetPlan sheet, IReadOnlyList<SemanticViewPlan> views)
            {
                Sheet = sheet;
                Views = views;
            }
            public SemanticSheetPlan Sheet { get; }
            public IReadOnlyList<SemanticViewPlan> Views { get; }
        }
    }
}
