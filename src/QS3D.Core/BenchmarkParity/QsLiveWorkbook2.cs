using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public enum LiveWorkbookSourceKind { BimElement, DrawingHandle }
    public enum LiveWorkbookFreshness { Fresh, Refreshed, Stale, MissingSource, Conflict, Error }

    public sealed class LiveWorkbookSourceSnapshot
    {
        public LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind kind, string sourceId, string revision, double quantity, string evidenceReference)
        {
            Kind = kind;
            SourceId = QsModelElementSnapshot.Require(sourceId, "sourceId");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            Quantity = QsModelElementSnapshot.Finite(quantity, "quantity");
            EvidenceReference = QsModelElementSnapshot.Require(evidenceReference, "evidenceReference");
        }

        public LiveWorkbookSourceKind Kind { get; private set; }
        public string SourceId { get; private set; }
        public string Revision { get; private set; }
        public double Quantity { get; private set; }
        public string EvidenceReference { get; private set; }

        internal string Key { get { return Kind + ":" + SourceId; } }
        internal string Fingerprint
        {
            get
            {
                var quantity = Quantity.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                return Segment(Revision) + "|" + Segment(quantity) + "|" + Segment(EvidenceReference);
            }
        }

        private static string Segment(string value)
        {
            return value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + value;
        }
    }

    public sealed class LiveWorkbookBinding
    {
        public LiveWorkbookBinding(
            string bindingId,
            string workbookId,
            string sheet,
            string cell,
            string boqLineId,
            LiveWorkbookSourceKind sourceKind,
            string sourceId,
            string sourceRevision,
            IEnumerable<string> dependsOnBindingIds,
            double multiplier,
            double offset,
            double lastValue)
        {
            BindingId = QsModelElementSnapshot.Require(bindingId, "bindingId");
            WorkbookId = QsModelElementSnapshot.Require(workbookId, "workbookId");
            Sheet = QsModelElementSnapshot.Require(sheet, "sheet");
            Cell = QsModelElementSnapshot.Require(cell, "cell");
            BoqLineId = QsModelElementSnapshot.Optional(boqLineId);
            SourceKind = sourceKind;
            SourceId = QsModelElementSnapshot.Optional(sourceId);
            SourceRevision = QsModelElementSnapshot.Optional(sourceRevision);
            DependsOnBindingIds = new ReadOnlyCollection<string>((dependsOnBindingIds ?? Enumerable.Empty<string>())
                .Select(x => QsModelElementSnapshot.Require(x, "dependsOnBindingIds"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList());
            Multiplier = QsModelElementSnapshot.Finite(multiplier, "multiplier");
            Offset = QsModelElementSnapshot.Finite(offset, "offset");
            LastValue = QsModelElementSnapshot.Finite(lastValue, "lastValue");
        }

        public string BindingId { get; private set; }
        public string WorkbookId { get; private set; }
        public string Sheet { get; private set; }
        public string Cell { get; private set; }
        public string BoqLineId { get; private set; }
        public LiveWorkbookSourceKind SourceKind { get; private set; }
        public string SourceId { get; private set; }
        public string SourceRevision { get; private set; }
        public IReadOnlyList<string> DependsOnBindingIds { get; private set; }
        public double Multiplier { get; private set; }
        public double Offset { get; private set; }
        public double LastValue { get; private set; }

        internal string CellKey { get { return Segment(WorkbookId) + "|" + Segment(Sheet) + "|" + Segment(Cell); } }

        private static string Segment(string value)
        {
            return value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + value;
        }
    }

    public sealed class LiveWorkbookRefreshResult
    {
        public LiveWorkbookRefreshResult(
            LiveWorkbookBinding binding,
            LiveWorkbookFreshness freshness,
            double previousValue,
            double value,
            string resolvedRevision,
            string sourceEvidenceReference,
            IEnumerable<string> trace,
            string message)
        {
            Binding = binding ?? throw new ArgumentNullException("binding");
            Freshness = freshness;
            PreviousValue = QsModelElementSnapshot.Finite(previousValue, "previousValue");
            Value = QsModelElementSnapshot.Finite(value, "value");
            ResolvedRevision = QsModelElementSnapshot.Optional(resolvedRevision);
            SourceEvidenceReference = QsModelElementSnapshot.Optional(sourceEvidenceReference);
            Trace = new ReadOnlyCollection<string>((trace ?? Enumerable.Empty<string>()).ToList());
            Message = QsModelElementSnapshot.Optional(message);
        }

        public LiveWorkbookBinding Binding { get; private set; }
        public LiveWorkbookFreshness Freshness { get; private set; }
        public double PreviousValue { get; private set; }
        public double Value { get; private set; }
        public string ResolvedRevision { get; private set; }
        public string SourceEvidenceReference { get; private set; }
        public IReadOnlyList<string> Trace { get; private set; }
        public string Message { get; private set; }
        public bool IsUsable { get { return Freshness == LiveWorkbookFreshness.Fresh || Freshness == LiveWorkbookFreshness.Refreshed || Freshness == LiveWorkbookFreshness.Stale; } }

        public LiveWorkbookBinding ToNextBinding()
        {
            return new LiveWorkbookBinding(
                Binding.BindingId,
                Binding.WorkbookId,
                Binding.Sheet,
                Binding.Cell,
                Binding.BoqLineId,
                Binding.SourceKind,
                Binding.SourceId,
                ResolvedRevision.Length == 0 ? Binding.SourceRevision : ResolvedRevision,
                Binding.DependsOnBindingIds,
                Binding.Multiplier,
                Binding.Offset,
                Value);
        }
    }

    public sealed class LiveWorkbookRefreshBatch
    {
        public LiveWorkbookRefreshBatch(IEnumerable<LiveWorkbookRefreshResult> results)
        {
            Results = new ReadOnlyCollection<LiveWorkbookRefreshResult>((results ?? throw new ArgumentNullException("results")).ToList());
        }

        public IReadOnlyList<LiveWorkbookRefreshResult> Results { get; private set; }
        public bool HasBlockingFailure { get { return Results.Any(x => x.Freshness == LiveWorkbookFreshness.Conflict || x.Freshness == LiveWorkbookFreshness.Error || x.Freshness == LiveWorkbookFreshness.MissingSource); } }
        public bool HasStaleData { get { return Results.Any(x => x.Freshness == LiveWorkbookFreshness.Stale); } }
    }

    public sealed class LiveWorkbookRefreshEngine2
    {
        private const double Epsilon = 1e-12;

        public LiveWorkbookRefreshBatch Refresh(
            IEnumerable<LiveWorkbookBinding> bindings,
            IEnumerable<LiveWorkbookSourceSnapshot> sources,
            string currentRevision)
        {
            if (bindings == null) throw new ArgumentNullException("bindings");
            if (sources == null) throw new ArgumentNullException("sources");
            currentRevision = QsModelElementSnapshot.Require(currentRevision, "currentRevision");

            var bindingList = bindings.ToList();
            if (bindingList.Any(x => x == null)) throw new ArgumentException("Binding collection contains null.", "bindings");
            var sourceList = sources.ToList();
            if (sourceList.Any(x => x == null)) throw new ArgumentException("Source collection contains null.", "sources");

            var bindingGroups = bindingList.GroupBy(x => x.BindingId, StringComparer.OrdinalIgnoreCase).ToList();
            var duplicateBindingIds = new HashSet<string>(bindingGroups.Where(x => x.Count() > 1).Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            var distinctBindings = bindingGroups.Select(x => x.OrderBy(y => y.CellKey, StringComparer.OrdinalIgnoreCase).First()).ToDictionary(x => x.BindingId, StringComparer.OrdinalIgnoreCase);

            var cellConflicts = new HashSet<string>(bindingList
                .GroupBy(x => x.CellKey, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key), StringComparer.OrdinalIgnoreCase);

            var sourceGroups = sourceList.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
            var sourceConflicts = new HashSet<string>(sourceGroups
                .Where(x => x.Value.Select(y => y.Fingerprint).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                .Select(x => x.Key), StringComparer.OrdinalIgnoreCase);

            var orderedIds = TopologicalOrder(distinctBindings);
            var results = new Dictionary<string, LiveWorkbookRefreshResult>(StringComparer.OrdinalIgnoreCase);

            foreach (var id in orderedIds)
            {
                var binding = distinctBindings[id];
                if (duplicateBindingIds.Contains(id))
                {
                    results[id] = Failure(binding, LiveWorkbookFreshness.Conflict, "Duplicate binding id.");
                    continue;
                }
                if (cellConflicts.Contains(binding.CellKey))
                {
                    results[id] = Failure(binding, LiveWorkbookFreshness.Conflict, "Multiple bindings target the same workbook cell.");
                    continue;
                }

                var missingDependency = binding.DependsOnBindingIds.FirstOrDefault(x => !results.ContainsKey(x));
                if (missingDependency != null)
                {
                    results[id] = Failure(binding, LiveWorkbookFreshness.Error, "Dependency is missing or cyclic: " + missingDependency + ".");
                    continue;
                }
                var failedDependency = binding.DependsOnBindingIds.Select(x => results[x]).FirstOrDefault(x => !x.IsUsable);
                if (failedDependency != null)
                {
                    results[id] = Failure(binding, LiveWorkbookFreshness.Error, "Upstream binding is not usable: " + failedDependency.Binding.BindingId + ".");
                    continue;
                }

                var sourceValue = 0d;
                var sourceRevision = string.Empty;
                var evidence = string.Empty;
                var trace = new List<string>();
                if (binding.SourceId.Length > 0)
                {
                    var key = binding.SourceKind + ":" + binding.SourceId;
                    List<LiveWorkbookSourceSnapshot> candidates;
                    if (!sourceGroups.TryGetValue(key, out candidates))
                    {
                        results[id] = Failure(binding, LiveWorkbookFreshness.MissingSource, "Authoritative source was not found.");
                        continue;
                    }
                    if (sourceConflicts.Contains(key))
                    {
                        results[id] = Failure(binding, LiveWorkbookFreshness.Conflict, "Authoritative source has conflicting snapshots.");
                        continue;
                    }
                    var source = candidates.OrderBy(x => x.Revision, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.EvidenceReference, StringComparer.OrdinalIgnoreCase).First();
                    sourceValue = source.Quantity;
                    sourceRevision = source.Revision;
                    evidence = source.EvidenceReference;
                    trace.Add("source:" + key + "@" + source.Revision);
                }

                var dependencyValue = 0d;
                foreach (var dependencyId in binding.DependsOnBindingIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    var dependency = results[dependencyId];
                    dependencyValue += dependency.Value;
                    trace.Add("binding:" + dependencyId + "=" + dependency.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                }

                var value = (sourceValue + dependencyValue) * binding.Multiplier + binding.Offset;
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    results[id] = Failure(binding, LiveWorkbookFreshness.Error, "Refresh arithmetic produced a non-finite value.");
                    continue;
                }
                var isStale = binding.SourceId.Length > 0 && !string.Equals(sourceRevision, currentRevision, StringComparison.OrdinalIgnoreCase);
                var changed = Math.Abs(value - binding.LastValue) >= Epsilon || (binding.SourceId.Length > 0 && !string.Equals(binding.SourceRevision, sourceRevision, StringComparison.OrdinalIgnoreCase));
                var freshness = isStale ? LiveWorkbookFreshness.Stale : changed ? LiveWorkbookFreshness.Refreshed : LiveWorkbookFreshness.Fresh;
                results[id] = new LiveWorkbookRefreshResult(binding, freshness, binding.LastValue, value, sourceRevision, evidence, trace, isStale ? "Source revision is not current." : changed ? "Binding refreshed deterministically." : "Binding is current.");
            }

            foreach (var unresolved in distinctBindings.Keys.Where(x => !results.ContainsKey(x)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                results[unresolved] = Failure(distinctBindings[unresolved], LiveWorkbookFreshness.Error, "Dependency cycle detected.");

            return new LiveWorkbookRefreshBatch(results.Values.OrderBy(x => x.Binding.WorkbookId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Binding.Sheet, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Binding.Cell, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Binding.BindingId, StringComparer.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<string> TopologicalOrder(IDictionary<string, LiveWorkbookBinding> bindings)
        {
            var indegree = bindings.Keys.ToDictionary(x => x, x => 0, StringComparer.OrdinalIgnoreCase);
            var dependents = bindings.Keys.ToDictionary(x => x, x => new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var binding in bindings.Values)
            {
                foreach (var dependency in binding.DependsOnBindingIds)
                {
                    if (!bindings.ContainsKey(dependency)) continue;
                    indegree[binding.BindingId]++;
                    dependents[dependency].Add(binding.BindingId);
                }
            }

            var ready = new SortedSet<string>(indegree.Where(x => x.Value == 0).Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            var ordered = new List<string>();
            while (ready.Count > 0)
            {
                var id = ready.Min;
                ready.Remove(id);
                ordered.Add(id);
                foreach (var dependent in dependents[id].OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    indegree[dependent]--;
                    if (indegree[dependent] == 0) ready.Add(dependent);
                }
            }
            return new ReadOnlyCollection<string>(ordered);
        }

        private static LiveWorkbookRefreshResult Failure(LiveWorkbookBinding binding, LiveWorkbookFreshness freshness, string message)
        {
            return new LiveWorkbookRefreshResult(binding, freshness, binding.LastValue, binding.LastValue, binding.SourceRevision, string.Empty, new string[0], message);
        }
    }
}
