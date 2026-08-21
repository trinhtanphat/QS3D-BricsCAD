using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using QS3D.Core.Cost;

namespace QS3D.Core.Progress
{
    public enum ClaimSnapshotState
    {
        Draft = 0,
        Frozen = 1,
        Issued = 2
    }

    public sealed class ClaimSnapshotLine
    {
        internal ClaimSnapshotLine(ProgressContractItem contract, ProgressClaimLineResult result)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (!string.Equals(contract.ItemCode, result.ItemCode, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Claim result item code does not match its contract item.", nameof(result));

            ItemCode = contract.ItemCode;
            Unit = contract.Unit;
            ContractQuantity = contract.ContractQuantity;
            UnitRate = contract.UnitRate;
            PreviousCertifiedQuantity = result.PreviousCertifiedQuantity;
            ClaimedThisPeriodQuantity = result.ClaimedThisPeriodQuantity;
            CertifiedThisPeriodQuantity = result.CertifiedThisPeriodQuantity;
            RejectedQuantity = result.RejectedQuantity;
            RemainingQuantity = result.RemainingQuantity;
            CertifiedThisPeriodValue = result.CertifiedThisPeriodValue;

            if (PreviousCertifiedQuantity < 0m || ClaimedThisPeriodQuantity < 0m ||
                CertifiedThisPeriodQuantity < 0m || RejectedQuantity < 0m || RemainingQuantity < 0m ||
                CertifiedThisPeriodValue < 0m)
                throw new ArgumentException("Claim snapshot quantities and values must be non-negative.", nameof(result));

            decimal available;
            decimal expectedRejected;
            decimal expectedRemaining;
            decimal expectedValue;
            try
            {
                available = checked(ContractQuantity - PreviousCertifiedQuantity);
                expectedRejected = checked(ClaimedThisPeriodQuantity - CertifiedThisPeriodQuantity);
                expectedRemaining = checked(available - CertifiedThisPeriodQuantity);
                expectedValue = checked(CertifiedThisPeriodQuantity * UnitRate);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException("Claim snapshot line reconciliation overflowed decimal arithmetic.", ex);
            }

            if (available < 0m)
                throw new ArgumentException("Previous certified quantity exceeds contract quantity.", nameof(result));
            if (CertifiedThisPeriodQuantity > available)
                throw new ArgumentException("Certified quantity exceeds available contract quantity.", nameof(result));
            if (expectedRejected != RejectedQuantity)
                throw new ArgumentException("Claim rejected quantity does not reconcile.", nameof(result));
            if (expectedRemaining != RemainingQuantity)
                throw new ArgumentException("Claim remaining quantity does not reconcile.", nameof(result));
            if (expectedValue != CertifiedThisPeriodValue)
                throw new ArgumentException("Claim certified value does not reconcile with contract rate.", nameof(result));
        }

        public string ItemCode { get; }
        public string Unit { get; }
        public decimal ContractQuantity { get; }
        public decimal UnitRate { get; }
        public decimal PreviousCertifiedQuantity { get; }
        public decimal ClaimedThisPeriodQuantity { get; }
        public decimal CertifiedThisPeriodQuantity { get; }
        public decimal RejectedQuantity { get; }
        public decimal RemainingQuantity { get; }
        public decimal CertifiedThisPeriodValue { get; }

        public string ToCanonicalString()
        {
            var builder = new StringBuilder();
            ProgressDomainContract.AppendToken(builder, "CSL1");
            ProgressDomainContract.AppendToken(builder, ItemCode);
            ProgressDomainContract.AppendToken(builder, Unit);
            ProgressDomainContract.AppendDecimal(builder, ContractQuantity);
            ProgressDomainContract.AppendDecimal(builder, UnitRate);
            ProgressDomainContract.AppendDecimal(builder, PreviousCertifiedQuantity);
            ProgressDomainContract.AppendDecimal(builder, ClaimedThisPeriodQuantity);
            ProgressDomainContract.AppendDecimal(builder, CertifiedThisPeriodQuantity);
            ProgressDomainContract.AppendDecimal(builder, RejectedQuantity);
            ProgressDomainContract.AppendDecimal(builder, RemainingQuantity);
            ProgressDomainContract.AppendDecimal(builder, CertifiedThisPeriodValue);
            return builder.ToString();
        }
    }

    public sealed class ClaimSnapshot
    {
        public ClaimSnapshot(
            string snapshotId,
            string claimId,
            int revision,
            ClaimSnapshotState state,
            ProjectDate periodStart,
            ProjectDate periodEnd,
            string currency,
            ProgressSnapshot sourceProgressSnapshot,
            string estimateSnapshotId,
            ProgressClaimResult evaluatedClaim,
            IEnumerable<ProgressContractItem> contractItems,
            DateTime createdAtUtc,
            string? supersedesSnapshotId = null)
        {
            SnapshotId = ProgressDomainContract.RequireToken(snapshotId, nameof(snapshotId));
            ClaimId = ProgressDomainContract.RequireToken(claimId, nameof(claimId));
            if (revision <= 0) throw new ArgumentOutOfRangeException(nameof(revision));
            if (!Enum.IsDefined(typeof(ClaimSnapshotState), state))
                throw new ArgumentOutOfRangeException(nameof(state));
            Revision = revision;
            State = state;
            PeriodStart = periodStart ?? throw new ArgumentNullException(nameof(periodStart));
            PeriodEnd = periodEnd ?? throw new ArgumentNullException(nameof(periodEnd));
            if (PeriodEnd.CompareTo(PeriodStart) < 0)
                throw new ArgumentException("Claim period end cannot precede period start.", nameof(periodEnd));

            Currency = ProgressDomainContract.RequireCurrency(currency, nameof(currency));
            SourceProgressSnapshot = sourceProgressSnapshot ?? throw new ArgumentNullException(nameof(sourceProgressSnapshot));
            if (SourceProgressSnapshot.DataDate.CompareTo(PeriodEnd) > 0)
                throw new ArgumentException("Claim cannot consume a progress snapshot dated after the claim period.", nameof(sourceProgressSnapshot));
            SourceProgressSnapshotId = SourceProgressSnapshot.SnapshotId;
            SourceProgressDigest = SourceProgressSnapshot.CanonicalDigest;
            EstimateSnapshotId = ProgressDomainContract.RequireToken(estimateSnapshotId, nameof(estimateSnapshotId));
            if (evaluatedClaim == null) throw new ArgumentNullException(nameof(evaluatedClaim));
            CreatedAtUtc = ProgressDomainContract.RequireUtc(createdAtUtc, nameof(createdAtUtc));

            SupersedesSnapshotId = supersedesSnapshotId == null
                ? null
                : ProgressDomainContract.RequireToken(supersedesSnapshotId, nameof(supersedesSnapshotId));
            if (string.Equals(SnapshotId, SupersedesSnapshotId, StringComparison.Ordinal))
                throw new ArgumentException("Claim snapshot cannot supersede itself.", nameof(supersedesSnapshotId));
            if (Revision == 1 && SupersedesSnapshotId != null)
                throw new ArgumentException("Claim revision 1 cannot supersede another snapshot.", nameof(supersedesSnapshotId));
            if (Revision > 1 && SupersedesSnapshotId == null)
                throw new ArgumentException("Claim revisions after 1 require a superseded snapshot id.", nameof(supersedesSnapshotId));

            var contracts = ProgressDomainContract.Snapshot(contractItems, nameof(contractItems), "claim contract items");
            var byCode = new Dictionary<string, ProgressContractItem>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < contracts.Count; i++)
            {
                if (byCode.ContainsKey(contracts[i].ItemCode))
                    throw new ArgumentException("Claim contract items contain a duplicate item code.", nameof(contractItems));
                byCode.Add(contracts[i].ItemCode, contracts[i]);
            }

            var resultLines = evaluatedClaim.Lines;
            if (resultLines.Count > ProgressDomainContract.MaximumEntries)
                throw new ArgumentException("Claim result supports at most " + ProgressDomainContract.MaximumEntries + " lines.", nameof(evaluatedClaim));
            if (resultLines.Count != byCode.Count)
                throw new ArgumentException("Claim result line count does not match the supplied contract snapshot.", nameof(evaluatedClaim));

            var lines = new List<ClaimSnapshotLine>(resultLines.Count);
            var resultCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            decimal gross = 0m;
            for (var i = 0; i < resultLines.Count; i++)
            {
                var resultLine = resultLines[i];
                if (resultLine == null)
                    throw new ArgumentException("Claim result contains a null line.", nameof(evaluatedClaim));
                if (!resultCodes.Add(resultLine.ItemCode))
                    throw new ArgumentException("Claim result contains a duplicate item code.", nameof(evaluatedClaim));
                if (!byCode.TryGetValue(resultLine.ItemCode, out var contract))
                    throw new ArgumentException("Claim result references an unknown contract item.", nameof(evaluatedClaim));

                var line = new ClaimSnapshotLine(contract, resultLine);
                gross = AddPreservingContribution(gross, line.CertifiedThisPeriodValue, "claim snapshot gross value");
                lines.Add(line);
            }

            lines.Sort((left, right) =>
            {
                var compare = StringComparer.OrdinalIgnoreCase.Compare(left.ItemCode, right.ItemCode);
                return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.ItemCode, right.ItemCode);
            });

            if (gross != evaluatedClaim.GrossCertifiedThisPeriod)
                throw new ArgumentException("Claim gross value does not reconcile with its evaluated lines.", nameof(evaluatedClaim));
            if (evaluatedClaim.RetentionPercent < 0m || evaluatedClaim.RetentionPercent > 100m)
                throw new ArgumentException("Claim retention percentage is outside the valid range.", nameof(evaluatedClaim));
            if (evaluatedClaim.RetentionThisPeriod < 0m || evaluatedClaim.NetCertifiedThisPeriod < 0m)
                throw new ArgumentException("Claim retention and net values must be non-negative.", nameof(evaluatedClaim));

            decimal expectedNet;
            try
            {
                expectedNet = checked(evaluatedClaim.GrossCertifiedThisPeriod - evaluatedClaim.RetentionThisPeriod);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException("Claim snapshot net reconciliation overflowed decimal arithmetic.", ex);
            }
            if (expectedNet != evaluatedClaim.NetCertifiedThisPeriod)
                throw new ArgumentException("Claim net value does not reconcile with gross less retention.", nameof(evaluatedClaim));

            Lines = new ReadOnlyCollection<ClaimSnapshotLine>(lines.ToArray());
            GrossCertifiedThisPeriod = evaluatedClaim.GrossCertifiedThisPeriod;
            RetentionPercent = evaluatedClaim.RetentionPercent;
            RetentionThisPeriod = evaluatedClaim.RetentionThisPeriod;
            NetCertifiedThisPeriod = evaluatedClaim.NetCertifiedThisPeriod;
            CanonicalDigest = ProgressDomainContract.Sha256(ToCanonicalStringCore());
        }

        public string SnapshotId { get; }
        public string ClaimId { get; }
        public int Revision { get; }
        public ClaimSnapshotState State { get; }
        public ProjectDate PeriodStart { get; }
        public ProjectDate PeriodEnd { get; }
        public string Currency { get; }
        public ProgressSnapshot SourceProgressSnapshot { get; }
        public string SourceProgressSnapshotId { get; }
        public string SourceProgressDigest { get; }
        public string EstimateSnapshotId { get; }
        public DateTime CreatedAtUtc { get; }
        public string? SupersedesSnapshotId { get; }
        public IReadOnlyList<ClaimSnapshotLine> Lines { get; }
        public decimal GrossCertifiedThisPeriod { get; }
        public decimal RetentionPercent { get; }
        public decimal RetentionThisPeriod { get; }
        public decimal NetCertifiedThisPeriod { get; }
        public string CanonicalDigest { get; }

        public string ToCanonicalString() => ToCanonicalStringCore();

        private string ToCanonicalStringCore()
        {
            var builder = new StringBuilder();
            ProgressDomainContract.AppendToken(builder, "CS1");
            ProgressDomainContract.AppendToken(builder, SnapshotId);
            ProgressDomainContract.AppendToken(builder, ClaimId);
            ProgressDomainContract.AppendInt(builder, Revision);
            ProgressDomainContract.AppendInt(builder, (int)State);
            ProgressDomainContract.AppendToken(builder, PeriodStart.ToString());
            ProgressDomainContract.AppendToken(builder, PeriodEnd.ToString());
            ProgressDomainContract.AppendToken(builder, Currency);
            ProgressDomainContract.AppendToken(builder, SourceProgressSnapshotId);
            ProgressDomainContract.AppendToken(builder, SourceProgressDigest);
            ProgressDomainContract.AppendToken(builder, EstimateSnapshotId);
            ProgressDomainContract.AppendToken(builder, CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
            ProgressDomainContract.AppendNullableToken(builder, SupersedesSnapshotId);
            ProgressDomainContract.AppendDecimal(builder, GrossCertifiedThisPeriod);
            ProgressDomainContract.AppendDecimal(builder, RetentionPercent);
            ProgressDomainContract.AppendDecimal(builder, RetentionThisPeriod);
            ProgressDomainContract.AppendDecimal(builder, NetCertifiedThisPeriod);
            ProgressDomainContract.AppendInt(builder, Lines.Count);
            for (var i = 0; i < Lines.Count; i++)
                ProgressDomainContract.AppendToken(builder, Lines[i].ToCanonicalString());
            return builder.ToString();
        }

        private static decimal AddPreservingContribution(decimal left, decimal right, string label)
        {
            decimal result;
            try
            {
                result = checked(left + right);
            }
            catch (OverflowException ex)
            {
                throw new OverflowException(label + " overflowed decimal arithmetic.", ex);
            }
            if (right != 0m && result == left)
                throw new OverflowException(label + " lost a non-zero contribution.");
            if (left != 0m && result == right)
                throw new OverflowException(label + " lost a non-zero accumulated contribution.");
            return result;
        }
    }
}