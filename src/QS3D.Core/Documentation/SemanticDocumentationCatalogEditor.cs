using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Documentation
{
    public sealed class SemanticDocumentationEditResult
    {
        internal SemanticDocumentationEditResult(
            string operation,
            string id,
            bool changed,
            int viewCount,
            int sheetCount,
            int scheduleCount,
            int rewrittenPlacementCount,
            int rewrittenScheduleReferenceCount)
        {
            Operation = operation;
            Id = id;
            Changed = changed;
            ViewCount = viewCount;
            SheetCount = sheetCount;
            ScheduleCount = scheduleCount;
            RewrittenPlacementCount = rewrittenPlacementCount;
            RewrittenScheduleReferenceCount = rewrittenScheduleReferenceCount;
        }

        public string Operation { get; }
        public string Id { get; }
        public bool Changed { get; }
        public int ViewCount { get; }
        public int SheetCount { get; }
        public int ScheduleCount { get; }
        public int RewrittenPlacementCount { get; }
        public int RewrittenScheduleReferenceCount { get; }
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
            var schedules = new List<SemanticScheduleDefinition>(catalog.Schedules);
            var matches = MatchingIndexes(views, definition.Id, x => x.Id, "view");
            var rewrittenPlacements = 0;
            var rewrittenSchedules = 0;

            if (matches.Count == 0)
            {
                views.Add(definition);
            }
            else
            {
                var previous = views[matches[0]];
                views[matches[0]] = definition;
                if (!string.Equals(previous.Id, definition.Id, StringComparison.Ordinal))
                {
                    sheets = RewriteViewReferences(sheets, previous.Id, definition.Id, out rewrittenPlacements);
                    schedules = RewriteScheduleReferences(schedules, previous.Id, definition.Id, out rewrittenSchedules);
                }
            }

            return Save(project, "UpsertView", definition.Id, views, sheets, schedules, rewrittenPlacements, rewrittenSchedules);
        }

        public SemanticDocumentationEditResult ReplaceView(
            ProjectState project,
            string existingViewId,
            SemanticViewDefinition replacement,
            bool rewriteSheetReferences)
        {
            return ReplaceView(project, existingViewId, replacement, rewriteSheetReferences, false);
        }

        public SemanticDocumentationEditResult ReplaceView(
            ProjectState project,
            string existingViewId,
            SemanticViewDefinition replacement,
            bool rewriteSheetReferences,
            bool rewriteScheduleReferences)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));
            var existingId = RequiredId(existingViewId, nameof(existingViewId));
            var catalog = _store.Load(project);
            var views = new List<SemanticViewDefinition>(catalog.Views);
            var sheets = new List<SemanticSheetDefinition>(catalog.Sheets);
            var schedules = new List<SemanticScheduleDefinition>(catalog.Schedules);
            var matches = MatchingIndexes(views, existingId, x => x.Id, "view");
            if (matches.Count == 0) throw new KeyNotFoundException("Unknown semantic view: " + existingId);

            var previous = views[matches[0]];
            var changesIdentity = !string.Equals(previous.Id, replacement.Id, StringComparison.OrdinalIgnoreCase);
            var placementReferenceCount = CountSheetViewReferences(sheets, previous.Id);
            var scheduleReferenceCount = CountScheduleViewReferences(schedules, previous.Id);
            if (changesIdentity && placementReferenceCount > 0 && !rewriteSheetReferences)
                throw new InvalidOperationException("Cannot change semantic view id while sheets still reference it: " + previous.Id + ". Enable explicit sheet-reference rewrite.");
            if (changesIdentity && scheduleReferenceCount > 0 && !rewriteScheduleReferences)
                throw new InvalidOperationException("Cannot change semantic view id while schedules still reference it: " + previous.Id + ". Enable explicit schedule-reference rewrite.");

            views[matches[0]] = replacement;
            var rewrittenPlacements = 0;
            var rewrittenSchedules = 0;
            var spellingChanged = !string.Equals(previous.Id, replacement.Id, StringComparison.Ordinal);
            if (placementReferenceCount > 0 && spellingChanged)
            {
                if (changesIdentity || rewriteSheetReferences || string.Equals(previous.Id, replacement.Id, StringComparison.OrdinalIgnoreCase))
                    sheets = RewriteViewReferences(sheets, previous.Id, replacement.Id, out rewrittenPlacements);
            }
            if (scheduleReferenceCount > 0 && spellingChanged)
            {
                if (changesIdentity || rewriteScheduleReferences || string.Equals(previous.Id, replacement.Id, StringComparison.OrdinalIgnoreCase))
                    schedules = RewriteScheduleReferences(schedules, previous.Id, replacement.Id, out rewrittenSchedules);
            }

            return Save(project, "ReplaceView", replacement.Id, views, sheets, schedules, rewrittenPlacements, rewrittenSchedules);
        }

        public SemanticDocumentationEditResult RemoveView(ProjectState project, string viewId, bool removeSheetPlacements = false)
        {
            return RemoveView(project, viewId, removeSheetPlacements, false);
        }

        public SemanticDocumentationEditResult RemoveView(
            ProjectState project,
            string viewId,
            bool removeSheetPlacements,
            bool removeSchedules)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var id = RequiredId(viewId, nameof(viewId));
            var catalog = _store.Load(project);
            var views = new List<SemanticViewDefinition>(catalog.Views);
            var sheets = new List<SemanticSheetDefinition>(catalog.Sheets);
            var schedules = new List<SemanticScheduleDefinition>(catalog.Schedules);
            var matches = MatchingIndexes(views, id, x => x.Id, "view");
            if (matches.Count == 0) return Unchanged("RemoveView", id, views.Count, sheets.Count, schedules.Count);

            var ownedId = views[matches[0]].Id;
            var placementReferenceCount = CountSheetViewReferences(sheets, ownedId);
            var scheduleReferenceCount = CountScheduleViewReferences(schedules, ownedId);
            if (placementReferenceCount > 0 && !removeSheetPlacements)
                throw new InvalidOperationException("Cannot remove semantic view while sheets reference it: " + ownedId + " (" + placementReferenceCount + " placement(s)).");
            if (scheduleReferenceCount > 0 && !removeSchedules)
                throw new InvalidOperationException("Cannot remove semantic view while schedules reference it: " + ownedId + " (" + scheduleReferenceCount + " schedule(s)).");

            views.RemoveAt(matches[0]);
            var removedPlacements = 0;
            var removedSchedules = 0;
            if (placementReferenceCount > 0)
                sheets = RemoveViewReferences(sheets, ownedId, out removedPlacements);
            if (scheduleReferenceCount > 0)
                schedules = RemoveSchedulesForView(schedules, ownedId, out removedSchedules);
            return Save(project, "RemoveView", ownedId, views, sheets, schedules, removedPlacements, removedSchedules);
        }

        public SemanticDocumentationEditResult UpsertSheet(ProjectState project, SemanticSheetDefinition definition)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var catalog = _store.Load(project);
            var views = new List<SemanticViewDefinition>(catalog.Views);
            var sheets = new List<SemanticSheetDefinition>(catalog.Sheets);
            var schedules = new List<SemanticScheduleDefinition>(catalog.Schedules);
            var matches = MatchingIndexes(sheets, definition.Id, x => x.Id, "sheet");
            if (matches.Count == 0) sheets.Add(definition);
            else sheets[matches[0]] = definition;
            return Save(project, "UpsertSheet", definition.Id, views, sheets, schedules, 0, 0);
        }

        public SemanticDocumentationEditResult RemoveSheet(ProjectState project, string sheetId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var id = RequiredId(sheetId, nameof(sheetId));
            var catalog = _store.Load(project);
            var views = new List<SemanticViewDefinition>(catalog.Views);
            var sheets = new List<SemanticSheetDefinition>(catalog.Sheets);
            var schedules = new List<SemanticScheduleDefinition>(catalog.Schedules);
            var matches = MatchingIndexes(sheets, id, x => x.Id, "sheet");
            if (matches.Count == 0) return Unchanged("RemoveSheet", id, views.Count, sheets.Count, schedules.Count);
            var ownedId = sheets[matches[0]].Id;
            sheets.RemoveAt(matches[0]);
            return Save(project, "RemoveSheet", ownedId, views, sheets, schedules, 0, 0);
        }

        public SemanticDocumentationEditResult UpsertSchedule(ProjectState project, SemanticScheduleDefinition definition)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var catalog = _store.Load(project);
            var views = new List<SemanticViewDefinition>(catalog.Views);
            var sheets = new List<SemanticSheetDefinition>(catalog.Sheets);
            var schedules = new List<SemanticScheduleDefinition>(catalog.Schedules);
            var matches = MatchingIndexes(schedules, definition.Id, x => x.Id, "schedule");
            if (matches.Count == 0) schedules.Add(definition);
            else schedules[matches[0]] = definition;
            return Save(project, "UpsertSchedule", definition.Id, views, sheets, schedules, 0, 0);
        }

        public SemanticDocumentationEditResult RemoveSchedule(ProjectState project, string scheduleId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var id = RequiredId(scheduleId, nameof(scheduleId));
            var catalog = _store.Load(project);
            var views = new List<SemanticViewDefinition>(catalog.Views);
            var sheets = new List<SemanticSheetDefinition>(catalog.Sheets);
            var schedules = new List<SemanticScheduleDefinition>(catalog.Schedules);
            var matches = MatchingIndexes(schedules, id, x => x.Id, "schedule");
            if (matches.Count == 0) return Unchanged("RemoveSchedule", id, views.Count, sheets.Count, schedules.Count);
            var ownedId = schedules[matches[0]].Id;
            schedules.RemoveAt(matches[0]);
            return Save(project, "RemoveSchedule", ownedId, views, sheets, schedules, 0, 0);
        }

        private SemanticDocumentationEditResult Save(
            ProjectState project,
            string operation,
            string id,
            IReadOnlyList<SemanticViewDefinition> views,
            IReadOnlyList<SemanticSheetDefinition> sheets,
            IReadOnlyList<SemanticScheduleDefinition> schedules,
            int rewrittenPlacementCount,
            int rewrittenScheduleReferenceCount)
        {
            var version = project.ChangeVersion;
            _store.Save(project, views, sheets, schedules);
            return new SemanticDocumentationEditResult(
                operation,
                (id ?? string.Empty).Trim(),
                project.ChangeVersion != version,
                views.Count,
                sheets.Count,
                schedules.Count,
                rewrittenPlacementCount,
                rewrittenScheduleReferenceCount);
        }

        private static SemanticDocumentationEditResult Unchanged(string operation, string id, int viewCount, int sheetCount, int scheduleCount)
        {
            return new SemanticDocumentationEditResult(operation, id, false, viewCount, sheetCount, scheduleCount, 0, 0);
        }

        private static List<int> MatchingIndexes<T>(IReadOnlyList<T> items, string? id, Func<T, string> selector, string label)
        {
            var normalized = RequiredId(id, label + "Id");
            var result = new List<int>();
            for (var i = 0; i < items.Count; i++)
                if (string.Equals(selector(items[i]), normalized, StringComparison.OrdinalIgnoreCase)) result.Add(i);
            if (result.Count > 1) throw new InvalidOperationException("Semantic documentation catalog contains duplicate " + label + " id: " + normalized + ".");
            return result;
        }

        private static int CountSheetViewReferences(IEnumerable<SemanticSheetDefinition> sheets, string viewId)
        {
            var count = 0;
            foreach (var sheet in sheets)
                foreach (var placement in sheet.Placements)
                    if (string.Equals(placement.ViewId, viewId, StringComparison.OrdinalIgnoreCase)) count++;
            return count;
        }

        private static int CountScheduleViewReferences(IEnumerable<SemanticScheduleDefinition> schedules, string viewId)
        {
            return schedules.Count(x => string.Equals(x.ViewId, viewId, StringComparison.OrdinalIgnoreCase));
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
                    if (string.Equals(placement.ViewId, oldViewId, StringComparison.OrdinalIgnoreCase))
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

        private static List<SemanticScheduleDefinition> RewriteScheduleReferences(
            IEnumerable<SemanticScheduleDefinition> schedules,
            string oldViewId,
            string newViewId,
            out int rewritten)
        {
            rewritten = 0;
            var result = new List<SemanticScheduleDefinition>();
            foreach (var schedule in schedules)
            {
                if (string.Equals(schedule.ViewId, oldViewId, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(CloneSchedule(schedule, newViewId));
                    rewritten++;
                }
                else result.Add(CloneSchedule(schedule, schedule.ViewId));
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
                    if (string.Equals(placement.ViewId, viewId, StringComparison.OrdinalIgnoreCase))
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

        private static List<SemanticScheduleDefinition> RemoveSchedulesForView(
            IEnumerable<SemanticScheduleDefinition> schedules,
            string viewId,
            out int removed)
        {
            removed = 0;
            var result = new List<SemanticScheduleDefinition>();
            foreach (var schedule in schedules)
            {
                if (string.Equals(schedule.ViewId, viewId, StringComparison.OrdinalIgnoreCase))
                {
                    removed++;
                    continue;
                }
                result.Add(CloneSchedule(schedule, schedule.ViewId));
            }
            return result;
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

        private static SemanticScheduleDefinition CloneSchedule(SemanticScheduleDefinition schedule, string viewId)
        {
            return new SemanticScheduleDefinition(schedule.Id, schedule.Name, viewId, schedule.Columns);
        }

        private static string RequiredId(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Semantic documentation id is required.", name);
            return value!.Trim();
        }
    }
}
