using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialAggregatePrecisionSmoke
    {
        private const decimal Large = 10000000000000000000000000000m;
        private const decimal ExpectedRecovered = 10000000000000000000000000001m;

        [ModuleInitializer]
        internal static void Initialize()
        {
            TenderPreservesRecoverableContributions();
            TenderCanonicalOrderIsCallerOrderIndependent();
            ProgressPreservesRecoverableContributions();
            ProgressCanonicalOrderIsCallerOrderIndependent();
            OrdinaryControlsRemainExact();
            FinalUnrepresentableTotalsFailClosed();
            Console.WriteLine("PASS commercial aggregate precision");
        }

        private static void TenderPreservesRecoverableContributions()
        {
            var result = EvaluateTender(
                new[]
                {
                    Requirement("A-LARGE"),
                    Requirement("B-HALF"),
                    Requirement("C-HALF")
                },
                new[]
                {
                    Quote("A-LARGE", Large),
                    Quote("B-HALF", 0.5m),
                    Quote("C-HALF", 0.5m)
                });

            Require(result.Count == 1, "precision tender must produce one evaluation result");
            Require(result[0].EvaluatedTotal == ExpectedRecovered,
                "tender evaluated total must preserve recoverable half-unit contributions");
            Require(result[0].Rank == 1, "complete precision tender must retain rank one");
        }

        private static void TenderCanonicalOrderIsCallerOrderIndependent()
        {
            var result = EvaluateTender(
                new[]
                {
                    Requirement("C-HALF"),
                    Requirement("A-LARGE"),
                    Requirement("B-HALF")
                },
                new[]
                {
                    Quote("B-HALF", 0.5m),
                    Quote("C-HALF", 0.5m),
                    Quote("A-LARGE", Large)
                });

            Require(result[0].EvaluatedTotal == ExpectedRecovered,
                "tender exact total must be independent of caller enumeration order");
        }

        private static void ProgressPreservesRecoverableContributions()
        {
            var result = EvaluateProgress(
                new[]
                {
                    Contract("A-LARGE", Large),
                    Contract("B-HALF", 0.5m),
                    Contract("C-HALF", 0.5m)
                },
                new[]
                {
                    Claim("A-LARGE"),
                    Claim("B-HALF"),
                    Claim("C-HALF")
                });

            Require(result.GrossCertifiedThisPeriod == ExpectedRecovered,
                "progress gross certified total must preserve recoverable half-unit contributions");
            Require(result.NetCertifiedThisPeriod == ExpectedRecovered,
                "zero-retention progress net total must equal the exact recovered gross total");
            Require(result.Lines.Count == 3, "precision progress result must preserve all line results");
        }

        private static void ProgressCanonicalOrderIsCallerOrderIndependent()
        {
            var result = EvaluateProgress(
                new[]
                {
                    Contract("C-HALF", 0.5m),
                    Contract("A-LARGE", Large),
                    Contract("B-HALF", 0.5m)
                },
                new[]
                {
                    Claim("B-HALF"),
                    Claim("C-HALF"),
                    Claim("A-LARGE")
                });

            Require(result.GrossCertifiedThisPeriod == ExpectedRecovered,
                "progress exact gross must be independent of caller enumeration order");
        }

        private static void OrdinaryControlsRemainExact()
        {
            var tender = EvaluateTender(
                new[] { Requirement("A"), Requirement("B"), Requirement("C") },
                new[] { Quote("A", 10m), Quote("B", 20m), Quote("C", 30m) });
            Require(tender[0].EvaluatedTotal == 60m, "ordinary tender total must remain exact");

            var progress = EvaluateProgress(
                new[] { Contract("A", 10m), Contract("B", 20m), Contract("C", 30m) },
                new[] { Claim("A"), Claim("B"), Claim("C") },
                10m);
            Require(progress.GrossCertifiedThisPeriod == 60m, "ordinary progress gross must remain exact");
            Require(progress.RetentionThisPeriod == 6m, "ordinary progress retention must remain exact");
            Require(progress.NetCertifiedThisPeriod == 54m, "ordinary progress net must remain exact");
        }

        private static void FinalUnrepresentableTotalsFailClosed()
        {
            RequireOverflow(
                () => EvaluateTender(
                    new[] { Requirement("A-MAX"), Requirement("B-ONE") },
                    new[] { Quote("A-MAX", decimal.MaxValue), Quote("B-ONE", 1m) }),
                "unrepresentable tender total must fail closed");

            RequireOverflow(
                () => EvaluateProgress(
                    new[] { Contract("A-MAX", decimal.MaxValue), Contract("B-ONE", 1m) },
                    new[] { Claim("A-MAX"), Claim("B-ONE") }),
                "unrepresentable progress gross must fail closed");
        }

        private static System.Collections.Generic.IReadOnlyList<TenderEvaluationResult> EvaluateTender(
            TenderRequirement[] requirements,
            TenderQuoteLine[] lines)
        {
            var bid = new TenderBid("precision-bid", "Precision Bidder", "USD", lines);
            return new TenderEvaluationService().Evaluate(requirements, new[] { bid });
        }

        private static ProgressClaimResult EvaluateProgress(
            ProgressContractItem[] contracts,
            ProgressClaimLine[] claims,
            decimal retentionPercent = 0m) =>
            new ProgressClaimService().Evaluate(contracts, claims, retentionPercent);

        private static TenderRequirement Requirement(string code) =>
            new TenderRequirement(code, "Precision requirement " + code, "ea", 1m);

        private static TenderQuoteLine Quote(string code, decimal rate) =>
            new TenderQuoteLine(code, rate);

        private static ProgressContractItem Contract(string code, decimal rate) =>
            new ProgressContractItem(code, "ea", 1m, rate);

        private static ProgressClaimLine Claim(string code) =>
            new ProgressClaimLine(code, 0m, 1m);

        private static void RequireOverflow(Action action, string message)
        {
            try
            {
                action();
            }
            catch (OverflowException)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
