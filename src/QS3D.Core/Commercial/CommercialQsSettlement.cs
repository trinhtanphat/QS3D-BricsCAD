using System;
using System.Collections.Generic;
using QS3D.Core.Cost;

namespace QS3D.Core.Commercial
{
    public enum CommercialVariationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Superseded = 3
    }

    public sealed class CommercialVariation
    {
        public CommercialVariation(
            string variationId,
            string description,
            string currency,
            decimal proposedAmount,
            decimal approvedAmount,
            CommercialVariationStatus status,
            CommercialRevisionRef revision)
        {
            VariationId = CommercialGuard.RequireToken(variationId, nameof(variationId));
            Description = CommercialGuard.RequireCanonicalText(description, nameof(description));
            Currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            if (!Enum.IsDefined(typeof(CommercialVariationStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            if (revision == null) throw new ArgumentNullException(nameof(revision));
            if (!string.Equals(revision.SourceKind, "variation", StringComparison.Ordinal))
                throw new ArgumentException("Variation revision source kind must be 'variation'.", nameof(revision));
            if (!StringComparer.OrdinalIgnoreCase.Equals(revision.SourceId, VariationId))
                throw new ArgumentException("Variation revision source id must match the variation id.", nameof(revision));
            if (status != CommercialVariationStatus.Approved && approvedAmount != 0m)
                throw new ArgumentException("Only approved variations may carry an approved amount.", nameof(approvedAmount));
            if (status == CommercialVariationStatus.Approved)
                RequireApprovedDirection(proposedAmount, approvedAmount);

            ProposedAmount = proposedAmount;
            ApprovedAmount = approvedAmount;
            Status = status;
            Revision = revision;
        }

        public string VariationId { get; }
        public string Description { get; }
        public string Currency { get; }
        public decimal ProposedAmount { get; }
        public decimal ApprovedAmount { get; }
        public CommercialVariationStatus Status { get; }
        public CommercialRevisionRef Revision { get; }
        public bool IsApproved => Status == CommercialVariationStatus.Approved;

        private static void RequireApprovedDirection(decimal proposedAmount, decimal approvedAmount)
        {
            if (proposedAmount > 0m && approvedAmount < 0m)
                throw new ArgumentException("Approved addition cannot become an omission.", nameof(approvedAmount));
            if (proposedAmount < 0m && approvedAmount > 0m)
                throw new ArgumentException("Approved omission cannot become an addition.", nameof(approvedAmount));
            if (proposedAmount == 0m && approvedAmount != 0m)
                throw new ArgumentException("A zero proposal cannot carry a non-zero approved amount.", nameof(approvedAmount));
        }
    }

    public sealed class CommercialVariationRegister
    {
        private const int MaximumVariations = 10000;
        private readonly IReadOnlyList<CommercialVariation> _variations;
        private readonly Dictionary<string, CommercialVariation> _byId;

        public CommercialVariationRegister(string currency, IEnumerable<CommercialVariation> variations)
        {
            Currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            _variations = CommercialGuard.SnapshotStableGeneration(
                variations,
                nameof(variations),
                MaximumVariations,
                SameVariationState);

            _byId = new Dictionary<string, CommercialVariation>(StringComparer.OrdinalIgnoreCase);
            var approvedNetChange = 0m;
            for (var i = 0; i < _variations.Count; i++)
            {
                var variation = _variations[i];
                if (_byId.ContainsKey(variation.VariationId))
                    throw new ArgumentException(
                        "Duplicate commercial variation id: " + variation.VariationId + ".",
                        nameof(variations));
                _byId.Add(variation.VariationId, variation);
                if (!string.Equals(variation.Currency, Currency, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Variation " + variation.VariationId + " uses currency " + variation.Currency +
                        " but the register currency is " + Currency + ".");
                if (variation.IsApproved)
                {
                    approvedNetChange = CommercialGuard.Add(
                        approvedNetChange,
                        variation.ApprovedAmount,
                        "approved variation net change");
                }
            }

            ApprovedNetChange = approvedNetChange;
        }

        public string Currency { get; }
        public IReadOnlyList<CommercialVariation> Variations => _variations;
        public decimal ApprovedNetChange { get; }

        internal bool TryGet(string variationId, out CommercialVariation variation)
        {
            variationId = CommercialGuard.RequireToken(variationId, nameof(variationId));
            return _byId.TryGetValue(variationId, out variation!);
        }

        private static bool SameVariationState(CommercialVariation left, CommercialVariation right)
        {
            return left != null && right != null &&
                   string.Equals(left.VariationId, right.VariationId, StringComparison.Ordinal) &&
                   string.Equals(left.Description, right.Description, StringComparison.Ordinal) &&
                   string.Equals(left.Currency, right.Currency, StringComparison.Ordinal) &&
                   left.ProposedAmount == right.ProposedAmount &&
                   left.ApprovedAmount == right.ApprovedAmount &&
                   left.Status == right.Status &&
                   SameRevisionState(left.Revision, right.Revision);
        }

        private static bool SameRevisionState(CommercialRevisionRef left, CommercialRevisionRef right)
        {
            return left != null && right != null &&
                   string.Equals(left.SourceKind, right.SourceKind, StringComparison.Ordinal) &&
                   string.Equals(left.SourceId, right.SourceId, StringComparison.Ordinal) &&
                   string.Equals(left.RevisionId, right.RevisionId, StringComparison.Ordinal);
        }
    }

    public sealed class VariationCertificationLine
    {
        public VariationCertificationLine(
            string variationId,
            decimal previousCertified,
            decimal certifiedThisPeriod)
        {
            VariationId = CommercialGuard.RequireToken(variationId, nameof(variationId));
            PreviousCertified = previousCertified;
            CertifiedThisPeriod = certifiedThisPeriod;
        }

        public string VariationId { get; }
        public decimal PreviousCertified { get; }
        public decimal CertifiedThisPeriod { get; }
    }

    public sealed class InterimPaymentCertificate
    {
        internal InterimPaymentCertificate(
            string certificateId,
            string currency,
            ProgressClaimResult progress,
            CommercialVariationRegister variations,
            IReadOnlyList<VariationCertificationLine> variationCertificationLines,
            decimal variationCertifiedThisPeriod,
            decimal grossCertifiedThisPeriod,
            decimal variationRetentionThisPeriod,
            decimal retentionThisPeriod,
            decimal retentionRelease,
            decimal advanceRecovery,
            decimal otherDeductions,
            decimal netCertifiedThisPeriod,
            decimal previousNetCertified,
            decimal cumulativeNetCertified)
        {
            CertificateId = certificateId;
            Currency = currency;
            Progress = progress;
            Variations = variations;
            VariationCertificationLines = variationCertificationLines;
            VariationCertifiedThisPeriod = variationCertifiedThisPeriod;
            GrossCertifiedThisPeriod = grossCertifiedThisPeriod;
            VariationRetentionThisPeriod = variationRetentionThisPeriod;
            RetentionThisPeriod = retentionThisPeriod;
            RetentionRelease = retentionRelease;
            AdvanceRecovery = advanceRecovery;
            OtherDeductions = otherDeductions;
            NetCertifiedThisPeriod = netCertifiedThisPeriod;
            PreviousNetCertified = previousNetCertified;
            CumulativeNetCertified = cumulativeNetCertified;
        }

        public string CertificateId { get; }
        public string Currency { get; }
        public ProgressClaimResult Progress { get; }
        public CommercialVariationRegister Variations { get; }
        public IReadOnlyList<VariationCertificationLine> VariationCertificationLines { get; }
        public decimal VariationCertifiedThisPeriod { get; }
        public decimal GrossCertifiedThisPeriod { get; }
        public decimal VariationRetentionThisPeriod { get; }
        public decimal RetentionThisPeriod { get; }
        public decimal RetentionRelease { get; }
        public decimal AdvanceRecovery { get; }
        public decimal OtherDeductions { get; }
        public decimal NetCertifiedThisPeriod { get; }
        public decimal PreviousNetCertified { get; }
        public decimal CumulativeNetCertified { get; }
    }

    public sealed class InterimPaymentCertificateService
    {
        private const int MaximumVariationLines = 10000;

        public InterimPaymentCertificate Create(
            string certificateId,
            string currency,
            ProgressClaimResult progress,
            CommercialVariationRegister variations,
            IEnumerable<VariationCertificationLine> variationCertificationLines,
            decimal variationRetentionThisPeriod = 0m,
            decimal retentionRelease = 0m,
            decimal advanceRecovery = 0m,
            decimal otherDeductions = 0m,
            decimal previousNetCertified = 0m)
        {
            certificateId = CommercialGuard.RequireToken(certificateId, nameof(certificateId));
            currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (variations == null) throw new ArgumentNullException(nameof(variations));
            if (!string.Equals(currency, variations.Currency, StringComparison.Ordinal))
                throw new InvalidOperationException("IPC currency must match the commercial variation register currency.");
            RequireNonNegative(variationRetentionThisPeriod, nameof(variationRetentionThisPeriod));
            RequireNonNegative(retentionRelease, nameof(retentionRelease));
            RequireNonNegative(advanceRecovery, nameof(advanceRecovery));
            RequireNonNegative(otherDeductions, nameof(otherDeductions));
            RequireNonNegative(previousNetCertified, nameof(previousNetCertified));

            var lines = CommercialGuard.SnapshotStableGeneration(
                variationCertificationLines,
                nameof(variationCertificationLines),
                MaximumVariationLines,
                SameCertificationLineState);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var variationCertifiedThisPeriod = 0m;
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (!seen.Add(line.VariationId))
                    throw new ArgumentException(
                        "Duplicate variation certification id: " + line.VariationId + ".",
                        nameof(variationCertificationLines));
                if (!variations.TryGet(line.VariationId, out var variation))
                    throw new InvalidOperationException(
                        "Variation certification references unknown variation: " + line.VariationId + ".");
                RequireCertifiableVariation(variation, line);
                variationCertifiedThisPeriod = CommercialGuard.Add(
                    variationCertifiedThisPeriod,
                    line.CertifiedThisPeriod,
                    "IPC variation certification total");
            }

            var grossCertifiedThisPeriod = CommercialGuard.Add(
                progress.GrossCertifiedThisPeriod,
                variationCertifiedThisPeriod,
                "IPC gross certified this period");
            var retentionThisPeriod = CommercialGuard.Add(
                progress.RetentionThisPeriod,
                variationRetentionThisPeriod,
                "IPC retention this period");
            var netCertifiedThisPeriod = CommercialGuard.Subtract(
                grossCertifiedThisPeriod,
                retentionThisPeriod,
                "IPC net after retention");
            netCertifiedThisPeriod = CommercialGuard.Add(
                netCertifiedThisPeriod,
                retentionRelease,
                "IPC retention release");
            netCertifiedThisPeriod = CommercialGuard.Subtract(
                netCertifiedThisPeriod,
                advanceRecovery,
                "IPC advance recovery");
            netCertifiedThisPeriod = CommercialGuard.Subtract(
                netCertifiedThisPeriod,
                otherDeductions,
                "IPC other deductions");

            var cumulativeNetCertified = CommercialGuard.Add(
                previousNetCertified,
                netCertifiedThisPeriod,
                "IPC cumulative net certified");
            if (cumulativeNetCertified < 0m)
                throw new InvalidOperationException("IPC cumulative net certified value cannot become negative.");

            return new InterimPaymentCertificate(
                certificateId,
                currency,
                progress,
                variations,
                lines,
                variationCertifiedThisPeriod,
                grossCertifiedThisPeriod,
                variationRetentionThisPeriod,
                retentionThisPeriod,
                retentionRelease,
                advanceRecovery,
                otherDeductions,
                netCertifiedThisPeriod,
                previousNetCertified,
                cumulativeNetCertified);
        }

        private static void RequireCertifiableVariation(
            CommercialVariation variation,
            VariationCertificationLine line)
        {
            if (!variation.IsApproved)
                throw new InvalidOperationException(
                    "Variation " + variation.VariationId + " is not approved and cannot be certified.");

            var approved = variation.ApprovedAmount;
            var previous = line.PreviousCertified;
            var current = line.CertifiedThisPeriod;
            if (approved > 0m)
            {
                if (previous < 0m || current < 0m)
                    throw new InvalidOperationException(
                        "Positive approved variation " + variation.VariationId + " cannot be certified with negative values.");
                var cumulative = CommercialGuard.Add(previous, current, "variation cumulative certification");
                if (cumulative > approved)
                    ThrowOverCertified(variation.VariationId);
                return;
            }

            if (approved < 0m)
            {
                if (previous > 0m || current > 0m)
                    throw new InvalidOperationException(
                        "Approved omission " + variation.VariationId + " cannot be certified with positive values.");
                var cumulative = CommercialGuard.Add(previous, current, "variation cumulative omission certification");
                if (cumulative < approved)
                    ThrowOverCertified(variation.VariationId);
                return;
            }

            if (previous != 0m || current != 0m)
                ThrowOverCertified(variation.VariationId);
        }

        private static void ThrowOverCertified(string variationId)
        {
            throw new InvalidOperationException(
                "Variation " + variationId + " certification exceeds its approved amount.");
        }

        private static bool SameCertificationLineState(
            VariationCertificationLine left,
            VariationCertificationLine right)
        {
            return left != null && right != null &&
                   string.Equals(left.VariationId, right.VariationId, StringComparison.Ordinal) &&
                   left.PreviousCertified == right.PreviousCertified &&
                   left.CertifiedThisPeriod == right.CertifiedThisPeriod;
        }

        private static void RequireNonNegative(decimal value, string parameterName)
        {
            if (value < 0m)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public sealed class FinalAccountResult
    {
        internal FinalAccountResult(
            string finalAccountId,
            string currency,
            decimal originalContractValue,
            decimal approvedVariationNetChange,
            decimal finalAdjustment,
            decimal finalContractValue,
            decimal previousGrossCertified,
            decimal grossBalanceBeforeRetentionAndDeductions,
            decimal retentionHeld,
            decimal retentionRelease,
            decimal unreleasedRetention,
            decimal finalDeductions,
            decimal amountDue,
            decimal recoveryDue)
        {
            FinalAccountId = finalAccountId;
            Currency = currency;
            OriginalContractValue = originalContractValue;
            ApprovedVariationNetChange = approvedVariationNetChange;
            FinalAdjustment = finalAdjustment;
            FinalContractValue = finalContractValue;
            PreviousGrossCertified = previousGrossCertified;
            GrossBalanceBeforeRetentionAndDeductions = grossBalanceBeforeRetentionAndDeductions;
            RetentionHeld = retentionHeld;
            RetentionRelease = retentionRelease;
            UnreleasedRetention = unreleasedRetention;
            FinalDeductions = finalDeductions;
            AmountDue = amountDue;
            RecoveryDue = recoveryDue;
        }

        public string FinalAccountId { get; }
        public string Currency { get; }
        public decimal OriginalContractValue { get; }
        public decimal ApprovedVariationNetChange { get; }
        public decimal FinalAdjustment { get; }
        public decimal FinalContractValue { get; }
        public decimal PreviousGrossCertified { get; }
        public decimal GrossBalanceBeforeRetentionAndDeductions { get; }
        public decimal RetentionHeld { get; }
        public decimal RetentionRelease { get; }
        public decimal UnreleasedRetention { get; }
        public decimal FinalDeductions { get; }
        public decimal AmountDue { get; }
        public decimal RecoveryDue { get; }
    }

    public sealed class FinalAccountService
    {
        public FinalAccountResult Reconcile(
            string finalAccountId,
            string currency,
            decimal originalContractValue,
            CommercialVariationRegister variations,
            decimal finalAdjustment,
            decimal previousGrossCertified,
            decimal retentionHeld,
            decimal retentionRelease,
            decimal finalDeductions)
        {
            finalAccountId = CommercialGuard.RequireToken(finalAccountId, nameof(finalAccountId));
            currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            if (variations == null) throw new ArgumentNullException(nameof(variations));
            if (!string.Equals(currency, variations.Currency, StringComparison.Ordinal))
                throw new InvalidOperationException("Final account currency must match the commercial variation register currency.");
            RequireNonNegative(originalContractValue, nameof(originalContractValue));
            RequireNonNegative(previousGrossCertified, nameof(previousGrossCertified));
            RequireNonNegative(retentionHeld, nameof(retentionHeld));
            RequireNonNegative(retentionRelease, nameof(retentionRelease));
            RequireNonNegative(finalDeductions, nameof(finalDeductions));
            if (retentionRelease > retentionHeld)
                throw new ArgumentOutOfRangeException(
                    nameof(retentionRelease),
                    "Retention release cannot exceed retention held.");

            var finalContractValue = CommercialGuard.Add(
                originalContractValue,
                variations.ApprovedNetChange,
                "final account approved variation reconciliation");
            finalContractValue = CommercialGuard.Add(
                finalContractValue,
                finalAdjustment,
                "final account adjustment reconciliation");
            if (finalContractValue < 0m)
                throw new InvalidOperationException("Final contract value cannot be negative.");

            var grossBalance = CommercialGuard.Subtract(
                finalContractValue,
                previousGrossCertified,
                "final account gross balance");
            var unreleasedRetention = CommercialGuard.Subtract(
                retentionHeld,
                retentionRelease,
                "final account unreleased retention");
            var settlement = CommercialGuard.Add(
                grossBalance,
                retentionRelease,
                "final account retention release");
            settlement = CommercialGuard.Subtract(
                settlement,
                finalDeductions,
                "final account deductions");

            var amountDue = settlement > 0m ? settlement : 0m;
            var recoveryDue = settlement < 0m
                ? CommercialGuard.Subtract(0m, settlement, "final account recovery due")
                : 0m;

            return new FinalAccountResult(
                finalAccountId,
                currency,
                originalContractValue,
                variations.ApprovedNetChange,
                finalAdjustment,
                finalContractValue,
                previousGrossCertified,
                grossBalance,
                retentionHeld,
                retentionRelease,
                unreleasedRetention,
                finalDeductions,
                amountDue,
                recoveryDue);
        }

        private static void RequireNonNegative(decimal value, string parameterName)
        {
            if (value < 0m)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
