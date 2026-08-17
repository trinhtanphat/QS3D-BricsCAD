using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class AdvancedCostKnownCountInspectionSmoke
    {
        private const int MaximumEntries = 10000;

        internal static void Run()
        {
            InBoundConflictReadsEveryKnownCountBeforeRejecting();
            HiddenOversizeReadsEveryKnownCountAndPreservesCapacityFailure();
        }

        private static void InBoundConflictReadsEveryKnownCountBeforeRejecting()
        {
            var source = new InstrumentedMultiCount<CostResourceComponent>(1, 2, 3);
            var error = Capture<InvalidOperationException>(() => BuildUp("BUILDUP-COUNT-CONFLICT-INSPECTION", source));

            Equal(1, source.GenericCountReads, "Generic ICollection<T>.Count must be inspected exactly once.");
            Equal(1, source.ReadOnlyCountReads, "IReadOnlyCollection<T>.Count must be inspected exactly once.");
            Equal(1, source.NonGenericCountReads, "Non-generic ICollection.Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Conflicting known Count contracts must fail before enumeration.");
            Contains("conflicting known counts", error.Message, "In-bound Count disagreement must fail closed explicitly.");
        }

        private static void HiddenOversizeReadsEveryKnownCountAndPreservesCapacityFailure()
        {
            var source = new InstrumentedMultiCount<CostResourceComponent>(1, 2, MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() => BuildUp("BUILDUP-HIDDEN-OVERSIZE-INSPECTION", source));

            Equal(1, source.GenericCountReads, "Generic ICollection<T>.Count must still be inspected for hidden oversize input.");
            Equal(1, source.ReadOnlyCountReads, "IReadOnlyCollection<T>.Count must still be inspected for hidden oversize input.");
            Equal(1, source.NonGenericCountReads, "Non-generic ICollection.Count must expose the hidden oversize value.");
            Equal(0, source.GetEnumeratorCalls, "Hidden oversized Count contracts must fail before enumeration.");
            Contains("at most 10000", error.Message, "Any known Count above the bound must preserve the capacity diagnostic.");
        }

        private static CostRateBuildUp BuildUp(string id, IEnumerable<CostResourceComponent> components)
        {
            return new CostRateBuildUp(id, new CostCode("CONC"), "m3", "VND", components);
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

        private sealed class InstrumentedMultiCount<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal InstrumentedMultiCount(int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            int ICollection<T>.Count
            {
                get
                {
                    GenericCountReads++;
                    return _genericCount;
                }
            }

            int IReadOnlyCollection<T>.Count
            {
                get
                {
                    ReadOnlyCountReads++;
                    return _readOnlyCount;
                }
            }

            int ICollection.Count
            {
                get
                {
                    NonGenericCountReads++;
                    return _nonGenericCount;
                }
            }

            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            internal int GenericCountReads { get; private set; }
            internal int ReadOnlyCountReads { get; private set; }
            internal int NonGenericCountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Known-count contract inspection must reject before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => false;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }

    internal static class AdvancedCostKnownCountInspectionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AdvancedCostKnownCountInspectionSmoke.Run();
        }
    }
}
