using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class SemanticSheetRuntimeHealthService
    {
        public static IReadOnlyList<ModelHealthIssue> Inspect(
            Document document,
            ProjectState project,
            SemanticSheetPlan sheet,
            IEnumerable<SemanticViewPlan> availableViews,
            IEnumerable<SemanticTitleBlockParameterDefinition> titleBlockMappings)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (availableViews == null) throw new ArgumentNullException(nameof(availableViews));
            if (titleBlockMappings == null) throw new ArgumentNullException(nameof(titleBlockMappings));

            var issues = new List<ModelHealthIssue>();
            var views = BuildViewIndex(availableViews);
            foreach (var placement in sheet.Placements)
                if (!views.ContainsKey(placement.ViewId))
                    issues.Add(Issue("SEMANTIC_SHEET_VIEW_PLAN_MISSING", HealthSeverity.Error, "Sheet placement references unavailable semantic view id: " + placement.ViewId + ".", sheet));

            var layoutName = SemanticSheetArtifactService.LayoutNameFor(sheet);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var layoutId = TryGetLayoutId(document.Database, transaction, layoutName);
                if (layoutId.IsNull || !layoutId.IsValid)
                {
                    issues.Add(Issue("SEMANTIC_SHEET_LAYOUT_MISSING", HealthSeverity.Error, "Native Layout is missing: " + layoutName + ".", sheet));
                    transaction.Commit();
                    return issues.AsReadOnly();
                }

                var layout = transaction.GetObject(layoutId, OpenMode.ForRead, false) as Layout;
                if (layout == null)
                {
                    issues.Add(Issue("SEMANTIC_SHEET_LAYOUT_TYPE_MISMATCH", HealthSeverity.Error, "Layout dictionary entry is not a Layout: " + layoutName + ".", sheet));
                    transaction.Commit();
                    return issues.AsReadOnly();
                }
                if (!SemanticSheetOwnershipService.HasMatching(layout, project.ProjectId, sheet.Id, SemanticSheetOwnershipService.ArtifactLayout))
                    issues.Add(Issue("SEMANTIC_SHEET_LAYOUT_OWNERSHIP_MISMATCH", HealthSeverity.Error, "Native Layout QS3D_SHEET ownership does not match current project/sheet.", sheet));

                var paper = transaction.GetObject(layout.BlockTableRecordId, OpenMode.ForRead, false) as BlockTableRecord;
                if (paper == null)
                {
                    issues.Add(Issue("SEMANTIC_SHEET_PAPERSPACE_MISSING", HealthSeverity.Error, "Layout does not resolve to a PaperSpace BlockTableRecord.", sheet));
                    transaction.Commit();
                    return issues.AsReadOnly();
                }
                if (!SemanticSheetOwnershipService.HasMatching(paper, project.ProjectId, sheet.Id, SemanticSheetOwnershipService.ArtifactPaperSpace))
                    issues.Add(Issue("SEMANTIC_SHEET_PAPERSPACE_OWNERSHIP_MISMATCH", HealthSeverity.Error, "PaperSpace QS3D_SHEET ownership does not match current project/sheet.", sheet));

                var expectedViews = new HashSet<string>(sheet.Placements.Select(x => x.ViewId), StringComparer.OrdinalIgnoreCase);
                var seenViews = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var titleBlocks = 0;
                var paperViewports = 0;
                var map = SemanticTitleBlockParameterMapBuilder.Build(sheet, titleBlockMappings)
                    .Values.ToDictionary(x => x.DestinationTag, x => x.Value, StringComparer.OrdinalIgnoreCase);

                foreach (ObjectId id in paper)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    if (!SemanticSheetOwnershipService.HasMarker(entity))
                    {
                        issues.Add(Issue("SEMANTIC_SHEET_UNOWNED_PAPERSPACE_CONTENT", HealthSeverity.Warning, "Generated layout contains unowned PaperSpace content " + entity.Handle + "; refresh preserves it and remove will refuse.", sheet));
                        continue;
                    }
                    if (!SemanticSheetOwnershipService.TryRead(entity, out var ownerProject, out var ownerSheet, out var artifact, out var viewId))
                    {
                        issues.Add(Issue("SEMANTIC_SHEET_OWNERSHIP_MARKER_INVALID", HealthSeverity.Error, "PaperSpace contains malformed QS3D_SHEET ownership metadata at handle " + entity.Handle + ".", sheet));
                        continue;
                    }
                    if (!string.Equals(ownerProject, project.ProjectId, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(ownerSheet, sheet.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add(Issue("SEMANTIC_SHEET_MIXED_OWNERSHIP", HealthSeverity.Error, "PaperSpace contains QS3D_SHEET content owned by another project/sheet at handle " + entity.Handle + ".", sheet));
                        continue;
                    }

                    if (string.Equals(artifact, SemanticSheetOwnershipService.ArtifactPaperViewport, StringComparison.Ordinal))
                    {
                        paperViewports++;
                        if (!(entity is Viewport))
                            issues.Add(Issue("SEMANTIC_SHEET_PAPER_VIEWPORT_TYPE_MISMATCH", HealthSeverity.Error, "PaperViewport ownership marker is attached to a non-Viewport entity.", sheet));
                        continue;
                    }
                    if (string.Equals(artifact, SemanticSheetOwnershipService.ArtifactViewport, StringComparison.Ordinal))
                    {
                        if (!(entity is Viewport viewport))
                        {
                            issues.Add(Issue("SEMANTIC_SHEET_VIEWPORT_TYPE_MISMATCH", HealthSeverity.Error, "Semantic Viewport ownership marker is attached to a non-Viewport entity.", sheet));
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(viewId))
                        {
                            issues.Add(Issue("SEMANTIC_SHEET_VIEWPORT_VIEW_ID_MISSING", HealthSeverity.Error, "Owned semantic Viewport is missing its view id.", sheet));
                            continue;
                        }
                        if (seenViews.ContainsKey(viewId)) seenViews[viewId]++;
                        else seenViews.Add(viewId, 1);
                        if (!expectedViews.Contains(viewId))
                            issues.Add(Issue("SEMANTIC_SHEET_VIEWPORT_UNEXPECTED", HealthSeverity.Warning, "Layout contains an owned viewport no longer present in the sheet plan: " + viewId + ".", sheet));
                        if (!viewport.Locked)
                            issues.Add(Issue("SEMANTIC_SHEET_VIEWPORT_UNLOCKED", HealthSeverity.Warning, "Owned semantic Viewport is not locked: " + viewId + ".", sheet));
                        if (!Finite(viewport.CustomScale) || !(viewport.CustomScale > 0d))
                            issues.Add(Issue("SEMANTIC_SHEET_VIEWPORT_SCALE_INVALID", HealthSeverity.Error, "Owned semantic Viewport CustomScale is not finite/positive: " + viewId + ".", sheet));
                        if (!Finite(viewport.Width) || !(viewport.Width > 0d) || !Finite(viewport.Height) || !(viewport.Height > 0d))
                            issues.Add(Issue("SEMANTIC_SHEET_VIEWPORT_SIZE_INVALID", HealthSeverity.Error, "Owned semantic Viewport width/height is not finite/positive: " + viewId + ".", sheet));
                        continue;
                    }
                    if (string.Equals(artifact, SemanticSheetOwnershipService.ArtifactTitleBlock, StringComparison.Ordinal))
                    {
                        titleBlocks++;
                        if (!(entity is BlockReference reference))
                        {
                            issues.Add(Issue("SEMANTIC_SHEET_TITLEBLOCK_TYPE_MISMATCH", HealthSeverity.Error, "TitleBlock ownership marker is attached to a non-BlockReference entity.", sheet));
                            continue;
                        }
                        InspectTitleBlock(transaction, reference, sheet, map, issues);
                        continue;
                    }

                    issues.Add(Issue("SEMANTIC_SHEET_ARTIFACT_KIND_INVALID", HealthSeverity.Error, "Unexpected QS3D_SHEET artifact kind inside PaperSpace: " + artifact + ".", sheet));
                }

                foreach (var expected in expectedViews)
                {
                    if (!seenViews.TryGetValue(expected, out var count))
                        issues.Add(Issue("SEMANTIC_SHEET_VIEWPORT_MISSING", HealthSeverity.Error, "Native sheet is missing viewport for semantic view id: " + expected + ".", sheet));
                    else if (count != 1)
                        issues.Add(Issue("SEMANTIC_SHEET_VIEWPORT_DUPLICATE", HealthSeverity.Error, "Native sheet has " + count + " owned viewports for semantic view id: " + expected + ".", sheet));
                }

                if (paperViewports == 0)
                    issues.Add(Issue("SEMANTIC_SHEET_PAPER_VIEWPORT_MISSING", HealthSeverity.Warning, "Generated layout has no claimed system paper-space viewport.", sheet));

                var expectsTitleBlock = !string.IsNullOrWhiteSpace(sheet.TitleBlockName);
                if (expectsTitleBlock && titleBlocks != 1)
                    issues.Add(Issue("SEMANTIC_SHEET_TITLEBLOCK_COUNT_INVALID", HealthSeverity.Error, "Sheet plan expects exactly one owned title block but native layout has " + titleBlocks + ".", sheet));
                if (!expectsTitleBlock && titleBlocks != 0)
                    issues.Add(Issue("SEMANTIC_SHEET_TITLEBLOCK_UNEXPECTED", HealthSeverity.Warning, "Sheet plan has no title block but native layout still contains an owned title block.", sheet));

                transaction.Commit();
            }

            return issues.AsReadOnly();
        }

        private static void InspectTitleBlock(
            Transaction transaction,
            BlockReference reference,
            SemanticSheetPlan sheet,
            IReadOnlyDictionary<string, string> expectedValues,
            ICollection<ModelHealthIssue> issues)
        {
            var definition = transaction.GetObject(reference.BlockTableRecord, OpenMode.ForRead, false) as BlockTableRecord;
            if (definition == null)
            {
                issues.Add(Issue("SEMANTIC_SHEET_TITLEBLOCK_DEFINITION_MISSING", HealthSeverity.Error, "Owned title-block reference no longer resolves to a block definition.", sheet));
                return;
            }
            if (!string.Equals(definition.Name, sheet.TitleBlockName, StringComparison.OrdinalIgnoreCase))
                issues.Add(Issue("SEMANTIC_SHEET_TITLEBLOCK_NAME_DRIFT", HealthSeverity.Warning, "Owned title-block definition no longer matches sheet plan: " + definition.Name + ".", sheet));

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ObjectId id in reference.AttributeCollection)
            {
                var attribute = transaction.GetObject(id, OpenMode.ForRead, false) as AttributeReference;
                if (attribute == null) continue;
                var tag = (attribute.Tag ?? string.Empty).Trim();
                if (tag.Length == 0) continue;
                if (!seen.Add(tag))
                {
                    issues.Add(Issue("SEMANTIC_SHEET_TITLEBLOCK_ATTRIBUTE_DUPLICATE", HealthSeverity.Error, "Owned title block contains duplicate attribute tag: " + tag + ".", sheet));
                    continue;
                }
                if (expectedValues.TryGetValue(tag, out var expected) && !string.Equals(attribute.TextString ?? string.Empty, expected, StringComparison.Ordinal))
                    issues.Add(Issue("SEMANTIC_SHEET_TITLEBLOCK_ATTRIBUTE_DRIFT", HealthSeverity.Warning, "Title-block attribute " + tag + " no longer matches semantic sheet value.", sheet));
            }
        }

        private static Dictionary<string, SemanticViewPlan> BuildViewIndex(IEnumerable<SemanticViewPlan> views)
        {
            var result = new Dictionary<string, SemanticViewPlan>(StringComparer.OrdinalIgnoreCase);
            foreach (var view in views)
            {
                if (view == null) throw new InvalidOperationException("Available semantic view cannot be null.");
                if (result.ContainsKey(view.Id)) throw new InvalidOperationException("Available semantic views contain duplicate id: " + view.Id + ".");
                result.Add(view.Id, view);
            }
            return result;
        }

        private static ObjectId TryGetLayoutId(Database database, Transaction transaction, string layoutName)
        {
            var dictionary = transaction.GetObject(database.LayoutDictionaryId, OpenMode.ForRead, false) as DBDictionary;
            if (dictionary == null || !dictionary.Contains(layoutName)) return ObjectId.Null;
            return dictionary.GetAt(layoutName);
        }

        private static ModelHealthIssue Issue(string code, HealthSeverity severity, string message, SemanticSheetPlan sheet) =>
            new ModelHealthIssue(code, severity, message, sheet.Id);

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
