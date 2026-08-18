using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class AdvancedCostKnownCountTraversalSmoke
    {
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            BuildUpRejectsKnownCountTraversalMismatch();
            HistoricalCatalogRejectsKnownCountTraversalMismatch();
            TenderBidRejectsKnownCountTraversalMismatch();
            TenderEvaluationRejectsRequirementAndBidCountMismatch();
            ProgressEvaluationRejectsContractAndClaimCountMismatch();
            ExactKnownCountAndPureStreamingRemainAccepted();
            SemanticValidationStillPrecedesPostTraversalMismatch();
        }

        private static void BuildUpRejectsKnownCountTraversalMismatch()
        {
            AssertCountMismatch(
                () => BuildUp(new MisreportedReadOnlyCollection<CostResourceComponent>(2, Component(0))),
                "Build-up under-yield must reject a trusted known Count that exceeds traversal.");
            AssertCountMismatch(
                () => BuildUp(new MisreportedReadOnlyCollection<CostResourceComponent>(1, Component(0), Component(1))),
                "Build-up over-yield must reject traversal that exceeds a trusted known Count.");
        }

        private static void HistoricalCatalogRejectsKnownCountTraversalMismatch()
        {
            AssertCountMismatch(
                () => new HistoricalCostCatalog(new MisreportedReadOnlyCollection<HistoricalCostRecord>(2, Historical(0))),
                "Historical under-yield must reject a Count/traversal mismatch.");
            AssertCountMismatch(
                () => new HistoricalCostCatalog(new MisreportedReadOnlyCollection<HistoricalCostRecord>(1, Historical(0), Historical(1))),
                "Historical over-yield must reject a Count/traversal mismatch.");
        }

        private static void TenderBidRejectsKnownCountTraversalMismatch()
        {
            AssertCountMismatch(
                () => Bid("BID-UNDER", new MisreportedReadOnlyCollection<TenderQuoteLine>(2, Quote(0))),
                "Tender quote under-yield must reject a Count/traversal mismatch.");
            AssertCountMismatch(
                () => Bid("BID-OVER", new MisreportedReadOnlyCollection<TenderQuoteLine>(1, Quote(0), Quote(1))),
                "Tender quote over-yield must reject a Count/traversal mismatch.");
        }

        private static void TenderEvaluationRejectsRequirementAndBidCountMismatch()
        {
            var bid = Bid("BID-VALID", new[] { Quote(0), Quote(1) });
            var service = new TenderEvaluationService();

            AssertCountMismatch(
                () => service.Evaluate(
                    new MisreportedReadOnlyCollection<TenderRequirement>(2, Requirement(0)),
                    new[] { bid }),
                "Tender requirement under-yield must reject a Count/traversal mismatch.");
            AssertCountMismatch(
                () => service.Evaluate(
                    new MisreportedReadOnlyCollection<TenderRequirement>(1, Requirement(0), Requirement(1)),
                    new[] { bid }),
                "Tender requirement over-yield must reject a Count/traversal mismatch.");
            AssertCountMismatch(
                () => service.Evaluate(
                    new[] { Requirement(0), Requirement(1) },
                    new MisreportedReadOnlyCollection<TenderBid>(2, bid)),
                "Tender bid under-yield must reject a Count/traversal mismatch.");
            AssertCountMismatch(
                () => service.Evaluate(
                    new[] { Requirement(0), Requirement(1) },
                    new MisreportedReadOnlyCollection<TenderBid>(1, bid, Bid("BID-SECOND", new[] { Quote(0), Quote(1) }))),
                "Tender bid over-yield must reject a Count/traversal mismatch.");
        }

        private static void ProgressEvaluationRejectsContractAndClaimCountMismatch()
        {
            var service = new ProgressClaimService();

            AssertCountMismatch(
                () => service.Evaluate(
                    new MisreportedReadOnlyCollection<ProgressContractItem>(2, Contract(0)),
                    Array.Empty<ProgressClaimLine>()),
                "Progress contract under-yield must reject a Count/traversal mismatch.");
            AssertCountMismatch(
                () => service.Evaluate(
                    new MisreportedReadOnlyCollection<ProgressContractItem>(1, Contract(0), Contract(1)),
                    Array.Empty<ProgressClaimLine>()),
                "Progress contract over-yield must reject a Count/traversal mismatch.");
            AssertCountMismatch(
                () => service.Evaluate(
                    new[] { Contract(0), Contract(1) },
                    new MisreportedReadOnlyCollection<ProgressClaimLine>(2, Claim(0))),
                "Progress claim under-yield must reject a Count/traversal mismatch.");
            AssertCountMismatch(
                () => service.Evaluate(
                    new[] { Contract(0), Contract(1) },
                    new MisreportedReadOnlyCollection<ProgressClaimLine>(1, Claim(0), Claim(1))),
                "Progress claim over-yield must reject a Count/traversal mismatch.");
        }

        private static void ExactKnownCountAndPureStreamingRemainAccepted()
        {
            var counted = BuildUp(new MisreportedReadOnlyCollection<CostResourceComponent>(2, Component(0), Component(1)));
            Equal(2, counted.Components.Count, "Exact known Count/traversal agreement must remain accepted.");

            var streaming = BuildUp(Stream(Component(0), Component(1)));
            Equal(2, streaming.Components.Count, "Pure streaming AdvancedCost sources without a known Count must remain accepted.");

            var progress = new ProgressClaimService().Evaluate(
                new MisreportedReadOnlyCollection<ProgressContractItem>(2, Contract(0), Contract(1)),
                new MisreportedReadOnlyCollection<ProgressClaimLine>(2, Claim(0), Claim(1)));
            Equal(2, progress.Lines.Count, "Exact counted progress inputs must retain ordinary evaluation behavior.");
        }

        private static void SemanticValidationStillPrecedesPostTraversalMismatch()
        {
            var duplicate = Component(0);
            var error = Capture<ArgumentException>(() =>
                BuildUp(new MisreportedReadOnlyCollection<CostResourceComponent>(3, duplicate, duplicate)));
            Contains("Duplicate rate build-up resource code", error.Message,
                "Existing semantic validation during traversal must fail before the post-traversal Count check.");
        }

        private static void AssertCountMismatch(Action action, string message)
        {
            var error = Capture<InvalidOperationException>(action);
            Contains("known count reported", error.Message, message);
        }

        private static CostRateBuildUp BuildUp(IEnumerable<CostResourceComponent> components)
        {
            return new CostRateBuildUp("BUILDUP-TRAVERSAL", new CostCode("CONC"), "m3", "VND", components);
        }

        private static TenderBid Bid(string id, IEnumerable<TenderQuoteLine> lines)
        {
            return new TenderBid(id, "Bidder " + id, "VND", lines);
        }

        private static CostResourceComponent Component(int index)
        {
            return new CostResourceComponent("RES-" + index, "Resource " + index, "kg", 1m, 1m);
        }

        private static HistoricalCostRecord Historical(int index)
        {
            return new HistoricalCostRecord(
                "HIST-" + index,
                "BUILDING",
                "OFFICE",
                1m,
                index + 1m,
                "VND",
                StartUtc.AddTicks(index));
        }

        private static TenderRequirement Requirement(int index)
        {
            return new TenderRequirement("ITEM-" + index, "Item " + index, "m2", 1m);
        }

        private static TenderQuoteLine Quote(int index)
        {
            return new TenderQuoteLine("ITEM-" + index, index + 1m);
        }

        private static ProgressContractItem Contract(int index)
        {
            return new ProgressContractItem("ITEM-" + index, "m2", 2m, 1m);
        }

        private static ProgressClaimLine Claim(int index)
        {
            return new ProgressClaimLine("ITEM-" + index, 0m, 1m);
        }

        private static IEnumerable<T> Stream<T>(params T[] items)
        {
            for (var i = 0; i < items.Length; i++)
                yield return items[i];
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class MisreportedReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;

            internal MisreportedReadOnlyCollection(int advertisedCount, params T[] items)
            {
                Count = advertisedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count { get; }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                    yield return _items[i];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class AdvancedCostKnownCountTraversalRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AdvancedCostKnownCountTraversalSmoke.Run();
        }
    }
}
