using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepQuantityKnownCountNoOverreadSmoke
    {
        internal static void Run()
        {
            KnownCountOverrunRejectsBeforeExtraCurrent();
            StreamingCeilingRejectsBeforeExtraCurrent();
            UnderYieldStillFailsClosed();
            StableCountedAndStreamingInputsRemainAccepted();
        }

        private static void KnownCountOverrunRejectsBeforeExtraCurrent()
        {
            var source = new CountedElements(
                advertisedCount: 1,
                Element("E1", 1d),
                Element("E2", 2d));

            var error = Throws<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));
            Contains(error.Message, "known count does not match");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentAccesses);
        }

        private static void StreamingCeilingRejectsBeforeExtraCurrent()
        {
            var source = new StreamingElements(10001);

            var error = Throws<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));
            Contains(error.Message, "at most 10000 elements");
            Equal(10001, source.MoveNextCalls);
            Equal(10000, source.CurrentAccesses);
        }

        private static void UnderYieldStillFailsClosed()
        {
            var source = new CountedElements(advertisedCount: 2, Element("E1", 1d));

            var error = Throws<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));
            Contains(error.Message, "known count does not match");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentAccesses);
        }

        private static void StableCountedAndStreamingInputsRemainAccepted()
        {
            var counted = new CountedElements(
                advertisedCount: 2,
                Element("E1", 1d),
                Element("E2", 2d));
            var countedResult = new MepQuantityService().Aggregate(counted);
            Equal(1, countedResult.Count);
            Equal(2, countedResult[0].ElementCount);
            Equal(3d, countedResult[0].LengthM);
            Equal(2, counted.CurrentAccesses);

            var streamed = new MepQuantityService().Aggregate(Stream(Element("S1", 4d)));
            Equal(1, streamed.Count);
            Equal(1, streamed[0].ElementCount);
            Equal(4d, streamed[0].LengthM);
        }

        private static MepElement Element(string id, double lengthM) =>
            new MepElement(id, MepElementKind.Pipe, "CHW", "DN50", "L1", lengthM: lengthM);

        private static IEnumerable<MepElement> Stream(MepElement element)
        {
            yield return element;
        }

        private sealed class CountedElements : ICollection<MepElement>, IReadOnlyCollection<MepElement>, ICollection
        {
            private readonly IReadOnlyList<MepElement> _items;
            private readonly int _advertisedCount;

            internal CountedElements(int advertisedCount, params MepElement[] items)
            {
                _advertisedCount = advertisedCount;
                _items = items;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentAccesses { get; private set; }

            int ICollection<MepElement>.Count => _advertisedCount;
            int IReadOnlyCollection<MepElement>.Count => _advertisedCount;
            int ICollection.Count => _advertisedCount;
            bool ICollection<MepElement>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<MepElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<MepElement>.Add(MepElement item) => throw new NotSupportedException();
            void ICollection<MepElement>.Clear() => throw new NotSupportedException();
            bool ICollection<MepElement>.Contains(MepElement item) => throw new NotSupportedException();
            void ICollection<MepElement>.CopyTo(MepElement[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<MepElement>.Remove(MepElement item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<MepElement>
            {
                private readonly CountedElements _owner;
                private int _index = -1;

                internal Enumerator(CountedElements owner) => _owner = owner;

                public MepElement Current
                {
                    get
                    {
                        _owner.CurrentAccesses++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    var next = _index + 1;
                    if (next >= _owner._items.Count)
                        return false;
                    _index = next;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StreamingElements : IEnumerable<MepElement>
        {
            private readonly int _count;

            internal StreamingElements(int count) => _count = count;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentAccesses { get; private set; }

            public IEnumerator<MepElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<MepElement>
            {
                private readonly StreamingElements _owner;
                private int _index = -1;

                internal Enumerator(StreamingElements owner) => _owner = owner;

                public MepElement Current
                {
                    get
                    {
                        _owner.CurrentAccesses++;
                        var ordinal = _index + 1;
                        return Element("S" + ordinal, 1d);
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    var next = _index + 1;
                    if (next >= _owner._count)
                        return false;
                    _index = next;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string actual, string expected)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected text containing '" + expected + "', got '" + (actual ?? string.Empty) + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class MepQuantityKnownCountNoOverreadSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MepQuantityKnownCountNoOverreadSmoke.Run();
    }
}
