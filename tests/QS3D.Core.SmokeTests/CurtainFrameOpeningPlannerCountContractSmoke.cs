using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainFrameOpeningPlannerCountContractSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownOversizeCollectionsFailBeforeEnumeration();
            InvalidAndConflictingKnownCountsFailClosed();
            AdvertisedCountDriftFailsAfterBoundedTraversal();
        }

        private static void KnownOversizeCollectionsFailBeforeEnumeration()
        {
            var frames = Counted(
                genericCount: 20001,
                readOnlyCount: 20001,
                nonGenericCount: 20001,
                values: new[] { new CurtainWallRect(0d, 0d, 1d, 1d) });
            Expect<InvalidOperationException>(
                () => CurtainFrameOpeningPlanner.Interrupt(frames, Array.Empty<CurtainOpeningRect>()),
                "known oversized frame collection");
            if (frames.Enumerated != 0)
                throw new InvalidOperationException("Known oversized frame collection must be rejected before enumeration.");

            var openings = Counted(
                genericCount: 4097,
                readOnlyCount: 4097,
                nonGenericCount: 4097,
                values: new[] { new CurtainOpeningRect(100d, 100d, 1d, 1d) });
            Expect<InvalidOperationException>(
                () => CurtainFrameOpeningPlanner.Interrupt(Array.Empty<CurtainWallRect>(), openings),
                "known oversized opening collection");
            if (openings.Enumerated != 0)
                throw new InvalidOperationException("Known oversized opening collection must be rejected before enumeration.");
        }

        private static void InvalidAndConflictingKnownCountsFailClosed()
        {
            var negative = Counted<CurtainWallRect>(-1, -1, -1, Array.Empty<CurtainWallRect>());
            Expect<InvalidOperationException>(
                () => CurtainFrameOpeningPlanner.Interrupt(negative, Array.Empty<CurtainOpeningRect>()),
                "negative frame count");
            if (negative.Enumerated != 0)
                throw new InvalidOperationException("Negative frame count must fail before enumeration.");

            var conflicting = Counted(
                genericCount: 1,
                readOnlyCount: 2,
                nonGenericCount: 1,
                values: new[] { new CurtainOpeningRect(100d, 100d, 1d, 1d) });
            Expect<InvalidOperationException>(
                () => CurtainFrameOpeningPlanner.Interrupt(Array.Empty<CurtainWallRect>(), conflicting),
                "conflicting opening counts");
            if (conflicting.Enumerated != 0)
                throw new InvalidOperationException("Conflicting opening counts must fail before enumeration.");
        }

        private static void AdvertisedCountDriftFailsAfterBoundedTraversal()
        {
            var frame = new CurtainWallRect(0d, 0d, 1d, 1d);
            var frames = Counted(2, 2, 2, new[] { frame });
            Expect<InvalidOperationException>(
                () => CurtainFrameOpeningPlanner.Interrupt(frames, Array.Empty<CurtainOpeningRect>()),
                "frame advertised count drift");
            if (frames.Enumerated != 1)
                throw new InvalidOperationException("Frame count-drift control must traverse only the supplied item once.");

            var opening = new CurtainOpeningRect(100d, 100d, 1d, 1d);
            var openings = Counted(2, 2, 2, new[] { opening });
            Expect<InvalidOperationException>(
                () => CurtainFrameOpeningPlanner.Interrupt(Array.Empty<CurtainWallRect>(), openings),
                "opening advertised count drift");
            if (openings.Enumerated != 1)
                throw new InvalidOperationException("Opening count-drift control must traverse only the supplied item once.");
        }

        private static CountContractEnumerable<T> Counted<T>(
            int genericCount,
            int readOnlyCount,
            int nonGenericCount,
            IReadOnlyList<T> values)
            => new CountContractEnumerable<T>(genericCount, readOnlyCount, nonGenericCount, values);

        private static void Expect<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException(label + " must fail with " + typeof(TException).Name + ".");
        }

        private sealed class CountContractEnumerable<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly IReadOnlyList<T> _values;

            internal CountContractEnumerable(int genericCount, int readOnlyCount, int nonGenericCount, IReadOnlyList<T> values)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _values = values;
            }

            internal int Enumerated { get; private set; }
            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _values.Count; i++)
                {
                    Enumerated++;
                    yield return _values[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => throw new NotSupportedException();
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}
