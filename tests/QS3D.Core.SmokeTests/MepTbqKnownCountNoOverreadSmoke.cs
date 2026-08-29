using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepTbqKnownCountNoOverreadSmoke
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
            var group = Group();
            var source = new CountedGroups(1, group, group);
            var error = Throws<InvalidOperationException>(() => new MepTbqProjectionService().BuildReport(source));
            Contains(error.Message, "Count does not match");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentAccesses);
        }

        private static void StreamingCeilingRejectsBeforeExtraCurrent()
        {
            var source = new StreamingGroups(10001, Group());
            var error = Throws<InvalidOperationException>(() => new MepTbqProjectionService().BuildReport(source));
            Contains(error.Message, "at most 10000 quantity groups");
            Equal(10001, source.MoveNextCalls);
            Equal(10000, source.CurrentAccesses);
        }

        private static void UnderYieldStillFailsClosed()
        {
            var source = new CountedGroups(2, Group());
            var error = Throws<InvalidOperationException>(() => new MepTbqProjectionService().BuildReport(source));
            Contains(error.Message, "Count does not match");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentAccesses);
        }

        private static void StableCountedAndStreamingInputsRemainAccepted()
        {
            var group = Group();
            var counted = new CountedGroups(1, group);
            var countedResult = new MepTbqProjectionService().BuildReport(counted);
            Equal(1, countedResult.Count);
            Equal(1, counted.CurrentAccesses);

            var streamed = new MepTbqProjectionService().BuildReport(Stream(group));
            Equal(1, streamed.Count);
        }

        private static MepQuantityGroup Group() =>
            new MepQuantityService().Aggregate(new[]
            {
                new MepElement("E1", MepElementKind.Pipe, "CHW", "DN50", "L1", lengthM: 1d)
            })[0];

        private static IEnumerable<MepQuantityGroup> Stream(MepQuantityGroup group)
        {
            yield return group;
        }

        private sealed class CountedGroups : ICollection<MepQuantityGroup>, IReadOnlyCollection<MepQuantityGroup>, ICollection
        {
            private readonly IReadOnlyList<MepQuantityGroup> _items;
            private readonly int _count;

            internal CountedGroups(int count, params MepQuantityGroup[] items)
            {
                _count = count;
                _items = items;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentAccesses { get; private set; }
            int ICollection<MepQuantityGroup>.Count => _count;
            int IReadOnlyCollection<MepQuantityGroup>.Count => _count;
            int ICollection.Count => _count;
            bool ICollection<MepQuantityGroup>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<MepQuantityGroup> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<MepQuantityGroup>.Add(MepQuantityGroup item) => throw new NotSupportedException();
            void ICollection<MepQuantityGroup>.Clear() => throw new NotSupportedException();
            bool ICollection<MepQuantityGroup>.Contains(MepQuantityGroup item) => throw new NotSupportedException();
            void ICollection<MepQuantityGroup>.CopyTo(MepQuantityGroup[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<MepQuantityGroup>.Remove(MepQuantityGroup item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<MepQuantityGroup>
            {
                private readonly CountedGroups _owner;
                private int _index = -1;
                internal Enumerator(CountedGroups owner) => _owner = owner;
                public MepQuantityGroup Current
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
                    if (next >= _owner._items.Count) return false;
                    _index = next;
                    return true;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StreamingGroups : IEnumerable<MepQuantityGroup>
        {
            private readonly int _count;
            private readonly MepQuantityGroup _group;
            internal StreamingGroups(int count, MepQuantityGroup group) { _count = count; _group = group; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentAccesses { get; private set; }
            public IEnumerator<MepQuantityGroup> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<MepQuantityGroup>
            {
                private readonly StreamingGroups _owner;
                private int _index = -1;
                internal Enumerator(StreamingGroups owner) => _owner = owner;
                public MepQuantityGroup Current { get { _owner.CurrentAccesses++; return _owner._group; } }
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    var next = _index + 1;
                    if (next >= _owner._count) return false;
                    _index = next;
                    return true;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException error) { return error; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string actual, string expected)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected text containing '" + expected + "', got '" + (actual ?? string.Empty) + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class MepTbqKnownCountNoOverreadSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => MepTbqKnownCountNoOverreadSmoke.Run();
    }
}
