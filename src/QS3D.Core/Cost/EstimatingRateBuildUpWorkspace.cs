using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Cost
{
    public enum EstimatingResourceCategory
    {
        Material = 0,
        Labour = 1,
        Plant = 2,
        Subcontract = 3
    }

    public enum EstimatingRevisionStatus
    {
        Draft = 0,
        Reviewed = 1,
        Approved = 2
    }

    public sealed class EstimatingRateSource
    {
        public EstimatingRateSource(
            string sourceId,
            string supplier,
            string reference,
            DateTime effectiveFromUtc,
            string currency)
        {
            SourceId = RateBookContract.RequireToken(sourceId, nameof(sourceId));
            Supplier = EstimatingTextContract.RequireText(supplier, nameof(supplier));
            Reference = EstimatingTextContract.RequireText(reference, nameof(reference));
            EffectiveFromUtc = RateBookContract.RequireUtc(effectiveFromUtc, nameof(effectiveFromUtc));
            Currency = RateBookContract.RequireCurrency(currency, nameof(currency));
        }

        public string SourceId { get; }
        public string Supplier { get; }
        public string Reference { get; }
        public DateTime EffectiveFromUtc { get; }
        public string Currency { get; }
    }

    public sealed class EstimatingRateLine
    {
        public EstimatingRateLine(
            string resourceCode,
            EstimatingResourceCategory category,
            string description,
            string unit,
            decimal quantityPerBillUnit,
            decimal unitRate,
            decimal wastagePercent,
            EstimatingRateSource source)
        {
            ResourceCode = RateBookContract.RequireToken(resourceCode, nameof(resourceCode));
            if (!Enum.IsDefined(typeof(EstimatingResourceCategory), category))
                throw new ArgumentOutOfRangeException(nameof(category));
            Category = category;
            Description = EstimatingTextContract.RequireText(description, nameof(description));
            Unit = RateBookContract.RequireLowerToken(unit, nameof(unit));
            if (quantityPerBillUnit < 0m)
                throw new ArgumentOutOfRangeException(nameof(quantityPerBillUnit));
            if (unitRate < 0m)
                throw new ArgumentOutOfRangeException(nameof(unitRate));
            EstimatingTextContract.RequirePercentage(wastagePercent, nameof(wastagePercent));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            QuantityPerBillUnit = quantityPerBillUnit;
            UnitRate = unitRate;
            WastagePercent = wastagePercent;
        }

        public string ResourceCode { get; }
        public EstimatingResourceCategory Category { get; }
        public string Description { get; }
        public string Unit { get; }
        public decimal QuantityPerBillUnit { get; }
        public decimal UnitRate { get; }
        public decimal WastagePercent { get; }
        public EstimatingRateSource Source { get; }

        public decimal EffectiveQuantityPerBillUnit
        {
            get
            {
                var wastage = CostDecimalMath.ApplyPercentagePreservingPrecision(
                    QuantityPerBillUnit,
                    WastagePercent,
                    "estimating resource wastage quantity");
                return CostDecimalMath.AddPreservingNonZeroContribution(
                    QuantityPerBillUnit,
                    wastage,
                    "estimating resource effective quantity");
            }
        }

        internal CostResourceComponent ToCostComponent()
        {
            return new CostResourceComponent(
                ResourceCode,
                Description,
                Unit,
                EffectiveQuantityPerBillUnit,
                UnitRate);
        }
    }

    public sealed class EstimatingRateBuildUpRevision
    {
        private const int MaximumLines = 2000;

        private EstimatingRateBuildUpRevision(
            string buildUpId,
            string revisionId,
            string? previousRevisionId,
            CostCode costCode,
            string billUnit,
            string currency,
            DateTime effectiveFromUtc,
            string author,
            string reason,
            IEnumerable<EstimatingRateLine> lines,
            decimal overheadPercent,
            decimal profitPercent,
            EstimatingRevisionStatus status,
            string? reviewedBy,
            string? reviewNote,
            DateTime? reviewedUtc,
            string? approvedBy,
            string? approvalNote,
            DateTime? approvedUtc)
        {
            BuildUpId = RateBookContract.RequireToken(buildUpId, nameof(buildUpId));
            RevisionId = RateBookContract.RequireToken(revisionId, nameof(revisionId));
            PreviousRevisionId = previousRevisionId == null
                ? null
                : RateBookContract.RequireToken(previousRevisionId, nameof(previousRevisionId));
            if (PreviousRevisionId != null &&
                StringComparer.OrdinalIgnoreCase.Equals(PreviousRevisionId, RevisionId))
            {
                throw new ArgumentException("Previous revision identity must differ from the current revision.", nameof(previousRevisionId));
            }

            CostCode = costCode ?? throw new ArgumentNullException(nameof(costCode));
            BillUnit = RateBookContract.RequireLowerToken(billUnit, nameof(billUnit));
            Currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            EffectiveFromUtc = RateBookContract.RequireUtc(effectiveFromUtc, nameof(effectiveFromUtc));
            Author = EstimatingTextContract.RequireText(author, nameof(author));
            Reason = EstimatingTextContract.RequireText(reason, nameof(reason));
            EstimatingTextContract.RequirePercentage(overheadPercent, nameof(overheadPercent));
            EstimatingTextContract.RequirePercentage(profitPercent, nameof(profitPercent));
            if (!Enum.IsDefined(typeof(EstimatingRevisionStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));

            Lines = SnapshotLines(lines, Currency, EffectiveFromUtc);
            OverheadPercent = overheadPercent;
            ProfitPercent = profitPercent;
            Status = status;
            ReviewedBy = EstimatingTextContract.RequireOptionalText(reviewedBy, nameof(reviewedBy));
            ReviewNote = EstimatingTextContract.RequireOptionalText(reviewNote, nameof(reviewNote));
            ReviewedUtc = EstimatingTextContract.RequireOptionalUtc(reviewedUtc, nameof(reviewedUtc));
            ApprovedBy = EstimatingTextContract.RequireOptionalText(approvedBy, nameof(approvedBy));
            ApprovalNote = EstimatingTextContract.RequireOptionalText(approvalNote, nameof(approvalNote));
            ApprovedUtc = EstimatingTextContract.RequireOptionalUtc(approvedUtc, nameof(approvedUtc));

            ValidateLifecycle();
        }

        public string BuildUpId { get; }
        public string RevisionId { get; }
        public string? PreviousRevisionId { get; }
        public CostCode CostCode { get; }
        public string BillUnit { get; }
        public string Currency { get; }
        public DateTime EffectiveFromUtc { get; }
        public string Author { get; }
        public string Reason { get; }
        public IReadOnlyList<EstimatingRateLine> Lines { get; }
        public decimal OverheadPercent { get; }
        public decimal ProfitPercent { get; }
        public EstimatingRevisionStatus Status { get; }
        public string? ReviewedBy { get; }
        public string? ReviewNote { get; }
        public DateTime? ReviewedUtc { get; }
        public string? ApprovedBy { get; }
        public string? ApprovalNote { get; }
        public DateTime? ApprovedUtc { get; }

        public static EstimatingRateBuildUpRevision CreateDraft(
            string buildUpId,
            string revisionId,
            CostCode costCode,
            string billUnit,
            string currency,
            DateTime effectiveFromUtc,
            string author,
            string reason,
            IEnumerable<EstimatingRateLine> lines,
            decimal overheadPercent = 0m,
            decimal profitPercent = 0m)
        {
            return new EstimatingRateBuildUpRevision(
                buildUpId,
                revisionId,
                null,
                costCode,
                billUnit,
                currency,
                effectiveFromUtc,
                author,
                reason,
                lines,
                overheadPercent,
                profitPercent,
                EstimatingRevisionStatus.Draft,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        public EstimatingRateBuildUpRevision CreateNextRevision(
            string revisionId,
            DateTime effectiveFromUtc,
            string author,
            string reason,
            IEnumerable<EstimatingRateLine> lines,
            decimal overheadPercent,
            decimal profitPercent)
        {
            return new EstimatingRateBuildUpRevision(
                BuildUpId,
                revisionId,
                RevisionId,
                CostCode,
                BillUnit,
                Currency,
                effectiveFromUtc,
                author,
                reason,
                lines,
                overheadPercent,
                profitPercent,
                EstimatingRevisionStatus.Draft,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        public EstimatingRateBuildUpRevision MarkReviewed(
            string reviewer,
            string note,
            DateTime reviewedUtc)
        {
            if (Status != EstimatingRevisionStatus.Draft)
                throw new InvalidOperationException("Only a draft estimating revision can be reviewed.");
            var reviewerText = EstimatingTextContract.RequireText(reviewer, nameof(reviewer));
            var noteText = EstimatingTextContract.RequireText(note, nameof(note));
            var timestamp = RateBookContract.RequireUtc(reviewedUtc, nameof(reviewedUtc));
            if (timestamp < EffectiveFromUtc)
                throw new ArgumentException("Review timestamp cannot precede revision effective time.", nameof(reviewedUtc));

            return WithLifecycle(
                EstimatingRevisionStatus.Reviewed,
                reviewerText,
                noteText,
                timestamp,
                null,
                null,
                null);
        }

        public EstimatingRateBuildUpRevision MarkApproved(
            string approver,
            string note,
            DateTime approvedUtc)
        {
            if (Status != EstimatingRevisionStatus.Reviewed)
                throw new InvalidOperationException("Only a reviewed estimating revision can be approved.");
            var approverText = EstimatingTextContract.RequireText(approver, nameof(approver));
            var noteText = EstimatingTextContract.RequireText(note, nameof(note));
            var timestamp = RateBookContract.RequireUtc(approvedUtc, nameof(approvedUtc));
            if (!ReviewedUtc.HasValue || timestamp < ReviewedUtc.Value)
                throw new ArgumentException("Approval timestamp cannot precede review time.", nameof(approvedUtc));

            return WithLifecycle(
                EstimatingRevisionStatus.Approved,
                ReviewedBy,
                ReviewNote,
                ReviewedUtc,
                approverText,
                noteText,
                timestamp);
        }

        public CostRateBuildUp Evaluate()
        {
            var components = new CostResourceComponent[Lines.Count];
            for (var i = 0; i < Lines.Count; i++)
                components[i] = Lines[i].ToCostComponent();

            return new CostRateBuildUp(
                RevisionId,
                CostCode,
                BillUnit,
                Currency,
                components,
                OverheadPercent,
                ProfitPercent);
        }

        private EstimatingRateBuildUpRevision WithLifecycle(
            EstimatingRevisionStatus status,
            string? reviewedBy,
            string? reviewNote,
            DateTime? reviewedUtc,
            string? approvedBy,
            string? approvalNote,
            DateTime? approvedUtc)
        {
            return new EstimatingRateBuildUpRevision(
                BuildUpId,
                RevisionId,
                PreviousRevisionId,
                CostCode,
                BillUnit,
                Currency,
                EffectiveFromUtc,
                Author,
                Reason,
                Lines,
                OverheadPercent,
                ProfitPercent,
                status,
                reviewedBy,
                reviewNote,
                reviewedUtc,
                approvedBy,
                approvalNote,
                approvedUtc);
        }

        private void ValidateLifecycle()
        {
            if (Status == EstimatingRevisionStatus.Draft)
            {
                if (ReviewedBy != null || ReviewNote != null || ReviewedUtc.HasValue ||
                    ApprovedBy != null || ApprovalNote != null || ApprovedUtc.HasValue)
                {
                    throw new ArgumentException("Draft estimating revision cannot carry review or approval metadata.");
                }
                return;
            }

            if (ReviewedBy == null || ReviewNote == null || !ReviewedUtc.HasValue)
                throw new ArgumentException("Reviewed estimating revision requires complete review metadata.");
            if (ReviewedUtc.Value < EffectiveFromUtc)
                throw new ArgumentException("Review timestamp cannot precede revision effective time.");

            if (Status == EstimatingRevisionStatus.Reviewed)
            {
                if (ApprovedBy != null || ApprovalNote != null || ApprovedUtc.HasValue)
                    throw new ArgumentException("Reviewed estimating revision cannot carry approval metadata.");
                return;
            }

            if (ApprovedBy == null || ApprovalNote == null || !ApprovedUtc.HasValue)
                throw new ArgumentException("Approved estimating revision requires complete approval metadata.");
            if (ApprovedUtc.Value < ReviewedUtc.Value)
                throw new ArgumentException("Approval timestamp cannot precede review time.");
        }

        private static IReadOnlyList<EstimatingRateLine> SnapshotLines(
            IEnumerable<EstimatingRateLine> lines,
            string currency,
            DateTime effectiveFromUtc)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            var snapshot = new List<EstimatingRateLine>();
            var resourceCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var enumerator = lines.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (snapshot.Count >= MaximumLines)
                        throw new InvalidOperationException("Estimating rate build-up supports at most " + MaximumLines + " resource lines.");
                    var line = enumerator.Current;
                    if (line == null)
                        throw new ArgumentException("Estimating rate build-up contains a null resource line.", nameof(lines));
                    if (!resourceCodes.Add(line.ResourceCode))
                        throw new ArgumentException("Duplicate estimating resource code: " + line.ResourceCode + ".", nameof(lines));
                    if (!StringComparer.Ordinal.Equals(line.Source.Currency, currency))
                        throw new ArgumentException("Resource source currency must match the build-up currency.", nameof(lines));
                    if (line.Source.EffectiveFromUtc > effectiveFromUtc)
                        throw new ArgumentException("Resource source effective time cannot be later than the build-up revision.", nameof(lines));
                    snapshot.Add(line);
                }
            }

            if (snapshot.Count == 0)
                throw new ArgumentException("Estimating rate build-up requires at least one resource line.", nameof(lines));
            snapshot.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.ResourceCode, right.ResourceCode));
            return new ReadOnlyCollection<EstimatingRateLine>(snapshot.ToArray());
        }
    }

    internal static class EstimatingTextContract
    {
        private const int MaximumTextLength = 512;

        public static string RequireText(string value, string paramName)
        {
            if (value == null) throw new ArgumentNullException(paramName);
            if (value.Length == 0 || value.Length > MaximumTextLength ||
                !StringComparer.Ordinal.Equals(value, value.Trim()))
            {
                throw new ArgumentException("Estimating text must be non-empty, bounded and canonically trimmed.", paramName);
            }
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                    throw new ArgumentException("Estimating text cannot contain control characters.", paramName);
            }
            return value;
        }

        public static string? RequireOptionalText(string? value, string paramName)
        {
            return value == null ? null : RequireText(value, paramName);
        }

        public static DateTime? RequireOptionalUtc(DateTime? value, string paramName)
        {
            if (!value.HasValue) return null;
            return RateBookContract.RequireUtc(value.Value, paramName);
        }

        public static void RequirePercentage(decimal value, string paramName)
        {
            if (value < 0m || value > 100m)
                throw new ArgumentOutOfRangeException(paramName, value, "Percentage must be between 0 and 100.");
            if (value > 0m && value / 100m == 0m)
                throw new ArgumentOutOfRangeException(paramName, value, "Positive percentage is too small to preserve at decimal precision.");
        }
    }
}
