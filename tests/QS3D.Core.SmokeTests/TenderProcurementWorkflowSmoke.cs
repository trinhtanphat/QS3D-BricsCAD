using System;
using System.Collections;
using System.Collections.Generic;
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
            ReboundKnownCountDoesNotConsumeSurplusCurrent();
            ZeroKnownCountReboundConsumesNoCurrent();
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
            var resultC = evaluation.FindCommercialResult("BID-C");
            Equal(200m, resultA.EvaluatedTotal, "Procurement workflow must reuse tender evaluated totals without recalculation drift.");
            Equal(2, resultA.Rank, "Compliance gating must not rewrite the existing commercial tender rank.");
            Equal(1, resultC.Rank, "The lowest complete commercial bid must retain its TenderEvaluationService rank even when compliance later fails.");
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

        private static void ReboundKnownCountDoesNotConsumeSurplusCurrent()
        {
            var responses = HostileResponses(1, 2);

            Expect<InvalidOperationException>(
                () => new TenderProcurementService().Evaluate(Package(ProcurementPackageStatus.Closed), responses),
                "A commercial snapshot whose known Count changes after admission must fail closed.");

            Require(
                responses.CurrentReads <= 1,
                "A rebound known Count must not authorize a surplus Current read beyond the originally admitted cardinality.");
            RequireDisposed(responses);
        }

        private static void ZeroKnownCountReboundConsumesNoCurrent()
        {
            var responses = HostileResponses(0, 2);

            Expect<InvalidOperationException>(
                () => new TenderProcurementService().Evaluate(Package(ProcurementPackageStatus.Closed), responses),
                "A zero-count commercial snapshot that rebounds before traversal must fail closed.");

            Equal(0, responses.CurrentReads, "An originally admitted zero Count must authorize no Current reads.");
            RequireDisposed(responses);
        }

        private static FlippingKnownCountCollection<TenderComplianceResponse> HostileResponses(int admittedCount, int reboundCount)
        {
            return new FlippingKnownCountCollection<TenderComplianceResponse>(
                new[]
                {
                    new TenderComplianceResponse("BID-A", "INSURANCE", true, "verified"),
                    new TenderComplianceResponse("BID-B", "INSURANCE", true, "verified")
                },
                admittedCount,
                reboundCount);
        }

        private static void RequireDisposed<T>(FlippingKnownCountCollection<T> source)
        {
            Require(
                source.EnumeratorCreations == 0 || source.DisposeCalls == source.EnumeratorCreations,
                "Any commercial snapshot enumerator acquired before a Count-drift failure must be disposed.");
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

        private sealed class FlippingKnownCountCollection<T> : IReadOnlyCollection<T>
        {
            private readonly IReadOnlyList<T> _items;
            private readonly int _admittedCount;
            private readonly int _reboundCount;
            private int _countReads;

            internal FlippingKnownCountCollection(IReadOnlyList<T> items, int admittedCount, int reboundCount)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
                if (_items.Count < 2)
                    throw new ArgumentException("At least two items are required for the hostile Count fixture.", nameof(items));
                _admittedCount = admittedCount;
                _reboundCount = reboundCount;
            }

            public int Count
            {
                get
                {
                    _countReads++;
                    return _countReads == 1 ? _admittedCount : _reboundCount;
                }
            }

            internal int CurrentReads { get; private set; }
            internal int EnumeratorCreations { get; private set; }
            internal int DisposeCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumeratorCreations++;
                return new TrackingEnumerator(this, _items);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TrackingEnumerator : IEnumerator<T>
            {
                private readonly FlippingKnownCountCollection<T> _owner;
                private readonly IReadOnlyList<T> _items;
                private int _index = -1;

                internal TrackingEnumerator(FlippingKnownCountCollection<T> owner, IReadOnlyList<T> items)
                {
                    _owner = owner;
                    _items = items;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _index++;
                    return _index < _items.Count;
                }

                public void Reset() => throw new NotSupportedException();

                public void Dispose()
                {
                    _owner.DisposeCalls++;
                }
            }
        }
    }
}
