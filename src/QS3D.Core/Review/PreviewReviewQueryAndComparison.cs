using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Review
{
    public sealed class PreviewReviewQueryOptions
    {
        public PreviewReviewQueryOptions(string? searchText = null, string? category = null, string? change = null, string? fieldPrefix = null)
        {
            SearchText = CanonicalOptional(searchText);
            Category = CanonicalOptional(category);
            Change = CanonicalOptional(change);
            FieldPrefix = CanonicalOptional(fieldPrefix);
        }

        public string SearchText { get; }
        public string Category { get; }
        public string Change { get; }
        public string FieldPrefix { get; }

        private static string CanonicalOptional(string? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Trim();
        }
    }

    public sealed class PreviewReviewFacet
    {
        internal PreviewReviewFacet(string key, int count)
        {
            Key = key ?? string.Empty;
            Count = count;
        }

        public string Key { get; }
        public int Count { get; }
    }

    public sealed class PreviewReviewQueryResult
    {
        internal PreviewReviewQueryResult(
            IEnumerable<PreviewReviewEntry> entries,
            IEnumerable<PreviewReviewFacet> changeFacets,
            IEnumerable<PreviewReviewFacet> categoryFacets,
            IEnumerable<PreviewReviewFacet> fieldFacets)
        {
            Entries = (entries ?? Enumerable.Empty<PreviewReviewEntry>()).ToList().AsReadOnly();
            ChangeFacets = (changeFacets ?? Enumerable.Empty<PreviewReviewFacet>()).ToList().AsReadOnly();
            CategoryFacets = (categoryFacets ?? Enumerable.Empty<PreviewReviewFacet>()).ToList().AsReadOnly();
            FieldFacets = (fieldFacets ?? Enumerable.Empty<PreviewReviewFacet>()).ToList().AsReadOnly();
        }

        public IReadOnlyList<PreviewReviewEntry> Entries { get; }
        public IReadOnlyList<PreviewReviewFacet> ChangeFacets { get; }
        public IReadOnlyList<PreviewReviewFacet> CategoryFacets { get; }
        public IReadOnlyList<PreviewReviewFacet> FieldFacets { get; }
        public int Count => Entries.Count;
    }

    public sealed class PreviewReviewQueryService
    {
        internal const int MaximumMaterializedQueryEntries = 10000;

        public PreviewReviewQueryResult Query(PreviewReviewSnapshot snapshot, PreviewReviewQueryOptions? options = null)
        {
            RequireVerified(snapshot);
            var safe = options ?? new PreviewReviewQueryOptions();
            var entries = new List<PreviewReviewEntry>();
            foreach (var entry in snapshot.Entries)
            {
                if (!Matches(entry, safe)) continue;
                if (entries.Count >= MaximumMaterializedQueryEntries)
                    throw new InvalidOperationException(
                        "Preview review query result exceeds the supported materialization bound of " +
                        MaximumMaterializedQueryEntries + ". Narrow the query filters before materializing review rows.");
                entries.Add(entry);
            }

            entries.Sort(CompareEntries);
            return new PreviewReviewQueryResult(
                entries,
                BuildFacets(entries, x => x.Change),
                BuildFacets(entries, x => x.Category),
                BuildFacets(entries, x => x.Field));
        }

        private static int CompareEntries(PreviewReviewEntry left, PreviewReviewEntry right)
        {
            var result = StringComparer.OrdinalIgnoreCase.Compare(left.ElementId, right.ElementId);
            if (result != 0) return result;
            result = StringComparer.OrdinalIgnoreCase.Compare(left.Field, right.Field);
            if (result != 0) return result;
            result = StringComparer.OrdinalIgnoreCase.Compare(left.Change, right.Change);
            if (result != 0) return result;
            result = StringComparer.Ordinal.Compare(left.Before, right.Before);
            if (result != 0) return result;
            return StringComparer.Ordinal.Compare(left.After, right.After);
        }

        private static bool Matches(PreviewReviewEntry entry, PreviewReviewQueryOptions options)
        {
            if (entry == null) return false;
            if (options.Category.Length > 0 && !string.Equals(entry.Category, options.Category, StringComparison.OrdinalIgnoreCase)) return false;
            if (options.Change.Length > 0 && !string.Equals(entry.Change, options.Change, StringComparison.OrdinalIgnoreCase)) return false;
            if (options.FieldPrefix.Length > 0 && !(entry.Field ?? string.Empty).StartsWith(options.FieldPrefix, StringComparison.OrdinalIgnoreCase)) return false;
            if (options.SearchText.Length == 0) return true;

            return Contains(entry.ElementId, options.SearchText)
                || Contains(entry.Category, options.SearchText)
                || Contains(entry.Change, options.SearchText)
                || Contains(entry.Field, options.SearchText)
                || Contains(entry.Before, options.SearchText)
                || Contains(entry.After, options.SearchText)
                || Contains(entry.BeforeProvenance, options.SearchText)
                || Contains(entry.AfterProvenance, options.SearchText);
        }

        private static bool Contains(string? value, string search) =>
            (value ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        private static IReadOnlyList<PreviewReviewFacet> BuildFacets(
            IEnumerable<PreviewReviewEntry> entries,
            Func<PreviewReviewEntry, string> selector)
        {
            return entries
                .GroupBy(x => selector(x) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => new PreviewReviewFacet(x.Key, x.Count()))
                .ToList()
                .AsReadOnly();
        }

        internal static void RequireVerified(PreviewReviewSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!new PreviewReviewSnapshotService().Verify(snapshot))
                throw new InvalidOperationException("Preview review snapshot fingerprint or invariants are invalid.");
        }
    }

    public enum PreviewReviewDeltaKind
    {
        Added = 0,
        Removed = 1,
        Changed = 2,
        Unchanged = 3
    }

    public sealed class PreviewReviewRowDelta
    {
        internal PreviewReviewRowDelta(
            PreviewReviewDeltaKind kind,
            string elementId,
            string field,
            PreviewReviewEntry? baseline,
            PreviewReviewEntry? candidate)
        {
            Kind = kind;
            ElementId = elementId ?? string.Empty;
            Field = field ?? string.Empty;
            Baseline = baseline;
            Candidate = candidate;
        }

        public PreviewReviewDeltaKind Kind { get; }
        public string ElementId { get; }
        public string Field { get; }
        public PreviewReviewEntry? Baseline { get; }
        public PreviewReviewEntry? Candidate { get; }
    }

    public sealed class PreviewReviewSummaryDelta
    {
        internal PreviewReviewSummaryDelta(string field, string baseline, string candidate)
        {
            Field = field ?? string.Empty;
            Baseline = baseline ?? string.Empty;
            Candidate = candidate ?? string.Empty;
        }

        public string Field { get; }
        public string Baseline { get; }
        public string Candidate { get; }
    }

    public sealed class PreviewReviewSnapshotComparison
    {
        internal PreviewReviewSnapshotComparison(
            IEnumerable<PreviewReviewRowDelta> rows,
            IEnumerable<PreviewReviewSummaryDelta> summaryChanges)
        {
            Rows = (rows ?? Enumerable.Empty<PreviewReviewRowDelta>()).ToList().AsReadOnly();
            SummaryChanges = (summaryChanges ?? Enumerable.Empty<PreviewReviewSummaryDelta>()).ToList().AsReadOnly();
        }

        public IReadOnlyList<PreviewReviewRowDelta> Rows { get; }
        public IReadOnlyList<PreviewReviewSummaryDelta> SummaryChanges { get; }
        public int AddedCount => Rows.Count(x => x.Kind == PreviewReviewDeltaKind.Added);
        public int RemovedCount => Rows.Count(x => x.Kind == PreviewReviewDeltaKind.Removed);
        public int ChangedCount => Rows.Count(x => x.Kind == PreviewReviewDeltaKind.Changed);
        public int UnchangedCount => Rows.Count(x => x.Kind == PreviewReviewDeltaKind.Unchanged);
        public bool HasChanges => AddedCount > 0 || RemovedCount > 0 || ChangedCount > 0 || SummaryChanges.Count > 0;
    }

    public sealed class PreviewReviewSnapshotComparisonService
    {
        internal const int MaximumComparisonEntriesPerSnapshot = PreviewReviewQueryService.MaximumMaterializedQueryEntries;

        public PreviewReviewSnapshotComparison Compare(PreviewReviewSnapshot baseline, PreviewReviewSnapshot candidate)
        {
            PreviewReviewQueryService.RequireVerified(baseline);
            PreviewReviewQueryService.RequireVerified(candidate);
            RequireCompatibleScope(baseline, candidate);
            RequireComparisonBound(baseline, "baseline");
            RequireComparisonBound(candidate, "candidate");

            var left = Index(baseline.Entries);
            var right = Index(candidate.Entries);
            var keys = new SortedSet<string>(left.Keys, StringComparer.OrdinalIgnoreCase);
            keys.UnionWith(right.Keys);
            var rows = new List<PreviewReviewRowDelta>();
            foreach (var key in keys)
            {
                left.TryGetValue(key, out var before);
                right.TryGetValue(key, out var after);
                var sample = before ?? after;
                if (sample == null)
                    throw new InvalidOperationException("Preview review comparison index contains a key without a baseline or candidate row: " + key + ".");
                var kind = before == null
                    ? PreviewReviewDeltaKind.Added
                    : after == null
                        ? PreviewReviewDeltaKind.Removed
                        : Equivalent(before, after)
                            ? PreviewReviewDeltaKind.Unchanged
                            : PreviewReviewDeltaKind.Changed;
                rows.Add(new PreviewReviewRowDelta(kind, sample.ElementId, sample.Field, before, after));
            }

            rows = rows
                .OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Field, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Kind)
                .ToList();

            return new PreviewReviewSnapshotComparison(rows, SummaryDiff(baseline, candidate));
        }

        private static void RequireComparisonBound(PreviewReviewSnapshot snapshot, string label)
        {
            if (snapshot.Entries.Count <= MaximumComparisonEntriesPerSnapshot) return;
            throw new InvalidOperationException(
                "Preview review " + label + " snapshot exceeds the supported comparison bound of " +
                MaximumComparisonEntriesPerSnapshot + " entries.");
        }

        private static Dictionary<string, PreviewReviewEntry> Index(IEnumerable<PreviewReviewEntry> entries)
        {
            var result = new Dictionary<string, PreviewReviewEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                var key = RowKey(entry.ElementId, entry.Field);
                if (result.ContainsKey(key))
                    throw new InvalidOperationException("Preview review comparison contains a duplicate element/field row: " + entry.ElementId + "/" + entry.Field + ".");
                result.Add(key, entry);
            }
            return result;
        }

        private static string RowKey(string elementId, string field)
        {
            var safeElementId = elementId ?? string.Empty;
            var safeField = field ?? string.Empty;
            return safeElementId.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ":" + safeElementId
                + safeField.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ":" + safeField;
        }

        private static bool Equivalent(PreviewReviewEntry left, PreviewReviewEntry right)
        {
            return string.Equals(left.ElementId, right.ElementId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Category, right.Category, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Change, right.Change, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Field, right.Field, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Before, right.Before, StringComparison.Ordinal)
                && string.Equals(left.After, right.After, StringComparison.Ordinal)
                && string.Equals(left.BeforeProvenance, right.BeforeProvenance, StringComparison.Ordinal)
                && string.Equals(left.AfterProvenance, right.AfterProvenance, StringComparison.Ordinal);
        }

        private static IReadOnlyList<PreviewReviewSummaryDelta> SummaryDiff(PreviewReviewSnapshot baseline, PreviewReviewSnapshot candidate)
        {
            var result = new List<PreviewReviewSummaryDelta>();
            AddSummary(result, "ChangedElementCount", baseline.ChangedElementCount, candidate.ChangedElementCount);
            AddSummary(result, "RegeneratedElementCount", baseline.RegeneratedElementCount, candidate.RegeneratedElementCount);
            AddSummary(result, "NewHealthIssueCount", baseline.NewHealthIssueCount, candidate.NewHealthIssueCount);
            AddSummary(result, "NewHealthErrorCount", baseline.NewHealthErrorCount, candidate.NewHealthErrorCount);
            AddSummary(result, "ResolvedHealthIssueCount", baseline.ResolvedHealthIssueCount, candidate.ResolvedHealthIssueCount);
            AddSummary(result, "OmittedHandleFieldCount", baseline.OmittedHandleFieldCount, candidate.OmittedHandleFieldCount);
            return result.AsReadOnly();
        }

        private static void AddSummary(ICollection<PreviewReviewSummaryDelta> result, string field, int baseline, int candidate)
        {
            if (baseline == candidate) return;
            result.Add(new PreviewReviewSummaryDelta(field, baseline.ToString(System.Globalization.CultureInfo.InvariantCulture), candidate.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        private static void RequireCompatibleScope(PreviewReviewSnapshot baseline, PreviewReviewSnapshot candidate)
        {
            if (!string.Equals(baseline.ProjectId, candidate.ProjectId, StringComparison.Ordinal))
                throw new InvalidOperationException("Preview review snapshots belong to different projects.");
            if (baseline.Kind != candidate.Kind)
                throw new InvalidOperationException("Preview review snapshots have different review kinds.");
            if (!string.Equals(baseline.Scope, candidate.Scope, StringComparison.Ordinal))
                throw new InvalidOperationException("Preview review snapshots have different scopes.");
            if (!baseline.TargetElementIds.SequenceEqual(candidate.TargetElementIds, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Preview review subset snapshots target different element sets.");
        }
    }
}
