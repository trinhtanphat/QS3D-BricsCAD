using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Documentation
{
    public sealed class SemanticDocumentationEditResult
    {
        internal SemanticDocumentationEditResult(string operation, string id, bool changed, int viewCount, int sheetCount, int rewrittenPlacementCount)
        {
            Operation = operation;
            Id = id;
            Changed = changed;
            ViewCount = viewCount;
            SheetCount = sheetCount;
            RewrittenPlacementCount = rewrittenPlacementCount;
        }

        public string Operation { get; }
        public string Id { get; }
        public bool Changed { get; }
        public int ViewCount { get; }
        public int SheetCount { get; }
        public int RewrittenPlacementCount { get; }
    }

    public sealed class SemanticDocumentationCatalogEditor
    {
        private readonly SemanticDocumentationCatalogStore _store = new SemanticDocumentationCatalogStore();

        public SemanticDocumentationEditResult UpsertView(ProjectState project, SemanticViewDefinition definition)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var catalog = _store.Load(project);
            var views = new List<SemanticViewDefinition>(catalog.Views);
            var sheets = new List<SemanticSheetDefinition>(catalog.Sheets);
            var matches = MatchingIndexes(views, definition.Id, x => x.Id, "view");
            var rewritten = 0;

            if (matches.Count == 0)
            {
                views.Add(definition);
            }
            else
            {
                var previous = views[matches[0]];
                views[matches[0]] = definition;
                if (!IdsEqualOrdinal(previous.Id, definition.Id))
                    sheets = RewriteViewReferences(sheets, previous.Id, definition.Id, out rewritten);
            }

            return Save(project, "UpsertView", definition.Id, views, sheets, rewritten);
        }

        public SemanticDocumentationEditResult ReplaceView(
            ProjectState project,
            string existingViewId,
            SemanticViewDefinition replacement,
            bool rewriteSheetReferences)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            var existingId = RequiredId(existingViewId, nameof(existingViewId));
            var catalog = _store.Load(project);
            var views = new List<SemanticViewDefinition>(catalog.Views);
            var sheets = new List<SemanticSheetDefinition>(catalog.Sheets);
            var matches = MatchingIndexes(views, existingId, x => x.Id, "view");
            if (matches.Count == 0) throw new KeyNotFoundException("Unknown semantic view: " + existingId);

            var previous = views[matches[0]];
            var changesIdentity = !IdsEqual(previous.Id, replacement.Id);
            var referenceCount = CountViewReferences(sheets, previous.Id);
            if (changesIdentity && referenceCount > 0 && !rewriteSheetReferences)
                throw new InvalidOperationException("Cannot change semantic view id while sheets still reference it: " + previous.Id + ". Enable explicit sheet-reference rewrite.");

            views[matches[0]] = replacement;
            var rewritten = 0;
            if (referenceCount > 0 && (changesIdentity || !IdsEqualOrdinal(previous.Id, replacement.Id)))
                sheets = RewriteViewReferences(sheets, previous.Id, replacement.Id, out rewritten);

            return Save(project, "ReplaceView", replacement.Id, views, sheets, rewritten);
        }

        public SemanticDocumentationEditResult RemoveView(ProjectState project, string viewId, bool removeSheetPlacements = false)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var id = RequiredId(viewId, nameof(viewId));
            var catalog = _store.Load(project);
            var views = new List<SemanticViewDefinition>(catalog.Views);
            var sheets = new List<SemanticSheetDefinition>(catalog.Sheets);
            var matches = MatchingIndexes(views, id, x => x.Id, "view");
            if (matches.Count == 0) return Unchanged("RemoveView", id, views.Count, sheets.Count);

            var ownedId = views[matches[0]].Id;
            var referenceCount = CountViewReferences(sheets, ownedId);
            if (referenceCount > 0 && !removeSheetPlacements)
                throw new InvalidOperationException("Cannot remove semantic view while sheets reference it: " + ownedId + " (" + referenceCount + " placement(s)).");

            views.RemoveAt(matches[0]);
            var rewritten = 0;
            if (referenceCount > 0)
                sheets = RemoveViewReferences(sheets, ownedId, out rewritten);
            return Save(project, "RemoveView", ownedId, views, sheets, rewritten);
        }

        public SemanticDocumentationEditResult UpsertSheet(ProjectState project, SemanticSheetDefinition definition)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var catalog = _store.Load(project);
            var views = new List<SemanticViewDefinition>(catalog.Views);
            var sheets = new List<SemanticSheetDefinition>(catalog.Sheets);
            var matches = MatchingIndexes(sheets, definition.Id, x => x.Id, "sheet");
            if (matches.Count == 0) sheets.Add(definition);
            else sheets[matches[0]] = definition;
            return Save(project, "UpsertSheet", definition.Id, views, sheets, 0);
        }

        public SemanticDocumentationEditResult RemoveSheet(ProjectState project, string sheetId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var id = RequiredId(sheetId, nameof(sheetId));
            var catalog = _store.Load(project);
            var views = new List<SemanticViewDefinition>(catalog.Views);
            var sheets = new List<SemanticSheetDefinition>(catalog.Sheets);
            var matches = MatchingIndexes(sheets, id, x => x.Id, "sheet");
            if (matches.Count == 0) return Unchanged("RemoveSheet", id, views.Count, sheets.Count);
            var ownedId = sheets[matches[0]].Id;
            sheets.RemoveAt(matches[0]);
            return Save(project, "RemoveSheet", ownedId, views, sheets, 0);
        }

        private SemanticDocumentationEditResult Save(
            ProjectState project,
            string operation,
            string id,
            IReadOnlyList<SemanticViewDefinition> views,
            IReadOnlyList<SemanticSheetDefinition> sheets,
            int rewrittenPlacementCount)
        {
            var version = project.ChangeVersion;
            _store.Save(project, views, sheets);
            return new SemanticDocumentationEditResult(
                operation,
                (id ?? string.Empty).Trim(),
                project.ChangeVersion != version,
                views.Count,
                sheets.Count,
                rewrittenPlacementCount);
        }

        private static SemanticDocumentationEditResult Unchanged(string operation, string id, int viewCount, int sheetCount)
        {
            return new SemanticDocumentationEditResult(operation, id, false, viewCount, sheetCount, 0);
        }

        private static List<int> MatchingIndexes<T>(IReadOnlyList<T> items, string? id, Func<T, string> selector, string label)
        {
            var normalized = RequiredId(id, label + "Id");
            var result = new List<int>();
            for (var i = 0; i < items.Count; i++)
                if (IdsEqual(selector(items[i]), normalized)) result.Add(i);
            if (result.Count > 1) throw new InvalidOperationException("Semantic documentation catalog contains duplicate " + label + " id: " + normalized + ".");
            return result;
        }

        private static int CountViewReferences(IEnumerable<SemanticSheetDefinition> sheets, string viewId)
        {
            var count = 0;
            foreach (var sheet in sheets)
                foreach (var placement in sheet.Placements)
                    if (IdsEqual(placement.ViewId, viewId)) count++;
            return count;
        }

        private static List<SemanticSheetDefinition> RewriteViewReferences(
            IEnumerable<SemanticSheetDefinition> sheets,
            string oldViewId,
            string newViewId,
            out int rewritten)
        {
            rewritten = 0;
            var result = new List<SemanticSheetDefinition>();
            foreach (var sheet in sheets)
            {
                var placements = new List<SemanticSheetPlacementDefinition>();
                foreach (var placement in sheet.Placements)
                {
                    if (IdsEqual(placement.ViewId, oldViewId))
                    {
                        placements.Add(ClonePlacement(placement, newViewId));
                        rewritten++;
                    }
                    else placements.Add(ClonePlacement(placement, placement.ViewId));
                }
                result.Add(CloneSheet(sheet, placements));
            }
            return result;
        }

        private static List<SemanticSheetDefinition> RemoveViewReferences(
            IEnumerable<SemanticSheetDefinition> sheets,
            string viewId,
            out int removed)
        {
            removed = 0;
            var result = new List<SemanticSheetDefinition>();
            foreach (var sheet in sheets)
            {
                var placements = new List<SemanticSheetPlacementDefinition>();
                foreach (var placement in sheet.Placements)
                {
                    if (IdsEqual(placement.ViewId, viewId))
                    {
                        removed++;
                        continue;
                    }
                    placements.Add(ClonePlacement(placement, placement.ViewId));
                }
                result.Add(CloneSheet(sheet, placements));
            }
            return result;
        }

        private static bool IdsEqual(string? left, string? right)
        {
            return string.Equals(NormalizedId(left), NormalizedId(right), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IdsEqualOrdinal(string? left, string? right)
        {
            return string.Equals(NormalizedId(left), NormalizedId(right), StringComparison.Ordinal);
        }

        private static string NormalizedId(string? value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static SemanticSheetDefinition CloneSheet(SemanticSheetDefinition sheet, IEnumerable<SemanticSheetPlacementDefinition> placements)
        {
            return new SemanticSheetDefinition(
                sheet.Id,
                sheet.Number,
                sheet.Name,
                sheet.WidthMm,
                sheet.HeightMm,
                placements,
                sheet.TitleBlockName);
        }

        private static SemanticSheetPlacementDefinition ClonePlacement(SemanticSheetPlacementDefinition placement, string viewId)
        {
            return new SemanticSheetPlacementDefinition(viewId, placement.Xmm, placement.Ymm, placement.WidthMm, placement.HeightMm);
        }

        private static string RequiredId(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Semantic documentation id is required.", name);
            return value!.Trim();
        }
    }
}