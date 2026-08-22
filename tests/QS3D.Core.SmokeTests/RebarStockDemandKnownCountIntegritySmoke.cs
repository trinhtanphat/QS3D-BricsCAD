using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockDemandKnownCountIntegritySmoke
    {
        private const int MaxCutRequirements = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectNegativeKnownCountBeforeEnumeration();
            RejectConflictingKnownCountsBeforeEnumeration();
            RejectOversizedKnownCountBeforeEnumeration();
            RejectDishonestPositiveCountWithEmptyEnumeration();
            PreserveStreamingRequirementBoundaryForUnderreportedCount();
            PreserveOrdinaryListBehavior();
        }

        private static void RejectNegativeKnownCountBeforeEnumeration()
        {
            var source = new MultiCountList(Array.Empty<RebarCutRequirement>(), 0, -1, 0, throwOnEnumeration: true);
            ExpectInvalidOperation(
                () => CreateDemand(source),
                "invalid negative known Count",
                "Negative stock-demand Count must fail closed before enumeration.");
            AssertNotEnumerated(source, "negative known Count");
        }

        private static void RejectConflictingKnownCountsBeforeEnumeration()
        {
            var source = new MultiCountList(Array.Empty<RebarCutRequirement>(), 1, 2, 1, throwOnEnumeration: true);
            ExpectInvalidOperation(
                () => CreateDemand(source),
                "conflicting known Count values",
                "Conflicting stock-demand Count contracts must fail closed before enumeration.");
            AssertNotEnumerated(source, "conflicting known Counts");
        }

        private static void RejectOversizedKnownCountBeforeEnumeration()
        {
            var source = new MultiCountList(Array.Empty<RebarCutRequirement>(), 1, 1, MaxCutRequirements + 1, throwOnEnumeration: true);
            ExpectArgumentOutOfRange(
                () => CreateDemand(source),
                "supported cut-requirement bound",
                "Oversized stock-demand Count must fail before enumeration.");
            AssertNotEnumerated(source, "oversized known Count");
        }

        private static void RejectDishonestPositiveCountWithEmptyEnumeration()
        {
            var source = new MultiCountList(Array.Empty<RebarCutRequirement>(), 1, 1, 1, throwOnEnumeration: false);
            ExpectArgument(
                () => CreateDemand(source),
                "At least one required cut",
                "A positive reported Count with an empty enumeration must not create an empty stock demand.");
            if (!source.EnumeratorRequested)
                throw new InvalidOperationException("Dishonest positive Count must be checked against the actual enumeration.");
        }

        private static void PreserveStreamingRequirementBoundaryForUnderreportedCount()
        {
            var source = new DishonestReadOnlyList(MaxCutRequirements + 1, reportedCount: 1);
            ExpectArgumentOutOfRange(
                () => CreateDemand(source),
                "supported cut-requirement bound",
                "Under-reported stock-demand Count must still stop at the streaming requirement boundary.");
            if (source.MoveNextCalls != MaxCutRequirements + 1)
                throw new InvalidOperationException(
                    "Stock-demand streaming guard must stop after observing requirement 10,001 without requesting another item. MoveNext calls: " + source.MoveNextCalls + ".");
        }

        private static void PreserveOrdinaryListBehavior()
        {
            var cut = new RebarCutRequirement("CUT-A", 1.5d, 2);
            var demand = new RebarStockDemand(
                "GROUP-A",
                "CB400-V",
                16d,
                11.7d,
                new[] { cut },
                new RebarCutAllowancePolicy(allowancePerRequiredCutM: 0.1d));

            if (demand.RequiredCuts.Count != 1 || !ReferenceEquals(cut, demand.RequiredCuts[0]))
                throw new InvalidOperationException("Ordinary stock-demand required cuts changed unexpectedly.");
            if (demand.RequiredCutCount != 2)
                throw new InvalidOperationException("Ordinary stock-demand cut quantity changed unexpectedly.");
            Near(3d, demand.RequiredCutLengthM, "required cut length");
            Near(0.2d, demand.AllowanceLengthM, "allowance length");
            Near(3.2d, demand.DemandLengthBeforeKerfM, "demand length before kerf");
        }

        private static RebarStockDemand CreateDemand(IReadOnlyList<RebarCutRequirement> cuts)
        {
            return new RebarStockDemand(
                "GROUP-COUNT",
                "CB400-V",
                16d,
                11.7d,
                cuts,
                new RebarCutAllowancePolicy());
        }

        private static void AssertNotEnumerated(MultiCountList source, string label)
        {
            if (source.EnumeratorRequested)
                throw new InvalidOperationException("RebarStockDemand enumerated input after " + label + " was already invalid from Count contracts.");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new InvalidOperationException("Unexpected " + label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment, string failureMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(failureMessage + " Actual diagnostic: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException(failureMessage);
        }

        private static void ExpectArgumentOutOfRange(Action action, string expectedMessageFragment, string failureMessage)
        {
            try
            {
                action();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(failureMessage + " Actual diagnostic: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException(failureMessage);
        }

        private static void ExpectArgument(Action action, string expectedMessageFragment, string failureMessage)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(failureMessage + " Actual diagnostic: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException(failureMessage);
        }

        private sealed class MultiCountList : IReadOnlyList<RebarCutRequirement>, ICollection<RebarCutRequirement>, ICollection
        {
            private readonly RebarCutRequirement[] _items;
            private readonly int _readOnlyCount;
            private readonly int _genericCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            internal MultiCountList(
                RebarCutRequirement[] items,
                int readOnlyCount,
                int genericCount,
                int nonGenericCount,
                bool throwOnEnumeration)
            {
                _items = items;
                _readOnlyCount = readOnlyCount;
                _genericCount = genericCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            internal bool EnumeratorRequested { get; private set; }
            int IReadOnlyCollection<RebarCutRequirement>.Count => _readOnlyCount;
            int ICollection<RebarCutRequirement>.Count => _genericCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<RebarCutRequirement>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            public RebarCutRequirement this[int index] => _items[index];

            public IEnumerator<RebarCutRequirement> GetEnumerator()
            {
                EnumeratorRequested = true;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Malformed stock-demand Count contracts must fail before enumeration.");
                return ((IEnumerable<RebarCutRequirement>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<RebarCutRequirement>.Add(RebarCutRequirement item) => throw new NotSupportedException();
            void ICollection<RebarCutRequirement>.Clear() => throw new NotSupportedException();
            bool ICollection<RebarCutRequirement>.Contains(RebarCutRequirement item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<RebarCutRequirement>.CopyTo(RebarCutRequirement[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<RebarCutRequirement>.Remove(RebarCutRequirement item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }

        private sealed class DishonestReadOnlyList : IReadOnlyList<RebarCutRequirement>
        {
            private readonly int _actualCount;
            private readonly int _reportedCount;

            internal DishonestReadOnlyList(int actualCount, int reportedCount)
            {
                _actualCount = actualCount;
                _reportedCount = reportedCount;
            }

            public int Count => _reportedCount;
            public RebarCutRequirement this[int index] => Requirement(index);
            internal int MoveNextCalls { get; private set; }
            public IEnumerator<RebarCutRequirement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private static RebarCutRequirement Requirement(int index)
            {
                return new RebarCutRequirement("STREAM-CUT-" + index, 1d, 1);
            }

            private sealed class Enumerator : IEnumerator<RebarCutRequirement>
            {
                private readonly DishonestReadOnlyList _owner;
                private int _index = -1;

                internal Enumerator(DishonestReadOnlyList owner) { _owner = owner; }
                public RebarCutRequirement Current { get; private set; } = null!;
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index >= _owner._actualCount) return false;
                    Current = Requirement(_index);
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
