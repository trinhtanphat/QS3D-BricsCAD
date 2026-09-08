using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using QS3D.Core.Cost;

namespace QS3D.Core.Commercial
{
    public enum ProcurementPackageStatus
    {
        Draft = 0,
        Issued = 1,
        Closed = 2,
        Awarded = 3,
        Cancelled = 4
    }

    public sealed class TenderComplianceRequirement
    {
        public TenderComplianceRequirement(string requirementCode, string description, bool isMandatory)
        {
            RequirementCode = CommercialGuard.RequireToken(requirementCode, nameof(requirementCode));
            Description = CommercialGuard.RequireCanonicalText(description, nameof(description));
            IsMandatory = isMandatory;
        }

        public string RequirementCode { get; }
        public string Description { get; }
        public bool IsMandatory { get; }
    }

    public sealed class TenderComplianceResponse
    {
        public TenderComplianceResponse(string bidId, string requirementCode, bool isCompliant, string note)
        {
            BidId = CommercialGuard.RequireToken(bidId, nameof(bidId));
            RequirementCode = CommercialGuard.RequireToken(requirementCode, nameof(requirementCode));
            IsCompliant = isCompliant;
            Note = CommercialGuard.RequireOptionalCanonicalText(note, nameof(note));
        }

        public string BidId { get; }
        public string RequirementCode { get; }
        public bool IsCompliant { get; }
        public string Note { get; }
    }

    public sealed class TenderProcurementPackage
    {
        private const int MaximumEntries = 10000;
        private readonly Dictionary<string, TenderBid> _bidsById;
        private readonly Dictionary<string, TenderComplianceRequirement> _complianceByCode;

        public TenderProcurementPackage(
            string packageId,
            string description,
            string currency,
            ProcurementPackageStatus status,
            IEnumerable<TenderRequirement> requirements,
            IEnumerable<TenderComplianceRequirement> complianceRequirements,
            IEnumerable<TenderBid> bids,
            CommercialRevisionRef revision)
        {
            PackageId = CommercialGuard.RequireToken(packageId, nameof(packageId));
            Description = CommercialGuard.RequireCanonicalText(description, nameof(description));
            Currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            if (!Enum.IsDefined(typeof(ProcurementPackageStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            Status = status;
            Revision = RequireRevision(revision, "procurement-package", PackageId, nameof(revision));

            Requirements = CommercialGuard.SnapshotStableGeneration(
                requirements,
                nameof(requirements),
                MaximumEntries,
                SameRequirementState);
            ComplianceRequirements = CommercialGuard.SnapshotStableGeneration(
                complianceRequirements,
                nameof(complianceRequirements),
                MaximumEntries,
                SameComplianceRequirementState);
            Bids = CommercialGuard.SnapshotStableGeneration(
                bids,
                nameof(bids),
                MaximumEntries,
                SameBidState);

            var requirementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Requirements.Count; i++)
            {
                if (!requirementIds.Add(Requirements[i].ItemCode))
                    throw new ArgumentException("Duplicate tender requirement item code: " + Requirements[i].ItemCode + ".", nameof(requirements));
            }

            _complianceByCode = new Dictionary<string, TenderComplianceRequirement>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < ComplianceRequirements.Count; i++)
            {
                var requirement = ComplianceRequirements[i];
                if (_complianceByCode.ContainsKey(requirement.RequirementCode))
                    throw new ArgumentException("Duplicate tender compliance requirement code: " + requirement.RequirementCode + ".", nameof(complianceRequirements));
                _complianceByCode.Add(requirement.RequirementCode, requirement);
            }

            _bidsById = new Dictionary<string, TenderBid>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Bids.Count; i++)
            {
                var bid = Bids[i];
                if (_bidsById.ContainsKey(bid.BidId))
                    throw new ArgumentException("Duplicate tender bid id: " + bid.BidId + ".", nameof(bids));
                if (!string.Equals(bid.Currency, Currency, StringComparison.Ordinal))
                    throw new InvalidOperationException("Tender bid " + bid.BidId + " currency must match procurement package currency.");
                _bidsById.Add(bid.BidId, bid);
            }
        }

        public string PackageId { get; }
        public string Description { get; }
        public string Currency { get; }
        public ProcurementPackageStatus Status { get; }
        public IReadOnlyList<TenderRequirement> Requirements { get; }
        public IReadOnlyList<TenderComplianceRequirement> ComplianceRequirements { get; }
        public IReadOnlyList<TenderBid> Bids { get; }
        public CommercialRevisionRef Revision { get; }

        internal bool TryGetBid(string bidId, out TenderBid bid)
        {
            return _bidsById.TryGetValue(CommercialGuard.RequireToken(bidId, nameof(bidId)), out bid!);
        }

        internal bool TryGetComplianceRequirement(string code, out TenderComplianceRequirement requirement)
        {
            return _complianceByCode.TryGetValue(CommercialGuard.RequireToken(code, nameof(code)), out requirement!);
        }

        private static CommercialRevisionRef RequireRevision(
            CommercialRevisionRef revision,
            string sourceKind,
            string sourceId,
            string parameterName)
        {
            if (revision == null) throw new ArgumentNullException(parameterName);
            if (!string.Equals(revision.SourceKind, sourceKind, StringComparison.Ordinal))
                throw new ArgumentException("Revision source kind must be '" + sourceKind + "'.", parameterName);
            if (!string.Equals(revision.SourceId, sourceId, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Revision source id must match " + sourceId + ".", parameterName);
            return revision;
        }

        private static bool SameRequirementState(TenderRequirement left, TenderRequirement right)
        {
            return left != null && right != null &&
                string.Equals(left.ItemCode, right.ItemCode, StringComparison.Ordinal) &&
                string.Equals(left.Description, right.Description, StringComparison.Ordinal) &&
                string.Equals(left.Unit, right.Unit, StringComparison.Ordinal) &&
                left.Quantity == right.Quantity;
        }

        private static bool SameComplianceRequirementState(TenderComplianceRequirement left, TenderComplianceRequirement right)
        {
            return left != null && right != null &&
                string.Equals(left.RequirementCode, right.RequirementCode, StringComparison.Ordinal) &&
                string.Equals(left.Description, right.Description, StringComparison.Ordinal) &&
                left.IsMandatory == right.IsMandatory;
        }

        private static bool SameBidState(TenderBid left, TenderBid right)
        {
            if (left == null || right == null ||
                !string.Equals(left.BidId, right.BidId, StringComparison.Ordinal) ||
                !string.Equals(left.Bidder, right.Bidder, StringComparison.Ordinal) ||
                !string.Equals(left.Currency, right.Currency, StringComparison.Ordinal) ||
                left.Lines.Count != right.Lines.Count)
                return false;

            foreach (var pair in left.Lines)
            {
                if (!right.Lines.TryGetValue(pair.Key, out var candidate) ||
                    !string.Equals(pair.Value.ItemCode, candidate.ItemCode, StringComparison.Ordinal) ||
                    pair.Value.UnitRate != candidate.UnitRate)
                    return false;
            }
            return true;
        }
    }

    public sealed class TenderBidComplianceResult
    {
        internal TenderBidComplianceResult(
            string bidId,
            IReadOnlyList<string> missingMandatoryRequirementCodes,
            IReadOnlyList<string> failedMandatoryRequirementCodes)
        {
            BidId = bidId;
            MissingMandatoryRequirementCodes = missingMandatoryRequirementCodes;
            FailedMandatoryRequirementCodes = failedMandatoryRequirementCodes;
        }

        public string BidId { get; }
        public IReadOnlyList<string> MissingMandatoryRequirementCodes { get; }
        public IReadOnlyList<string> FailedMandatoryRequirementCodes { get; }
        public bool PassesMandatoryCompliance =>
            MissingMandatoryRequirementCodes.Count == 0 && FailedMandatoryRequirementCodes.Count == 0;
    }

    public sealed class TenderProcurementEvaluation
    {
        private readonly Dictionary<string, TenderEvaluationResult> _commercialByBid;
        private readonly Dictionary<string, TenderBidComplianceResult> _complianceByBid;

        internal TenderProcurementEvaluation(
            string packageId,
            CommercialRevisionRef packageRevision,
            IReadOnlyList<TenderEvaluationResult> commercialResults,
            IReadOnlyList<TenderBidComplianceResult> complianceResults,
            string recommendedBidId)
        {
            PackageId = packageId;
            PackageRevision = packageRevision;
            CommercialResults = commercialResults;
            ComplianceResults = complianceResults;
            RecommendedBidId = recommendedBidId ?? string.Empty;
            _commercialByBid = new Dictionary<string, TenderEvaluationResult>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < CommercialResults.Count; i++)
                _commercialByBid.Add(CommercialResults[i].BidId, CommercialResults[i]);
            _complianceByBid = new Dictionary<string, TenderBidComplianceResult>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < ComplianceResults.Count; i++)
                _complianceByBid.Add(ComplianceResults[i].BidId, ComplianceResults[i]);
        }

        public string PackageId { get; }
        public CommercialRevisionRef PackageRevision { get; }
        public IReadOnlyList<TenderEvaluationResult> CommercialResults { get; }
        public IReadOnlyList<TenderBidComplianceResult> ComplianceResults { get; }
        public string RecommendedBidId { get; }

        public TenderEvaluationResult FindCommercialResult(string bidId)
        {
            bidId = CommercialGuard.RequireToken(bidId, nameof(bidId));
            if (!_commercialByBid.TryGetValue(bidId, out var result))
                throw new InvalidOperationException("Procurement evaluation has no commercial result for bid " + bidId + ".");
            return result;
        }

        public TenderBidComplianceResult FindComplianceResult(string bidId)
        {
            bidId = CommercialGuard.RequireToken(bidId, nameof(bidId));
            if (!_complianceByBid.TryGetValue(bidId, out var result))
                throw new InvalidOperationException("Procurement evaluation has no compliance result for bid " + bidId + ".");
            return result;
        }
    }

    public sealed class TenderAwardDecision
    {
        internal TenderAwardDecision(
            string awardId,
            string packageId,
            string bidId,
            string bidder,
            string currency,
            decimal evaluatedTotal,
            CommercialRevisionRef packageRevision,
            CommercialRevisionRef awardRevision)
        {
            AwardId = awardId;
            PackageId = packageId;
            BidId = bidId;
            Bidder = bidder;
            Currency = currency;
            EvaluatedTotal = evaluatedTotal;
            PackageRevision = packageRevision;
            AwardRevision = awardRevision;
        }

        public string AwardId { get; }
        public string PackageId { get; }
        public string BidId { get; }
        public string Bidder { get; }
        public string Currency { get; }
        public decimal EvaluatedTotal { get; }
        public CommercialRevisionRef PackageRevision { get; }
        public CommercialRevisionRef AwardRevision { get; }
    }

    public sealed class TenderProcurementService
    {
        private const int MaximumResponses = 10000;

        public TenderProcurementEvaluation Evaluate(
            TenderProcurementPackage package,
            IEnumerable<TenderComplianceResponse> complianceResponses)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            if (package.Status != ProcurementPackageStatus.Closed)
                throw new InvalidOperationException("Tender procurement evaluation requires a closed package.");

            var responses = CommercialGuard.SnapshotStableGeneration(
                complianceResponses,
                nameof(complianceResponses),
                MaximumResponses,
                SameComplianceResponseState);
            var responseByKey = new Dictionary<string, TenderComplianceResponse>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < responses.Count; i++)
            {
                var response = responses[i];
                if (!package.TryGetBid(response.BidId, out _))
                    throw new InvalidOperationException("Compliance response references unknown bid: " + response.BidId + ".");
                if (!package.TryGetComplianceRequirement(response.RequirementCode, out _))
                    throw new InvalidOperationException("Compliance response references unknown requirement: " + response.RequirementCode + ".");
                var key = response.BidId + "\u001f" + response.RequirementCode;
                if (responseByKey.ContainsKey(key))
                    throw new ArgumentException("Duplicate tender compliance response for bid/requirement: " + response.BidId + "/" + response.RequirementCode + ".", nameof(complianceResponses));
                responseByKey.Add(key, response);
            }

            var commercial = new TenderEvaluationService().Evaluate(package.Requirements, package.Bids);
            var compliance = new List<TenderBidComplianceResult>(package.Bids.Count);
            for (var i = 0; i < package.Bids.Count; i++)
            {
                var bid = package.Bids[i];
                var missing = new List<string>();
                var failed = new List<string>();
                for (var j = 0; j < package.ComplianceRequirements.Count; j++)
                {
                    var requirement = package.ComplianceRequirements[j];
                    if (!requirement.IsMandatory)
                        continue;
                    var key = bid.BidId + "\u001f" + requirement.RequirementCode;
                    if (!responseByKey.TryGetValue(key, out var response))
                        missing.Add(requirement.RequirementCode);
                    else if (!response.IsCompliant)
                        failed.Add(requirement.RequirementCode);
                }
                missing.Sort(StringComparer.OrdinalIgnoreCase);
                failed.Sort(StringComparer.OrdinalIgnoreCase);
                compliance.Add(new TenderBidComplianceResult(
                    bid.BidId,
                    new ReadOnlyCollection<string>(missing.ToArray()),
                    new ReadOnlyCollection<string>(failed.ToArray())));
            }
            compliance.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.BidId, right.BidId));

            string recommendedBidId = string.Empty;
            var bestRank = int.MaxValue;
            for (var i = 0; i < commercial.Count; i++)
            {
                var result = commercial[i];
                if (!result.IsComplete || result.Rank <= 0 || result.Rank >= bestRank)
                    continue;
                TenderBidComplianceResult complianceResult = null!;
                for (var j = 0; j < compliance.Count; j++)
                {
                    if (string.Equals(compliance[j].BidId, result.BidId, StringComparison.OrdinalIgnoreCase))
                    {
                        complianceResult = compliance[j];
                        break;
                    }
                }
                if (complianceResult != null && complianceResult.PassesMandatoryCompliance)
                {
                    bestRank = result.Rank;
                    recommendedBidId = result.BidId;
                }
            }

            return new TenderProcurementEvaluation(
                package.PackageId,
                package.Revision,
                commercial,
                new ReadOnlyCollection<TenderBidComplianceResult>(compliance.ToArray()),
                recommendedBidId);
        }

        public TenderAwardDecision Award(
            TenderProcurementPackage package,
            TenderProcurementEvaluation evaluation,
            string awardId,
            string bidId,
            CommercialRevisionRef awardRevision)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            if (evaluation == null) throw new ArgumentNullException(nameof(evaluation));
            if (package.Status != ProcurementPackageStatus.Closed)
                throw new InvalidOperationException("Tender award requires a closed procurement package.");
            awardId = CommercialGuard.RequireToken(awardId, nameof(awardId));
            bidId = CommercialGuard.RequireToken(bidId, nameof(bidId));
            if (!string.Equals(evaluation.PackageId, package.PackageId, StringComparison.OrdinalIgnoreCase) ||
                !SameRevision(evaluation.PackageRevision, package.Revision))
                throw new InvalidOperationException("Tender award evaluation is stale for the procurement package revision.");
            if (!package.TryGetBid(bidId, out var bid))
                throw new InvalidOperationException("Tender award references unknown bid: " + bidId + ".");
            if (awardRevision == null) throw new ArgumentNullException(nameof(awardRevision));
            if (!string.Equals(awardRevision.SourceKind, "procurement-award", StringComparison.Ordinal) ||
                !string.Equals(awardRevision.SourceId, awardId, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Award revision must bind to the procurement-award identity.", nameof(awardRevision));

            var commercial = evaluation.FindCommercialResult(bidId);
            var compliance = evaluation.FindComplianceResult(bidId);
            if (!commercial.IsComplete || commercial.Rank <= 0)
                throw new InvalidOperationException("Incomplete tender bids cannot be awarded.");
            if (!compliance.PassesMandatoryCompliance)
                throw new InvalidOperationException("Tender bids with mandatory compliance failures cannot be awarded.");
            if (!string.Equals(evaluation.RecommendedBidId, bidId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Tender award must select the current deterministic recommended bid.");

            return new TenderAwardDecision(
                awardId,
                package.PackageId,
                bid.BidId,
                bid.Bidder,
                bid.Currency,
                commercial.EvaluatedTotal,
                package.Revision,
                awardRevision);
        }

        private static bool SameComplianceResponseState(TenderComplianceResponse left, TenderComplianceResponse right)
        {
            return left != null && right != null &&
                string.Equals(left.BidId, right.BidId, StringComparison.Ordinal) &&
                string.Equals(left.RequirementCode, right.RequirementCode, StringComparison.Ordinal) &&
                left.IsCompliant == right.IsCompliant &&
                string.Equals(left.Note, right.Note, StringComparison.Ordinal);
        }

        private static bool SameRevision(CommercialRevisionRef left, CommercialRevisionRef right)
        {
            return left != null && right != null &&
                string.Equals(left.SourceKind, right.SourceKind, StringComparison.Ordinal) &&
                string.Equals(left.SourceId, right.SourceId, StringComparison.Ordinal) &&
                string.Equals(left.RevisionId, right.RevisionId, StringComparison.Ordinal);
        }
    }
}
