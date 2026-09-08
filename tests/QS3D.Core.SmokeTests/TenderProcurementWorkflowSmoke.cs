using System;
using QS3D.Core.Commercial;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TenderProcurementWorkflowSmoke
    {
        public static void Run()
        {
            ReusesTenderRankingAndComplianceForRecommendation();
            AwardFailsClosedForIncompleteOrNonCompliantBid();
            DraftPackageCannotBeEvaluated();
        }

        private static void ReusesTenderRankingAndComplianceForRecommendation()
        {
            var package = Package(ProcurementPackageStatus.Closed);
            var evaluation = new TenderProcurementService().Evaluate(
                package,
                new[]
                {
                    new TenderComplianceResponse("BID-A", "INSURANCE", true, "verified"),
                    new TenderComplianceResponse("BID-B", "INSURANCE", true, "verified"),
                    new TenderComplianceResponse("BID-C", "INSURANCE", false, "expired")
                });

            Equal("BID-A", evaluation.RecommendedBidId, "Recommendation must choose the lowest-ranked complete compliant bid.");
            var resultA = evaluation.FindCommercialResult("BID-A");
            Equal(200m, resultA.EvaluatedTotal, "Procurement workflow must reuse tender evaluated totals without recalculation drift.");
            Equal(1, resultA.Rank, "Complete tender ranking must remain owned by TenderEvaluationService.");
            Require(evaluation.FindComplianceResult("BID-A").PassesMandatoryCompliance, "BID-A should pass mandatory compliance.");
            Require(!evaluation.FindComplianceResult("BID-C").PassesMandatoryCompliance, "BID-C should fail mandatory compliance.");

            var award = new TenderProcurementService().Award(
                package,
                evaluation,
                "AWD-001",
                "BID-A",
                Revision("procurement-award", "AWD-001", "R1"));
            Equal("BID-A", award.BidId, "Award must retain selected bid identity.");
            Equal("PKG-01", award.PackageId, "Award must retain procurement package identity.");
            Equal(200m, award.EvaluatedTotal, "Award must retain the Core tender-evaluation total.");
        }

        private static void AwardFailsClosedForIncompleteOrNonCompliantBid()
        {
            var package = Package(ProcurementPackageStatus.Closed);
            var service = new TenderProcurementService();
            var evaluation = service.Evaluate(
                package,
                new[]
                {
                    new TenderComplianceResponse("BID-A", "INSURANCE", true, "verified"),
                    new TenderComplianceResponse("BID-B", "INSURANCE", true, "verified"),
                    new TenderComplianceResponse("BID-C", "INSURANCE", false, "expired")
                });

            Expect<InvalidOperationException>(
                () => service.Award(package, evaluation, "AWD-INCOMPLETE", "BID-B", Revision("procurement-award", "AWD-INCOMPLETE", "R1")),
                "Incomplete commercial bids must not be awardable.");
            Expect<InvalidOperationException>(
                () => service.Award(package, evaluation, "AWD-NONCOMPLIANT", "BID-C", Revision("procurement-award", "AWD-NONCOMPLIANT", "R1")),
                "Mandatory-compliance failures must not be awardable.");
        }

        private static void DraftPackageCannotBeEvaluated()
        {
            Expect<InvalidOperationException>(
                () => new TenderProcurementService().Evaluate(Package(ProcurementPackageStatus.Draft), Array.Empty<TenderComplianceResponse>()),
                "Draft procurement packages must not enter bid evaluation.");
        }

        private static TenderProcurementPackage Package(ProcurementPackageStatus status)
        {
            return new TenderProcurementPackage(
                "PKG-01",
                "Structural works",
                "VND",
                status,
                new[]
                {
                    new TenderRequirement("A", "Item A", "m", 10m),
                    new TenderRequirement("B", "Item B", "m", 5m)
                },
                new[]
                {
                    new TenderComplianceRequirement("INSURANCE", "Valid insurance", true)
                },
                new[]
                {
                    new TenderBid("BID-A", "Alpha", "VND", new[]
                    {
                        new TenderQuoteLine("A", 10m),
                        new TenderQuoteLine("B", 20m)
                    }),
                    new TenderBid("BID-B", "Beta", "VND", new[]
                    {
                        new TenderQuoteLine("A", 5m)
                    }),
                    new TenderBid("BID-C", "Gamma", "VND", new[]
                    {
                        new TenderQuoteLine("A", 8m),
                        new TenderQuoteLine("B", 10m)
                    })
                },
                Revision("procurement-package", "PKG-01", "R3"));
        }

        private static CommercialRevisionRef Revision(string kind, string id, string revision)
        {
            return new CommercialRevisionRef(kind, id, revision);
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(decimal expected, decimal actual, string message)
        {
            if (expected != actual)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(int expected, int actual, string message)
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
