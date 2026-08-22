using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundaryDiagnosticKnownCountSmoke
    {
        internal static void Run()
        {
            InvalidKnownCountsFailBeforeEnumeration();
            CountTraversalMismatchFailsClosed();
            MatchingCountAndStreamingInputsRemainAccepted();
        }

        private static void InvalidKnownCountsFailBeforeEnumeration()
        {
            var negative = new GuardedReadOnlyCollection(-1);
            var negativeError = Capture<InvalidOperationException>(() => Analyze(negative));
            Contains("negative known count", negativeError.Message,
                "Negative known Count must fail at diagnostic preflight.");
            Equal(0, negative.EnumerationCount,
                "Negative known Count must fail before GetEnumerator().");

            var oversized = new GuardedReadOnlyCollection(5001);
            var oversizedError = Capture<InvalidOperationException>(() => Analyze(oversized));
            Contains("supported segment limit", oversizedError.Message,
                "Oversized known Count must retain the room-boundary capacity diagnostic.");
            Equal(0, oversized.EnumerationCount,
                "Oversized known Count must fail before GetEnumerator().");

            var nonGenericNegative = new GuardedNonGenericCollection(-1);
            var nonGenericError = Capture<InvalidOperationException>(() => Analyze(nonGenericNegative));
            Contains("negative known count", nonGenericError.Message,
                "Negative non-generic Count must fail at diagnostic preflight.");
            Equal(0, nonGenericNegative.EnumerationCount,
                "Negative non-generic Count must fail before GetEnumerator().");

            var conflicting = new ConflictingCountCollection(1, 2);
            var conflictingError = Capture<InvalidOperationException>(() => Analyze(conflicting));
            Contains("conflicting known counts", conflictingError.Message,
                "Conflicting supported Count contracts must fail closed.");
            Equal(0, conflicting.EnumerationCount,
                "Conflicting Count contracts must fail before GetEnumerator().");
        }

        private static void CountTraversalMismatchFailsClosed()
        {
            var under = new MisreportedReadOnlyCollection<BoundarySegment>(1);
            var underError = Capture<InvalidOperationException>(() => Analyze(under));
            Contains("known count does not match traversal", underError.Message,
                "Advertised Count greater than traversal must fail closed.");

            var over = new MisreportedReadOnlyCollection<BoundarySegment>(0, Segment("OVER"));
            var overError = Capture<InvalidOperationException>(() => Analyze(over));
            Contains("known count does not match traversal", overError.Message,
                "Traversal greater than advertised Count must fail closed.");
        }

        private static void MatchingCountAndStreamingInputsRemainAccepted()
        {
            var counted = Analyze(new MisreportedReadOnlyCollection<BoundarySegment>(0));
            Equal(RoomBoundaryDiagnosticReason.NoInput, counted.Report.Reason,
                "Matching empty counted input must retain NoInput behavior.");

            var streaming = Analyze(Stream(Segment("STREAM")));
            Equal(RoomBoundaryDiagnosticReason.InsufficientSegments, streaming.Report.Reason,
                "Pure streaming inputs without Count metadata must remain supported.");
            Equal(1, streaming.Report.InputSegmentCount,
                "Streaming diagnostic input count must remain based on traversal.");
        }

        private static RoomBoundaryDiagnosticAnalysis Analyze(IEnumerable<BoundarySegment> source)
        {
            return new RoomBoundaryDiagnosticService().Analyze(source);
        }

        private static BoundarySegment Segment(string id)
        {
            return new BoundarySegment(new Point2(0d, 0d), new Point2(1d, 0d), id);
        }

        private static IEnumerable<BoundarySegment> Stream(params BoundarySegment[] items)
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

        private sealed class GuardedReadOnlyCollection : IReadOnlyCollection<BoundarySegment>
        {
            internal GuardedReadOnlyCollection(int count)
            {
                Count = count;
            }

            public int Count { get; }
            public int EnumerationCount { get; private set; }

            public IEnumerator<BoundarySegment> GetEnumerator()
            {
                EnumerationCount++;
                throw new InvalidOperationException("Guarded collection must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class GuardedNonGenericCollection : IEnumerable<BoundarySegment>, ICollection
        {
            internal GuardedNonGenericCollection(int count)
            {
                Count = count;
            }

            public int Count { get; }
            public int EnumerationCount { get; private set; }
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public void CopyTo(Array array, int index)
            {
                throw new NotSupportedException();
            }

            public IEnumerator<BoundarySegment> GetEnumerator()
            {
                EnumerationCount++;
                throw new InvalidOperationException("Guarded collection must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ConflictingCountCollection : ICollection<BoundarySegment>, IReadOnlyCollection<BoundarySegment>
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;

            internal ConflictingCountCollection(int genericCount, int readOnlyCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
            }

            int ICollection<BoundarySegment>.Count => _genericCount;
            int IReadOnlyCollection<BoundarySegment>.Count => _readOnlyCount;
            bool ICollection<BoundarySegment>.IsReadOnly => true;
            public int EnumerationCount { get; private set; }

            void ICollection<BoundarySegment>.Add(BoundarySegment item) => throw new NotSupportedException();
            void ICollection<BoundarySegment>.Clear() => throw new NotSupportedException();
            bool ICollection<BoundarySegment>.Contains(BoundarySegment item) => false;
            void ICollection<BoundarySegment>.CopyTo(BoundarySegment[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<BoundarySegment>.Remove(BoundarySegment item) => throw new NotSupportedException();

            public IEnumerator<BoundarySegment> GetEnumerator()
            {
                EnumerationCount++;
                throw new InvalidOperationException("Conflicting collection must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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

    internal static class RoomBoundaryDiagnosticKnownCountRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RoomBoundaryDiagnosticKnownCountSmoke.Run();
        }
    }
}
