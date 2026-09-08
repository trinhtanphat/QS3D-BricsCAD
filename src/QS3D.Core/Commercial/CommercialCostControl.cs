using System;
using QS3D.Core.Cost;

namespace QS3D.Core.Commercial
{
    public enum CommercialControlPeriodStatus
    {
        Open = 0,
        Frozen = 1
    }

    public sealed class CommercialControlPeriod
    {
        public CommercialControlPeriod(
            string periodId,
            string currency,
            CommercialControlPeriodStatus status,
            decimal originalBudget,
            decimal approvedVariationNetChange,
            decimal committedCost,
            decimal actualCost,
            decimal accruedCost,
            decimal earnedValue,
            decimal forecastCostToComplete,
            string transitionReason,
            CommercialRevisionRef revision)
        {
            PeriodId = CommercialGuard.RequireToken(periodId, nameof(periodId));
            Currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            if (!Enum.IsDefined(typeof(CommercialControlPeriodStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            RequireNonNegative(originalBudget, nameof(originalBudget));
            RequireNonNegative(committedCost, nameof(committedCost));
            RequireNonNegative(actualCost, nameof(actualCost));
            RequireNonNegative(accruedCost, nameof(accruedCost));
            RequireNonNegative(earnedValue, nameof(earnedValue));
            RequireNonNegative(forecastCostToComplete, nameof(forecastCostToComplete));

            Revision = RequireRevision(revision, PeriodId, nameof(revision));
            Status = status;
            OriginalBudget = originalBudget;
            ApprovedVariationNetChange = approvedVariationNetChange;
            CommittedCost = committedCost;
            ActualCost = actualCost;
            AccruedCost = accruedCost;
            EarnedValue = earnedValue;
            ForecastCostToComplete = forecastCostToComplete;
            TransitionReason = CommercialGuard.RequireOptionalCanonicalText(transitionReason, nameof(transitionReason));

            var revisedBudget = CommercialGuard.Add(
                OriginalBudget,
                ApprovedVariationNetChange,
                "commercial control revised budget validation");
            if (revisedBudget < 0m)
                throw new InvalidOperationException("Commercial control revised budget cannot be negative.");
        }

        public string PeriodId { get; }
        public string Currency { get; }
        public CommercialControlPeriodStatus Status { get; }
        public decimal OriginalBudget { get; }
        public decimal ApprovedVariationNetChange { get; }
        public decimal CommittedCost { get; }
        public decimal ActualCost { get; }
        public decimal AccruedCost { get; }
        public decimal EarnedValue { get; }
        public decimal ForecastCostToComplete { get; }
        public string TransitionReason { get; }
        public CommercialRevisionRef Revision { get; }

        private static CommercialRevisionRef RequireRevision(
            CommercialRevisionRef revision,
            string periodId,
            string parameterName)
        {
            if (revision == null) throw new ArgumentNullException(parameterName);
            if (!string.Equals(revision.SourceKind, "commercial-control-period", StringComparison.Ordinal))
                throw new ArgumentException("Commercial control revision source kind must be 'commercial-control-period'.", parameterName);
            if (!string.Equals(revision.SourceId, periodId, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Commercial control revision source id must match the period id.", parameterName);
            return revision;
        }

        private static void RequireNonNegative(decimal value, string parameterName)
        {
            if (value < 0m)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public sealed class CommercialCostControlResult
    {
        internal CommercialCostControlResult(
            string periodId,
            string currency,
            CommercialControlPeriodStatus status,
            CommercialRevisionRef revision,
            decimal revisedBudget,
            decimal costToDate,
            decimal committedExposure,
            decimal forecastCostToComplete,
            decimal forecastFinalCost,
            decimal forecastVariance,
            decimal earnedValue,
            decimal cvrMargin)
        {
            PeriodId = periodId;
            Currency = currency;
            Status = status;
            Revision = revision;
            RevisedBudget = revisedBudget;
            CostToDate = costToDate;
            CommittedExposure = committedExposure;
            ForecastCostToComplete = forecastCostToComplete;
            ForecastFinalCost = forecastFinalCost;
            ForecastVariance = forecastVariance;
            EarnedValue = earnedValue;
            CvrMargin = cvrMargin;
        }

        public string PeriodId { get; }
        public string Currency { get; }
        public CommercialControlPeriodStatus Status { get; }
        public CommercialRevisionRef Revision { get; }
        public decimal RevisedBudget { get; }
        public decimal CostToDate { get; }
        public decimal CommittedExposure { get; }
        public decimal ForecastCostToComplete { get; }
        public decimal ForecastFinalCost { get; }
        public decimal ForecastVariance { get; }
        public decimal EarnedValue { get; }
        public decimal CvrMargin { get; }
    }

    public sealed class CommercialCostControlService
    {
        public CommercialCostControlResult Evaluate(CommercialControlPeriod period)
        {
            if (period == null) throw new ArgumentNullException(nameof(period));

            var revisedBudget = CommercialGuard.Add(
                period.OriginalBudget,
                period.ApprovedVariationNetChange,
                "commercial control revised budget");
            if (revisedBudget < 0m)
                throw new InvalidOperationException("Commercial control revised budget cannot be negative.");

            var costToDate = CommercialGuard.Add(
                period.ActualCost,
                period.AccruedCost,
                "commercial control cost to date");
            var committedExposureRaw = CommercialGuard.Subtract(
                period.CommittedCost,
                costToDate,
                "commercial control committed exposure");
            var committedExposure = committedExposureRaw > 0m ? committedExposureRaw : 0m;
            var forecastFinalCost = CommercialGuard.Add(
                costToDate,
                period.ForecastCostToComplete,
                "commercial control forecast final cost");
            var forecastVariance = CommercialGuard.Subtract(
                revisedBudget,
                forecastFinalCost,
                "commercial control forecast variance");
            var cvrMargin = CommercialGuard.Subtract(
                period.EarnedValue,
                costToDate,
                "commercial control CVR margin");

            return new CommercialCostControlResult(
                period.PeriodId,
                period.Currency,
                period.Status,
                period.Revision,
                revisedBudget,
                costToDate,
                committedExposure,
                period.ForecastCostToComplete,
                forecastFinalCost,
                forecastVariance,
                period.EarnedValue,
                cvrMargin);
        }

        public CommercialControlPeriod Freeze(
            CommercialControlPeriod period,
            CommercialRevisionRef newRevision)
        {
            if (period == null) throw new ArgumentNullException(nameof(period));
            if (period.Status != CommercialControlPeriodStatus.Open)
                throw new InvalidOperationException("Only an open commercial-control period can be frozen.");
            RequireFreshRevision(period, newRevision, nameof(newRevision));
            return Copy(
                period,
                CommercialControlPeriodStatus.Frozen,
                period.ForecastCostToComplete,
                "Period frozen",
                newRevision);
        }

        public CommercialControlPeriod Reopen(
            CommercialControlPeriod period,
            string reason,
            CommercialRevisionRef newRevision)
        {
            if (period == null) throw new ArgumentNullException(nameof(period));
            if (period.Status != CommercialControlPeriodStatus.Frozen)
                throw new InvalidOperationException("Only a frozen commercial-control period can be reopened.");
            reason = CommercialGuard.RequireCanonicalText(reason, nameof(reason));
            RequireFreshRevision(period, newRevision, nameof(newRevision));
            return Copy(
                period,
                CommercialControlPeriodStatus.Open,
                period.ForecastCostToComplete,
                reason,
                newRevision);
        }

        public CommercialControlPeriod ReviseForecast(
            CommercialControlPeriod period,
            decimal forecastCostToComplete,
            CommercialRevisionRef newRevision)
        {
            if (period == null) throw new ArgumentNullException(nameof(period));
            if (period.Status != CommercialControlPeriodStatus.Open)
                throw new InvalidOperationException("Frozen commercial-control periods cannot be revised.");
            if (forecastCostToComplete < 0m)
                throw new ArgumentOutOfRangeException(nameof(forecastCostToComplete));
            RequireFreshRevision(period, newRevision, nameof(newRevision));
            return Copy(
                period,
                CommercialControlPeriodStatus.Open,
                forecastCostToComplete,
                "Forecast revised",
                newRevision);
        }

        private static CommercialControlPeriod Copy(
            CommercialControlPeriod source,
            CommercialControlPeriodStatus status,
            decimal forecastCostToComplete,
            string transitionReason,
            CommercialRevisionRef revision)
        {
            return new CommercialControlPeriod(
                source.PeriodId,
                source.Currency,
                status,
                source.OriginalBudget,
                source.ApprovedVariationNetChange,
                source.CommittedCost,
                source.ActualCost,
                source.AccruedCost,
                source.EarnedValue,
                forecastCostToComplete,
                transitionReason,
                revision);
        }

        private static void RequireFreshRevision(
            CommercialControlPeriod period,
            CommercialRevisionRef revision,
            string parameterName)
        {
            if (revision == null) throw new ArgumentNullException(parameterName);
            if (!string.Equals(revision.SourceKind, "commercial-control-period", StringComparison.Ordinal) ||
                !string.Equals(revision.SourceId, period.PeriodId, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Commercial control revision must bind to the current period identity.", parameterName);
            if (string.Equals(revision.RevisionId, period.Revision.RevisionId, StringComparison.Ordinal))
                throw new InvalidOperationException("Commercial control lifecycle transition requires a fresh revision id.");
        }
    }
}
