using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Commercial
{
    public enum EstimatingReadinessState
    {
        Unclassified = 0,
        ClassifiedUnpriced = 1,
        Priced = 2,
        PricedWithOverride = 3,
        Blocked = 4,
        Stale = 5
    }

    public sealed class EstimatingLine
    {
        public EstimatingLine(
            string lineId,
            string quantitySourceId,
            string quantityRevision,
            decimal quantity,
            string unit,
            string costCode = "",
            string rateSourceId = "",
            string rateRevision = "",
            decimal? referencedRate = null,
            decimal? overrideRate = null,
            string overrideReason = "",
            bool isBlocked = false,
            string blockReason = "",
            bool isStale = false,
            string staleReason = "")
        {
            LineId = CommercialGuard.RequireToken(lineId, nameof(lineId));
            QuantitySourceId = CommercialGuard.RequireToken(quantitySourceId, nameof(quantitySourceId));
            QuantityRevision = CommercialGuard.RequireToken(quantityRevision, nameof(quantityRevision));
            if (quantity < 0m) throw new ArgumentOutOfRangeException(nameof(quantity));
            Quantity = quantity;
            Unit = CommercialGuard.RequireToken(unit, nameof(unit)).ToLowerInvariant();
            CostCode = CommercialGuard.RequireOptionalToken(costCode, nameof(costCode));
            RateSourceId = CommercialGuard.RequireOptionalToken(rateSourceId, nameof(rateSourceId));
            RateRevision = CommercialGuard.RequireOptionalToken(rateRevision, nameof(rateRevision));
            if (referencedRate.HasValue && referencedRate.Value < 0m)
                throw new ArgumentOutOfRangeException(nameof(referencedRate));
            if (overrideRate.HasValue && overrideRate.Value < 0m)
                throw new ArgumentOutOfRangeException(nameof(overrideRate));
            if (overrideRate.HasValue && !referencedRate.HasValue)
                throw new ArgumentException("A manual override requires a referenced/base rate.", nameof(overrideRate));
            OverrideReason = CommercialGuard.RequireOptionalCanonicalText(overrideReason, nameof(overrideReason));
            if (overrideRate.HasValue && OverrideReason.Length == 0)
                throw new ArgumentException("A manual rate override requires an explicit reason.", nameof(overrideReason));
            if (!overrideRate.HasValue && OverrideReason.Length != 0)
                throw new ArgumentException("An override reason is only valid when an override rate exists.", nameof(overrideReason));
            ReferencedRate = referencedRate;
            OverrideRate = overrideRate;
            BlockReason = CommercialGuard.RequireOptionalCanonicalText(blockReason, nameof(blockReason));
            IsBlocked = isBlocked;
            if (isBlocked && BlockReason.Length == 0)
                throw new ArgumentException("A blocked estimating line requires a block reason.", nameof(blockReason));
            if (!isBlocked && BlockReason.Length != 0)
                throw new ArgumentException("A block reason is only valid when the estimating line is blocked.", nameof(blockReason));
            StaleReason = CommercialGuard.RequireOptionalCanonicalText(staleReason, nameof(staleReason));
            IsStale = isStale;
            if (isStale && StaleReason.Length == 0)
                throw new ArgumentException("A stale estimating line requires a stale reason.", nameof(staleReason));
            if (!isStale && StaleReason.Length != 0)
                throw new ArgumentException("A stale reason is only valid when the estimating line is stale.", nameof(staleReason));

            if (referencedRate.HasValue)
            {
                if (CostCode.Length == 0 || RateSourceId.Length == 0 || RateRevision.Length == 0)
                    throw new ArgumentException("A referenced rate requires cost code, rate source, and rate revision provenance.");
            }
        }

        public string LineId { get; }
        public string QuantitySourceId { get; }
        public string QuantityRevision { get; }
        public decimal Quantity { get; }
        public string Unit { get; }
        public string CostCode { get; }
        public string RateSourceId { get; }
        public string RateRevision { get; }
        public decimal? ReferencedRate { get; }
        public decimal? OverrideRate { get; }
        public string OverrideReason { get; }
        public bool IsBlocked { get; }
        public string BlockReason { get; }
        public bool IsStale { get; }
        public string StaleReason { get; }
        public decimal? EffectiveRate => OverrideRate ?? ReferencedRate;
        public decimal? Amount => EffectiveRate.HasValue
            ? CommercialGuard.Multiply(Quantity, EffectiveRate.Value, "Estimating line amount")
            : (decimal?)null;

        public EstimatingReadinessState State
        {
            get
            {
                if (IsBlocked) return EstimatingReadinessState.Blocked;
                if (IsStale) return EstimatingReadinessState.Stale;
                if (CostCode.Length == 0) return EstimatingReadinessState.Unclassified;
                if (!EffectiveRate.HasValue) return EstimatingReadinessState.ClassifiedUnpriced;
                if (OverrideRate.HasValue) return EstimatingReadinessState.PricedWithOverride;
                return EstimatingReadinessState.Priced;
            }
        }

        internal EstimatingLine WithReferencedRate(string costCode, string rateSourceId, string rateRevision, decimal rate)
        {
            return new EstimatingLine(
                LineId, QuantitySourceId, QuantityRevision, Quantity, Unit,
                costCode, rateSourceId, rateRevision, rate, null, string.Empty,
                IsBlocked, BlockReason, IsStale, StaleReason);
        }

        internal EstimatingLine WithOverride(decimal rate, string reason)
        {
            return new EstimatingLine(
                LineId, QuantitySourceId, QuantityRevision, Quantity, Unit,
                CostCode, RateSourceId, RateRevision, ReferencedRate, rate, reason,
                IsBlocked, BlockReason, IsStale, StaleReason);
        }

        internal EstimatingLine WithoutOverride()
        {
            return new EstimatingLine(
                LineId, QuantitySourceId, QuantityRevision, Quantity, Unit,
                CostCode, RateSourceId, RateRevision, ReferencedRate, null, string.Empty,
                IsBlocked, BlockReason, IsStale, StaleReason);
        }

        internal EstimatingLine WithStaleState(string reason)
        {
            return new EstimatingLine(
                LineId, QuantitySourceId, QuantityRevision, Quantity, Unit,
                CostCode, RateSourceId, RateRevision, ReferencedRate, OverrideRate, OverrideReason,
                IsBlocked, BlockReason, true, reason);
        }
    }

    public sealed class EstimatingPortfolio
    {
        private const int MaximumLines = 10000;
        private readonly IReadOnlyList<EstimatingLine> _lines;
        private readonly Dictionary<string, EstimatingLine> _byId;

        public EstimatingPortfolio(IEnumerable<EstimatingLine> lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));

            var knownCount = SnapshotKnownCount(lines);
            var snapshot = knownCount.HasValue
                ? new List<EstimatingLine>(knownCount.Value)
                : new List<EstimatingLine>();
            _byId = new Dictionary<string, EstimatingLine>(StringComparer.OrdinalIgnoreCase);
            using (var enumerator = lines.GetEnumerator())
            {
                while (true)
                {
                    RequireKnownCountStable(lines, knownCount);
                    if (!enumerator.MoveNext())
                        break;
                    RequireKnownCountStable(lines, knownCount);
                    if (knownCount.HasValue && snapshot.Count >= knownCount.Value)
                        throw new InvalidOperationException("Estimating portfolio line count changed during enumeration.");
                    if (snapshot.Count >= MaximumLines)
                        throw new InvalidOperationException("Estimating portfolio supports at most 10000 lines.");
                    var line = enumerator.Current;
                    RequireKnownCountStable(lines, knownCount);
                    if (line == null) throw new ArgumentException("Estimating portfolio contains a null line.", nameof(lines));
                    if (_byId.ContainsKey(line.LineId))
                        throw new ArgumentException("Duplicate estimating line id: " + line.LineId + ".", nameof(lines));
                    _byId.Add(line.LineId, line);
                    snapshot.Add(line);
                }
            }

            if (knownCount.HasValue && snapshot.Count != knownCount.Value)
                throw new InvalidOperationException("Estimating portfolio line count changed during enumeration.");
            var postTraversalKnownCount = SnapshotKnownCount(lines);
            if (postTraversalKnownCount != knownCount)
                throw new InvalidOperationException("Estimating portfolio known line count changed during enumeration.");

            snapshot.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.LineId, right.LineId));
            _lines = new ReadOnlyCollection<EstimatingLine>(snapshot.ToArray());
        }

        public IReadOnlyList<EstimatingLine> Lines => _lines;

        public EstimatingLine GetLine(string lineId)
        {
            lineId = CommercialGuard.RequireToken(lineId, nameof(lineId));
            if (!_byId.TryGetValue(lineId, out var line))
                throw new KeyNotFoundException("Unknown estimating line id: " + lineId + ".");
            return line;
        }

        public decimal PricedTotal
        {
            get
            {
                var total = new CommercialExactDecimalAccumulator();
                for (var i = 0; i < _lines.Count; i++)
                {
                    var amount = _lines[i].Amount;
                    if (amount.HasValue)
                        total.Add(amount.Value, "Estimating portfolio total");
                }
                return total.ToDecimal("Estimating portfolio total");
            }
        }

        private static void RequireKnownCountStable(IEnumerable<EstimatingLine> lines, int? expectedKnownCount)
        {
            var currentKnownCount = SnapshotKnownCount(lines);
            if (currentKnownCount != expectedKnownCount)
                throw new InvalidOperationException("Estimating portfolio known line count changed during enumeration.");
        }

        private static int? SnapshotKnownCount(IEnumerable<EstimatingLine> lines)
        {
            int? knownCount = null;
            if (lines is ICollection<EstimatingLine> genericCollection)
                AcceptKnownCount(genericCollection.Count, ref knownCount);
            if (lines is IReadOnlyCollection<EstimatingLine> readOnlyCollection)
                AcceptKnownCount(readOnlyCollection.Count, ref knownCount);
            if (lines is System.Collections.ICollection nonGenericCollection)
                AcceptKnownCount(nonGenericCollection.Count, ref knownCount);
            return knownCount;
        }

        private static void AcceptKnownCount(int count, ref int? knownCount)
        {
            if (count < 0)
                throw new InvalidOperationException("Estimating portfolio exposes an invalid negative line count.");
            if (count > MaximumLines)
                throw new InvalidOperationException("Estimating portfolio exceeds the supported 10000-line limit.");
            if (knownCount.HasValue && knownCount.Value != count)
                throw new InvalidOperationException("Estimating portfolio exposes conflicting known line counts.");
            knownCount = count;
        }
    }

    public sealed class UnitRateAssignment
    {
        public UnitRateAssignment(string unit, decimal rate)
        {
            Unit = CommercialGuard.RequireToken(unit, nameof(unit)).ToLowerInvariant();
            if (rate < 0m) throw new ArgumentOutOfRangeException(nameof(rate));
            Rate = rate;
        }

        public string Unit { get; }
        public decimal Rate { get; }
    }

    public sealed class BulkRateAssignmentRequest
    {
        private const int MaximumSelectedLines = 10000;
        private const int MaximumUnitRates = 256;

        public BulkRateAssignmentRequest(
            IEnumerable<string> lineIds,
            string costCode,
            string rateSourceId,
            string rateRevision,
            IEnumerable<UnitRateAssignment> unitRates)
        {
            CostCode = CommercialGuard.RequireToken(costCode, nameof(costCode));
            RateSourceId = CommercialGuard.RequireToken(rateSourceId, nameof(rateSourceId));
            RateRevision = CommercialGuard.RequireToken(rateRevision, nameof(rateRevision));

            if (lineIds == null) throw new ArgumentNullException(nameof(lineIds));
            var lineIdKnownCount = SnapshotKnownCount(lineIds, MaximumSelectedLines, "selected-line");
            var ids = lineIdKnownCount.HasValue
                ? new List<string>(lineIdKnownCount.Value)
                : new List<string>();
            var uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var enumerator = lineIds.GetEnumerator())
            {
                while (true)
                {
                    RequireKnownCountStable(lineIds, lineIdKnownCount, MaximumSelectedLines, "selected-line");
                    if (!enumerator.MoveNext())
                        break;
                    RequireKnownCountStable(lineIds, lineIdKnownCount, MaximumSelectedLines, "selected-line");
                    if (lineIdKnownCount.HasValue && ids.Count >= lineIdKnownCount.Value)
                        throw new InvalidOperationException("Bulk rate assignment selected-line count changed during enumeration.");
                    if (ids.Count >= MaximumSelectedLines)
                        throw new InvalidOperationException("Bulk rate assignment supports at most 10000 selected lines.");
                    var raw = enumerator.Current;
                    RequireKnownCountStable(lineIds, lineIdKnownCount, MaximumSelectedLines, "selected-line");
                    var id = CommercialGuard.RequireToken(raw, nameof(lineIds));
                    if (!uniqueIds.Add(id))
                        throw new ArgumentException("Bulk rate assignment contains duplicate line id: " + id + ".", nameof(lineIds));
                    ids.Add(id);
                }
            }
            if (lineIdKnownCount.HasValue && ids.Count != lineIdKnownCount.Value)
                throw new InvalidOperationException("Bulk rate assignment selected-line count changed during enumeration.");
            var postTraversalLineIdCount = SnapshotKnownCount(lineIds, MaximumSelectedLines, "selected-line");
            if (postTraversalLineIdCount != lineIdKnownCount)
                throw new InvalidOperationException("Bulk rate assignment selected-line known count changed during enumeration.");
            if (ids.Count == 0) throw new ArgumentException("Bulk rate assignment requires at least one selected line.", nameof(lineIds));
            LineIds = new ReadOnlyCollection<string>(ids.ToArray());

            if (unitRates == null) throw new ArgumentNullException(nameof(unitRates));
            var unitRateKnownCount = SnapshotKnownCount(unitRates, MaximumUnitRates, "unit-rate");
            var rates = unitRateKnownCount.HasValue
                ? new List<UnitRateAssignment>(unitRateKnownCount.Value)
                : new List<UnitRateAssignment>();
            var units = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var enumerator = unitRates.GetEnumerator())
            {
                while (true)
                {
                    RequireKnownCountStable(unitRates, unitRateKnownCount, MaximumUnitRates, "unit-rate");
                    if (!enumerator.MoveNext())
                        break;
                    RequireKnownCountStable(unitRates, unitRateKnownCount, MaximumUnitRates, "unit-rate");
                    if (unitRateKnownCount.HasValue && rates.Count >= unitRateKnownCount.Value)
                        throw new InvalidOperationException("Bulk rate assignment unit-rate count changed during enumeration.");
                    if (rates.Count >= MaximumUnitRates)
                        throw new InvalidOperationException("Bulk rate assignment supports at most 256 unit rates.");
                    var assignment = enumerator.Current;
                    RequireKnownCountStable(unitRates, unitRateKnownCount, MaximumUnitRates, "unit-rate");
                    if (assignment == null) throw new ArgumentException("Bulk rate assignment contains a null unit rate.", nameof(unitRates));
                    if (!units.Add(assignment.Unit))
                        throw new ArgumentException("Duplicate unit-rate assignment for unit: " + assignment.Unit + ".", nameof(unitRates));
                    rates.Add(assignment);
                }
            }
            if (unitRateKnownCount.HasValue && rates.Count != unitRateKnownCount.Value)
                throw new InvalidOperationException("Bulk rate assignment unit-rate count changed during enumeration.");
            var postTraversalUnitRateCount = SnapshotKnownCount(unitRates, MaximumUnitRates, "unit-rate");
            if (postTraversalUnitRateCount != unitRateKnownCount)
                throw new InvalidOperationException("Bulk rate assignment unit-rate known count changed during enumeration.");
            UnitRates = new ReadOnlyCollection<UnitRateAssignment>(rates.ToArray());
        }

        public IReadOnlyList<string> LineIds { get; }
        public string CostCode { get; }
        public string RateSourceId { get; }
        public string RateRevision { get; }
        public IReadOnlyList<UnitRateAssignment> UnitRates { get; }

        private static void RequireKnownCountStable<T>(IEnumerable<T> values, int? expectedKnownCount, int maximum, string subject)
        {
            var currentKnownCount = SnapshotKnownCount(values, maximum, subject);
            if (currentKnownCount != expectedKnownCount)
                throw new InvalidOperationException("Bulk rate assignment " + subject + " known count changed during enumeration.");
        }

        private static int? SnapshotKnownCount<T>(IEnumerable<T> values, int maximum, string subject)
        {
            int? knownCount = null;
            if (values is ICollection<T> genericCollection)
                AcceptKnownCount(genericCollection.Count, maximum, subject, ref knownCount);
            if (values is IReadOnlyCollection<T> readOnlyCollection)
                AcceptKnownCount(readOnlyCollection.Count, maximum, subject, ref knownCount);
            if (values is System.Collections.ICollection nonGenericCollection)
                AcceptKnownCount(nonGenericCollection.Count, maximum, subject, ref knownCount);
            return knownCount;
        }

        private static void AcceptKnownCount(int count, int maximum, string subject, ref int? knownCount)
        {
            if (count < 0)
                throw new InvalidOperationException("Bulk rate assignment exposes an invalid negative " + subject + " count.");
            if (count > maximum)
                throw new InvalidOperationException("Bulk rate assignment exceeds the supported " + maximum + " " + subject + " limit.");
            if (knownCount.HasValue && knownCount.Value != count)
                throw new InvalidOperationException("Bulk rate assignment exposes conflicting known " + subject + " counts.");
            knownCount = count;
        }
    }

    public sealed class UnitDistributionItem
    {
        internal UnitDistributionItem(string unit, int lineCount, decimal quantity)
        {
            Unit = unit;
            LineCount = lineCount;
            Quantity = quantity;
        }

        public string Unit { get; }
        public int LineCount { get; }
        public decimal Quantity { get; }
    }

    public sealed class BulkRateAssignmentPreview
    {
        internal BulkRateAssignmentPreview(
            BulkRateAssignmentRequest request,
            IReadOnlyList<EstimatingLine> sourceLines,
            int affectedCount,
            int replacementCount,
            IReadOnlyList<UnitDistributionItem> unitDistribution,
            IReadOnlyList<string> unmatchedLineIds,
            IReadOnlyList<string> blockedLineIds,
            decimal totalBefore,
            decimal totalAfter)
        {
            Request = request;
            SourceLines = sourceLines;
            AffectedCount = affectedCount;
            ReplacementCount = replacementCount;
            UnitDistribution = unitDistribution;
            UnmatchedLineIds = unmatchedLineIds;
            BlockedLineIds = blockedLineIds;
            TotalBefore = totalBefore;
            TotalAfter = totalAfter;
            ValueDelta = CommercialGuard.Subtract(totalAfter, totalBefore, "Bulk rate assignment value delta");
        }

        public BulkRateAssignmentRequest Request { get; }
        internal IReadOnlyList<EstimatingLine> SourceLines { get; }
        public int AffectedCount { get; }
        public int ReplacementCount { get; }
        public IReadOnlyList<UnitDistributionItem> UnitDistribution { get; }
        public IReadOnlyList<string> UnmatchedLineIds { get; }
        public IReadOnlyList<string> BlockedLineIds { get; }
        public decimal TotalBefore { get; }
        public decimal TotalAfter { get; }
        public decimal ValueDelta { get; }
        public bool CanCommit => UnmatchedLineIds.Count == 0 && BlockedLineIds.Count == 0;
    }

    public sealed class EstimatingWorkflowService
    {
        public BulkRateAssignmentPreview PreviewBulkRateAssignment(
            EstimatingPortfolio portfolio,
            BulkRateAssignmentRequest request)
        {
            if (portfolio == null) throw new ArgumentNullException(nameof(portfolio));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var ratesByUnit = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < request.UnitRates.Count; i++)
                ratesByUnit.Add(request.UnitRates[i].Unit, request.UnitRates[i].Rate);

            var sourceLines = new List<EstimatingLine>(request.LineIds.Count);
            var units = new Dictionary<string, UnitAccumulator>(StringComparer.OrdinalIgnoreCase);
            var unmatched = new List<string>();
            var blocked = new List<string>();
            var replacements = 0;
            var before = new CommercialExactDecimalAccumulator();
            var after = new CommercialExactDecimalAccumulator();

            for (var i = 0; i < request.LineIds.Count; i++)
            {
                var line = portfolio.GetLine(request.LineIds[i]);
                sourceLines.Add(line);
                if (!units.TryGetValue(line.Unit, out var aggregate))
                {
                    aggregate = new UnitAccumulator(line.Unit);
                    units.Add(line.Unit, aggregate);
                }
                aggregate.Count++;
                aggregate.Quantity.Add(line.Quantity, "Bulk rate assignment unit quantity");

                var oldAmount = line.Amount;
                if (oldAmount.HasValue)
                    before.Add(oldAmount.Value, "Bulk rate assignment total before");

                if (line.IsBlocked || line.IsStale)
                {
                    blocked.Add(line.LineId);
                    if (oldAmount.HasValue)
                        after.Add(oldAmount.Value, "Bulk rate assignment total after blocked line");
                    continue;
                }

                if (!ratesByUnit.TryGetValue(line.Unit, out var rate))
                {
                    unmatched.Add(line.LineId);
                    if (oldAmount.HasValue)
                        after.Add(oldAmount.Value, "Bulk rate assignment total after unmatched line");
                    continue;
                }

                if (line.CostCode.Length != 0 || line.ReferencedRate.HasValue || line.OverrideRate.HasValue)
                    replacements++;
                var newAmount = CommercialGuard.Multiply(line.Quantity, rate, "Bulk rate assignment preview amount");
                after.Add(newAmount, "Bulk rate assignment total after");
            }

            var distribution = new List<UnitDistributionItem>();
            foreach (var pair in units)
                distribution.Add(new UnitDistributionItem(
                    pair.Value.Unit,
                    pair.Value.Count,
                    pair.Value.Quantity.ToDecimal("Bulk rate assignment unit quantity")));
            distribution.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Unit, right.Unit));
            unmatched.Sort(StringComparer.OrdinalIgnoreCase);
            blocked.Sort(StringComparer.OrdinalIgnoreCase);

            return new BulkRateAssignmentPreview(
                request,
                new ReadOnlyCollection<EstimatingLine>(sourceLines.ToArray()),
                request.LineIds.Count,
                replacements,
                new ReadOnlyCollection<UnitDistributionItem>(distribution.ToArray()),
                new ReadOnlyCollection<string>(unmatched.ToArray()),
                new ReadOnlyCollection<string>(blocked.ToArray()),
                before.ToDecimal("Bulk rate assignment total before"),
                after.ToDecimal("Bulk rate assignment total after"));
        }

        public EstimatingPortfolio CommitBulkRateAssignment(
            EstimatingPortfolio portfolio,
            BulkRateAssignmentPreview preview,
            CommercialAuditLog auditLog,
            string actor,
            string correlationId,
            DateTime occurredUtc)
        {
            if (portfolio == null) throw new ArgumentNullException(nameof(portfolio));
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            if (auditLog == null) throw new ArgumentNullException(nameof(auditLog));
            CommercialGuard.RequireOptionalCanonicalText(actor, nameof(actor));
            CommercialGuard.RequireOptionalToken(correlationId, nameof(correlationId));
            CommercialGuard.RequireUtc(occurredUtc, nameof(occurredUtc));
            if (!preview.CanCommit)
                throw new InvalidOperationException("Bulk rate assignment preview contains blocking or unmatched rows and cannot be committed.");

            var request = preview.Request;
            if (!SourceLinesMatch(preview.SourceLines, portfolio, request))
                throw new InvalidOperationException("Bulk rate assignment preview is stale and must be regenerated before commit.");

            var recomputed = PreviewBulkRateAssignment(portfolio, request);
            if (!recomputed.CanCommit ||
                recomputed.AffectedCount != preview.AffectedCount ||
                recomputed.ReplacementCount != preview.ReplacementCount ||
                recomputed.TotalBefore != preview.TotalBefore ||
                recomputed.TotalAfter != preview.TotalAfter)
                throw new InvalidOperationException("Bulk rate assignment preview is stale and must be regenerated before commit.");

            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < request.UnitRates.Count; i++)
                rates.Add(request.UnitRates[i].Unit, request.UnitRates[i].Rate);
            var selected = new HashSet<string>(request.LineIds, StringComparer.OrdinalIgnoreCase);
            var updated = new List<EstimatingLine>(portfolio.Lines.Count);
            var auditRecords = new List<CommercialAuditRecord>();
            var sourceRevision = new CommercialRevisionRef("rate", request.RateSourceId, request.RateRevision);

            for (var i = 0; i < portfolio.Lines.Count; i++)
            {
                var line = portfolio.Lines[i];
                if (!selected.Contains(line.LineId))
                {
                    updated.Add(line);
                    continue;
                }

                var rate = rates[line.Unit];
                var next = line.WithReferencedRate(request.CostCode, request.RateSourceId, request.RateRevision, rate);
                updated.Add(next);
                auditRecords.Add(new CommercialAuditRecord(
                    "rate-bulk-" + correlationId + "-" + line.LineId,
                    "estimate-line",
                    line.LineId,
                    "rate-assigned",
                    actor,
                    occurredUtc,
                    string.Empty,
                    correlationId,
                    Summarize(line),
                    Summarize(next),
                    new[] { sourceRevision }));
            }

            var result = new EstimatingPortfolio(updated);
            auditLog.AppendBatch(auditRecords);
            return result;
        }

        public EstimatingPortfolio ApplyManualRateOverride(
            EstimatingPortfolio portfolio,
            string lineId,
            decimal overrideRate,
            string reason,
            CommercialAuditLog auditLog,
            string actor,
            string correlationId,
            DateTime occurredUtc)
        {
            if (portfolio == null) throw new ArgumentNullException(nameof(portfolio));
            if (overrideRate < 0m) throw new ArgumentOutOfRangeException(nameof(overrideRate));
            reason = CommercialGuard.RequireCanonicalText(reason, nameof(reason));
            if (auditLog == null) throw new ArgumentNullException(nameof(auditLog));
            actor = CommercialGuard.RequireOptionalCanonicalText(actor, nameof(actor));
            correlationId = CommercialGuard.RequireOptionalToken(correlationId, nameof(correlationId));
            occurredUtc = CommercialGuard.RequireUtc(occurredUtc, nameof(occurredUtc));
            var target = portfolio.GetLine(lineId);
            if (!target.ReferencedRate.HasValue)
                throw new InvalidOperationException("Manual rate override requires an existing referenced/base rate.");
            if (target.IsBlocked)
                throw new InvalidOperationException("A blocked estimating line cannot receive a manual rate override.");

            var next = target.WithOverride(overrideRate, reason);
            var result = Replace(portfolio, next);
            auditLog.Append(new CommercialAuditRecord(
                "rate-override-" + correlationId + "-" + target.LineId,
                "estimate-line",
                target.LineId,
                "rate-override-created",
                actor,
                occurredUtc,
                reason,
                correlationId,
                Summarize(target),
                Summarize(next),
                RateRevisionRefs(next)));
            return result;
        }

        public EstimatingPortfolio RemoveManualRateOverride(
            EstimatingPortfolio portfolio,
            string lineId,
            string reason,
            CommercialAuditLog auditLog,
            string actor,
            string correlationId,
            DateTime occurredUtc)
        {
            if (portfolio == null) throw new ArgumentNullException(nameof(portfolio));
            reason = CommercialGuard.RequireCanonicalText(reason, nameof(reason));
            if (auditLog == null) throw new ArgumentNullException(nameof(auditLog));
            actor = CommercialGuard.RequireOptionalCanonicalText(actor, nameof(actor));
            correlationId = CommercialGuard.RequireOptionalToken(correlationId, nameof(correlationId));
            occurredUtc = CommercialGuard.RequireUtc(occurredUtc, nameof(occurredUtc));
            var target = portfolio.GetLine(lineId);
            if (!target.OverrideRate.HasValue)
                throw new InvalidOperationException("Estimating line does not have a manual rate override.");

            var next = target.WithoutOverride();
            var result = Replace(portfolio, next);
            auditLog.Append(new CommercialAuditRecord(
                "rate-restore-" + correlationId + "-" + target.LineId,
                "estimate-line",
                target.LineId,
                "rate-override-removed",
                actor,
                occurredUtc,
                reason,
                correlationId,
                Summarize(target),
                Summarize(next),
                RateRevisionRefs(next)));
            return result;
        }

        public EstimatingPortfolio MarkQuantitySourceStale(
            EstimatingPortfolio portfolio,
            string lineId,
            string reason)
        {
            if (portfolio == null) throw new ArgumentNullException(nameof(portfolio));
            reason = CommercialGuard.RequireCanonicalText(reason, nameof(reason));
            var line = portfolio.GetLine(lineId);
            return Replace(portfolio, line.WithStaleState(reason));
        }

        private static bool SourceLinesMatch(
            IReadOnlyList<EstimatingLine> expectedLines,
            EstimatingPortfolio portfolio,
            BulkRateAssignmentRequest request)
        {
            if (expectedLines.Count != request.LineIds.Count) return false;

            for (var i = 0; i < expectedLines.Count; i++)
            {
                EstimatingLine current;
                try
                {
                    current = portfolio.GetLine(request.LineIds[i]);
                }
                catch (KeyNotFoundException)
                {
                    return false;
                }

                var expected = expectedLines[i];
                if (!StringComparer.OrdinalIgnoreCase.Equals(expected.LineId, current.LineId) ||
                    !string.Equals(expected.QuantitySourceId, current.QuantitySourceId, StringComparison.Ordinal) ||
                    !string.Equals(expected.QuantityRevision, current.QuantityRevision, StringComparison.Ordinal) ||
                    expected.Quantity != current.Quantity ||
                    !string.Equals(expected.Unit, current.Unit, StringComparison.Ordinal) ||
                    !string.Equals(expected.CostCode, current.CostCode, StringComparison.Ordinal) ||
                    !string.Equals(expected.RateSourceId, current.RateSourceId, StringComparison.Ordinal) ||
                    !string.Equals(expected.RateRevision, current.RateRevision, StringComparison.Ordinal) ||
                    expected.ReferencedRate != current.ReferencedRate ||
                    expected.OverrideRate != current.OverrideRate ||
                    !string.Equals(expected.OverrideReason, current.OverrideReason, StringComparison.Ordinal) ||
                    expected.IsBlocked != current.IsBlocked ||
                    !string.Equals(expected.BlockReason, current.BlockReason, StringComparison.Ordinal) ||
                    expected.IsStale != current.IsStale ||
                    !string.Equals(expected.StaleReason, current.StaleReason, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static EstimatingPortfolio Replace(EstimatingPortfolio portfolio, EstimatingLine replacement)
        {
            var lines = new List<EstimatingLine>(portfolio.Lines.Count);
            for (var i = 0; i < portfolio.Lines.Count; i++)
                lines.Add(StringComparer.OrdinalIgnoreCase.Equals(portfolio.Lines[i].LineId, replacement.LineId)
                    ? replacement
                    : portfolio.Lines[i]);
            return new EstimatingPortfolio(lines);
        }

        private static IReadOnlyList<CommercialRevisionRef> RateRevisionRefs(EstimatingLine line)
        {
            return line.RateSourceId.Length == 0
                ? new ReadOnlyCollection<CommercialRevisionRef>(new CommercialRevisionRef[0])
                : new ReadOnlyCollection<CommercialRevisionRef>(new[]
                {
                    new CommercialRevisionRef("rate", line.RateSourceId, line.RateRevision)
                });
        }

        private static string Summarize(EstimatingLine line)
        {
            return line.State + "|" + line.CostCode + "|" +
                (line.ReferencedRate.HasValue ? line.ReferencedRate.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "") + "|" +
                (line.OverrideRate.HasValue ? line.OverrideRate.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "") + "|" +
                line.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + line.Unit;
        }

        private sealed class UnitAccumulator
        {
            internal UnitAccumulator(string unit)
            {
                Unit = unit;
                Quantity = new CommercialExactDecimalAccumulator();
            }
            internal string Unit { get; }
            internal int Count { get; set; }
            internal CommercialExactDecimalAccumulator Quantity { get; }
        }
    }
}