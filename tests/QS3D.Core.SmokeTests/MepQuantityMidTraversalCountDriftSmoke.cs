using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepQuantityMidTraversalCountDriftSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CountDriftBeforeMoveNextFailsBeforeAdvancement();
            CountDriftAfterMoveNextFailsBeforeCurrent();
            TransientCountDriftCannotRestoreBeforePublication();
            CountDriftFromCurrentFailsBeforeNullAcceptance();
            StableCountedInputRemainsAccepted();
        }

        private static void CountDriftBeforeMoveNextFailsBeforeAdvancement()
        {
            var source = new CountDriftsOnSecondRead(Element("A"));
            var error = Capture<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));

            Equal(0, source.MoveNextCalls, "MEP Count drift before MoveNext must fail before caller-controlled advancement.");
            Equal(0, source.CurrentReads, "MEP Count drift before MoveNext must fail before Current.");
            Contains("known count changed during traversal", error.Message, "MEP pre-advance drift must use the Count-stability contract.");
        }

        private static void CountDriftAfterMoveNextFailsBeforeCurrent()
        {
            var source = new CountDriftsAfterMoveNext(Element("A"));
            var error = Capture<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));

            Equal(1, source.MoveNextCalls, "MEP post-advance drift control must advance exactly once.");
            Equal(0, source.CurrentReads, "MEP post-advance Count drift must fail before Current is observed.");
            Contains("known count changed during traversal", error.Message, "MEP post-advance drift must use the Count-stability contract.");
        }

        private static void TransientCountDriftCannotRestoreBeforePublication()
        {
            var source = new TransientCountDrift(Element("A"));
            var error = Capture<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));

            Equal(1, source.MoveNextCalls, "Transient Count drift must be detected at the advancement that caused it.");
            Equal(0, source.CurrentReads, "Transient Count drift must not consume the unstable element before failing.");
            Contains("known count changed during traversal", error.Message, "Transient Count drift must fail even when a later Count read could restore the admitted value.");
        }

        private static void CountDriftFromCurrentFailsBeforeNullAcceptance()
        {
            var source = new CountDriftsFromCurrent(Element("A"));
            var error = Capture<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));

            Equal(1, source.MoveNextCalls, "MEP Current-induced Count drift must advance only the admitted item.");
            Equal(1, source.CurrentReads, "MEP Current-induced Count drift must observe Current exactly once before failing.");
            Contains(
                "known count changed during traversal",
                error.Message,
                "MEP Current-induced Count drift must win over ordinary returned-item validation.");
        }

        private static void StableCountedInputRemainsAccepted()
        {
            var source = new StableCounted(Element("A"), Element("B"));
            var groups = new MepQuantityService().Aggregate(source);

            Equal(3, source.MoveNextCalls, "Stable counted MEP input must perform the terminal MoveNext exactly once.");
            Equal(2, source.CurrentReads, "Stable counted MEP input must consume each admitted Current exactly once.");
            Equal(1, groups.Count, "Stable counted MEP grouping changed unexpectedly.");
            Equal(2, groups[0].ElementCount, "Stable counted MEP element count changed unexpectedly.");
        }

        private static MepElement Element(string suffix) => new MepElement(
            "MID-" + suffix,
            MepElementKind.Pipe,
            "CHW",
            "DN100",
            "L1",
            count: 1,
            lengthM: 1d,
            areaM2: 0d,
            volumeM3: 0d);

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual=" + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private abstract class InstrumentedCounted : IReadOnlyCollection<MepElement>, IEnumerator<MepElement>
        {
            private readonly MepElement[] _items;
            private int _index = -1;

            protected InstrumentedCounted(params MepElement[] items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public abstract int Count { get; }
            public MepElement Current
            {
                get
                {
                    CurrentReads++;
                    return ReadCurrent(_items[_index]);
                }
            }
            object IEnumerator.Current => Current;
            protected int CountReads { get; set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            protected virtual MepElement ReadCurrent(MepElement item) => item;

            public IEnumerator<MepElement> GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool MoveNext()
            {
                MoveNextCalls++;
                if (_index + 1 >= _items.Length) return false;
                _index++;
                return true;
            }

            public void Reset() => throw new NotSupportedException();
            public void Dispose() { }
        }

        private sealed class CountDriftsOnSecondRead : InstrumentedCounted
        {
            internal CountDriftsOnSecondRead(params MepElement[] items) : base(items) { }
            public override int Count
            {
                get
                {
                    CountReads++;
                    return CountReads == 1 ? 1 : 2;
                }
            }
        }

        private sealed class CountDriftsAfterMoveNext : InstrumentedCounted
        {
            internal CountDriftsAfterMoveNext(params MepElement[] items) : base(items) { }
            public override int Count
            {
                get
                {
                    CountReads++;
                    return MoveNextCalls == 0 ? 1 : 2;
                }
            }
        }

        private sealed class TransientCountDrift : InstrumentedCounted
        {
            internal TransientCountDrift(params MepElement[] items) : base(items) { }
            public override int Count
            {
                get
                {
                    CountReads++;
                    return MoveNextCalls == 1 && CountReads == 3 ? 2 : 1;
                }
            }
        }

        private sealed class CountDriftsFromCurrent : InstrumentedCounted
        {
            private bool _currentObserved;

            internal CountDriftsFromCurrent(params MepElement[] items) : base(items) { }

            public override int Count
            {
                get
                {
                    CountReads++;
                    return _currentObserved ? 2 : 1;
                }
            }

            protected override MepElement ReadCurrent(MepElement item)
            {
                _currentObserved = true;
                return null!;
            }
        }

        private sealed class StableCounted : InstrumentedCounted
        {
            private readonly int _count;
            internal StableCounted(params MepElement[] items) : base(items) => _count = items.Length;
            public override int Count
            {
                get
                {
                    CountReads++;
                    return _count;
                }
            }
        }
    }
}
