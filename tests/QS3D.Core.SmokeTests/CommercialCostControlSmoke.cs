using System;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialCostControlSmoke
    {
        public static void Run()
        {
            EvaluatesBudgetCvrAndForecastExactly();
            FrozenPeriodRejectsMutationUntilReopened();
            ReopenRequiresFreshRevisionAndReason();
        }

        private static void EvaluatesBudgetCvrAndForecastExactly()
        {
            var period = Period(CommercialControlPeriodStatus.Open, "R1", 450m);
            var result = new CommercialCostControlService().Evaluate(period);

            Equal(1100m, result.RevisedBudget, "Revised budget must include approved variation net change exactly once.");
            Equal(500m, result.CostToDate, "Cost-to-date must reconcile actuals plus accruals.");
            Equal(400m, result.CommittedExposure, "Committed exposure must exclude cost already recognized to date.");
            Equal(950m, result.ForecastFinalCost, "Forecast final cost must reconcile cost-to-date plus forecast-to-complete.");
            Equal(150m, result.ForecastVariance, "Forecast variance must be revised budget less forecast final cost.");
            Equal(150m, result.CvrMargin, "CVR margin must be earned value less cost-to-date.");
        }

        private static void FrozenPeriodRejectsMutationUntilReopened()
        {
            var service = new CommercialCostControlService();
            var open = Period(CommercialControlPeriodStatus.Open, "R1", 450m);
            var frozen = service.Freeze(open, Revision("commercial-control-period", "P-2026-09", "R2"));
            Require(frozen.Status == CommercialControlPeriodStatus.Frozen, "Freeze must return a frozen immutable period.");

            Expect<InvalidOperationException>(
                () => service.ReviseForecast(frozen, 500m, Revision("commercial-control-period", "P-2026-09", "R3")),
                "Frozen commercial-control periods must reject forecast mutation.");

            var reopened = service.Reopen(
                frozen,
                "Forecast updated after approved subcontractor quotation",
                Revision("commercial-control-period", "P-2026-09", "R3"));
            Require(reopened.Status == CommercialControlPeriodStatus.Open, "Reopen must restore editability.");

            var revised = service.ReviseForecast(
                reopened,
                500m,
                Revision("commercial-control-period", "P-2026-09", "R4"));
            Equal(1000m, service.Evaluate(revised).ForecastFinalCost, "Reopened period must accept a new forecast under a fresh revision.");
        }

        private static void ReopenRequiresFreshRevisionAndReason()
        {
            var service = new CommercialCostControlService();
            var frozen = service.Freeze(
                Period(CommercialControlPeriodStatus.Open, "R1", 450m),
                Revision("commercial-control-period", "P-2026-09", "R2"));

            Expect<ArgumentException>(
                () => service.Reopen(frozen, " reason with surrounding whitespace ", Revision("commercial-control-period", "P-2026-09", "R3")),
                "Reopen reason must be canonical audit text.");
            Expect<InvalidOperationException>(
                () => service.Reopen(frozen, "Approved governance reopen", Revision("commercial-control-period", "P-2026-09", "R2")),
                "Reopen must require a fresh revision id.");
        }

        private static CommercialControlPeriod Period(CommercialControlPeriodStatus status, string revision, decimal forecastToComplete)
        {
            return new CommercialControlPeriod(
                "P-2026-09",
                "VND",
                status,
                originalBudget: 1000m,
                approvedVariationNetChange: 100m,
                committedCost: 900m,
                actualCost: 400m,
                accruedCost: 100m,
                earnedValue: 650m,
                forecastCostToComplete: forecastToComplete,
                transitionReason: string.Empty,
                revision: Revision("commercial-control-period", "P-2026-09", revision));
        }

        private static CommercialRevisionRef Revision(string kind, string id, string revision)
        {
            return new CommercialRevisionRef(kind, id, revision);
        }

        private static void Equal(decimal expected, decimal actual, string message)
        {
            if (expected != actual)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void Expect<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new Exception(message);
        }
    }
}
