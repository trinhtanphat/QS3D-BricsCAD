using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class AdvancedCostCurrentCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RateBuildUpRejectsCurrentInducedDriftBeforeNullValidation();
            HistoricalCatalogRejectsCurrentInducedDriftBeforeNullValidation();
            TenderQuoteLinesRejectCurrentInducedDriftBeforeNullValidation();
            TenderRequirementsRejectCurrentInducedDriftBeforeNullValidation();
            TenderBidsRejectCurrentInducedDriftBeforeNullValidation();
            ProgressContractsRejectCurrentInducedDriftBeforeNullValidation();
            ProgressClaimsRejectCurrentInducedDriftBeforeNullValidation();
            StableCountedControlsRemainAccepted();
            StreamingControlsRemainAccepted();
            Console.WriteLine("PASS advanced cost Current-induced Count stability");
        }

        private static void RateBuildUpRejectsCurrentInducedDriftBeforeNullValidation()
        {
            var source = new CurrentDriftCollection<CostResourceComponent>(null!);
            ExpectCountDrift(
                () => new CostRateBuildUp("BU-CURRENT", new CostCode("COST-CURRENT"), "ea", "USD", source),
                source,
                "rate build-up component Current-induced Count drift");
        }

        private static void HistoricalCatalogRejectsCurrentInducedDriftBeforeNullValidation()
        {
            var source = new CurrentDriftCollection<HistoricalCostRecord>(null!);
            ExpectCountDrift(
                () => new HistoricalCostCatalog(source),
                source,
                "historical catalog Current-induced Count drift");
        }

        private static void TenderQuoteLinesRejectCurrentInducedDriftBeforeNullValidation()
        {
            var source = new CurrentDriftCollection<TenderQuoteLine>(null!);
            ExpectCountDrift(
                () => new TenderBid("BID-CURRENT", "Bidder", "USD", source),
                source,
                "tender quote line Current-induced Count drift");
        }

        private static void TenderRequirementsRejectCurrentInducedDriftBeforeNullValidation()
        {
            var source = new CurrentDriftCollection<TenderRequirement>(null!);
            ExpectCountDrift(
                () => new TenderEvaluationService().Evaluate(source, Array.Empty<TenderBid>()),
                source,
                "tender requirement Current-induced Count drift");
        }

        private static void TenderBidsRejectCurrentInducedDriftBeforeNullValidation()
        {
            var source = new CurrentDriftCollection<TenderBid>(null!);
            ExpectCountDrift(
                () => new TenderEvaluationService().Evaluate(Array.Empty<TenderRequirement>(), source),
                source,
                "tender bid Current-induced Count drift");
        }

        private static void ProgressContractsRejectCurrentInducedDriftBeforeNullValidation()
        {
            var source = new CurrentDriftCollection<ProgressContractItem>(null!);
            ExpectCountDrift(
                () => new ProgressClaimService().Evaluate(source, Array.Empty<ProgressClaimLine>()),
                source,
                "progress contract Current-induced Count drift");
        }

        private static void ProgressClaimsRejectCurrentInducedDriftBeforeNullValidation()
        {
            var source = new CurrentDriftCollection<ProgressClaimLine>(null!);
            var contracts = new[] { new ProgressContractItem("ITEM-CURRENT", "ea", 1m, 1m) };
            ExpectCountDrift(
                () => new ProgressClaimService().Evaluate(contracts, source),
                source,
                "progress claim Current-induced Count drift");
        }

        private static void StableCountedControlsRemainAccepted()
        {
            var component = new CostResourceComponent("RES-STABLE", "Resource", "ea", 1m, 2m);
            var buildUp = new CostRateBuildUp(
                "BU-STABLE",
                new CostCode("COST-STABLE"),
                "ea",
                "USD",
                new[] { component });
            Require(buildUp.Components.Count == 1, "stable rate build-up counted control changed");

            var historical = new HistoricalCostCatalog(new[]
            {
                new HistoricalCostRecord(
                    "REC-STABLE",
                    "BENCH-STABLE",
                    "DIM-STABLE",
                    1m,
                    2m,
                    "USD",
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            });
            Require(historical.Records.Count == 1, "stable historical counted control changed");

            var bid = new TenderBid(
                "BID-STABLE",
                "Bidder",
                "USD",
                new[] { new TenderQuoteLine("ITEM-STABLE", 2m) });
            var tender = new TenderEvaluationService().Evaluate(
                new[] { new TenderRequirement("ITEM-STABLE", "Item", "ea", 1m) },
                new[] { bid });
            Require(tender.Count == 1 && tender[0].EvaluatedTotal == 2m,
                "stable tender counted control changed");

            var progress = new ProgressClaimService().Evaluate(
                new[] { new ProgressContractItem("ITEM-STABLE", "ea", 1m, 2m) },
                new[] { new ProgressClaimLine("ITEM-STABLE", 0m, 1m) });
            Require(progress.GrossCertifiedThisPeriod == 2m,
                "stable progress counted control changed");
        }

        private static void StreamingControlsRemainAccepted()
        {
            var bid = new TenderBid(
                "BID-STREAM",
                "Bidder",
                "USD",
                YieldOne(new TenderQuoteLine("ITEM-STREAM", 3m)));
            var tender = new TenderEvaluationService().Evaluate(
                YieldOne(new TenderRequirement("ITEM-STREAM", "Item", "ea", 1m)),
                YieldOne(bid));
            Require(tender.Count == 1 && tender[0].EvaluatedTotal == 3m,
                "streaming tender control changed");

            var progress = new ProgressClaimService().Evaluate(
                YieldOne(new ProgressContractItem("ITEM-STREAM", "ea", 1m, 3m)),
                YieldOne(new ProgressClaimLine("ITEM-STREAM", 0m, 1m)));
            Require(progress.GrossCertifiedThisPeriod == 3m,
                "streaming progress control changed");
        }

        private static IEnumerable<T> YieldOne<T>(T item)
        {
            yield return item;
        }

        private static void ExpectCountDrift<T>(Action action, CurrentDriftCollection<T> source, string label)
        {
            try
            {
                action();
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                if (ex.Message.IndexOf("known count changed during traversal", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Require(source.CurrentReads == 1, label + " must read Current exactly once");
                    return;
                }
                throw new InvalidOperationException(
                    label + " was rejected after Current instead of at the Count boundary: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException(label + " was accepted unexpectedly.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class CurrentDriftCollection<T> : ICollection<T>
        {
            private readonly T _item;
            private bool _emitDrift;

            internal CurrentDriftCollection(T item) => _item = item;
            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    if (_emitDrift)
                    {
                        _emitDrift = false;
                        return 2;
                    }
                    return 1;
                }
            }

            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
            public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CurrentDriftCollection<T> _owner;
                private int _state;

                internal Enumerator(CurrentDriftCollection<T> owner) => _owner = owner;

                public bool MoveNext()
                {
                    if (_state != 0)
                    {
                        _state = 2;
                        return false;
                    }
                    _state = 1;
                    return true;
                }

                public T Current
                {
                    get
                    {
                        if (_state != 1) throw new InvalidOperationException();
                        _owner.CurrentReads++;
                        _owner._emitDrift = true;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
