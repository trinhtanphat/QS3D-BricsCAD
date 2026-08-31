using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SelectionStateFinalCountReentrancySmoke
    {
        internal static void Run()
        {
            FinalCountReentrancyCannotPublishStaleSelection();
            StableCountedReplacementKeepsObservationBudget();
        }

        private static void FinalCountReentrancyCannotPublishStaleSelection()
        {
            var state = new SelectionState();
            state.Replace(new[] { "KEEP" });
            var changed = 0;
            state.Changed += (_, _) => changed++;
            var source = new FinalCountReentrantCollection(state);

            ThrowsContaining<InvalidOperationException>(
                () => state.Replace(source),
                "Selection changed while replacement element ids were being enumerated");

            Equal(7, source.CountReads, "reentrant final Count reads");
            Equal(2, source.MoveNextCalls, "reentrant traversal MoveNext calls");
            Equal(1, source.CurrentReads, "reentrant traversal Current reads");
            Equal(1, changed, "only the nested replacement may publish");
            SequenceEqual(new[] { "INNER" }, state.ElementIds, "nested replacement must remain authoritative");
        }

        private static void StableCountedReplacementKeepsObservationBudget()
        {
            var state = new SelectionState();
            var source = new StableCountedCollection(" B ");
            var changed = 0;
            state.Changed += (_, _) => changed++;

            state.Replace(source);

            Equal(7, source.CountReads, "stable final Count reads");
            Equal(2, source.MoveNextCalls, "stable MoveNext calls");
            Equal(1, source.CurrentReads, "stable Current reads");
            Equal(1, changed, "stable Changed events");
            SequenceEqual(new[] { "B" }, state.ElementIds, "stable normalized publication");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception(label + ": expected '" + expected + "', got '" + actual + "'.");
        }

        private static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual, string label)
        {
            if (!expected.SequenceEqual(actual, StringComparer.OrdinalIgnoreCase))
                throw new Exception(label + ": expected [" + string.Join(", ", expected) + "] but got [" + string.Join(", ", actual) + "].");
        }

        private static void ThrowsContaining<T>(Action action, string expectedText) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("Expected exception message containing '" + expectedText + "', got '" + ex.Message + "'.");
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private abstract class SingleItemCountedCollection : ICollection<string>
        {
            private readonly string _value;
            protected SingleItemCountedCollection(string value) => _value = value;

            public int CountReads { get; protected set; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public abstract int Count { get; }
            public bool IsReadOnly => true;

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(string item) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly SingleItemCountedCollection _owner;
                private int _index = -1;

                internal Enumerator(SingleItemCountedCollection owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index == 0;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class FinalCountReentrantCollection : SingleItemCountedCollection
        {
            private readonly SelectionState _state;
            private bool _reentered;

            internal FinalCountReentrantCollection(SelectionState state) : base("OUTER") => _state = state;

            public override int Count
            {
                get
                {
                    CountReads++;
                    if (!_reentered && CountReads == 7)
                    {
                        _reentered = true;
                        _state.Replace(new[] { "INNER" });
                    }
                    return 1;
                }
            }
        }

        private sealed class StableCountedCollection : SingleItemCountedCollection
        {
            internal StableCountedCollection(string value) : base(value) { }
            public override int Count
            {
                get
                {
                    CountReads++;
                    return 1;
                }
            }
        }
    }

    internal static class SelectionStateFinalCountReentrancySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SelectionStateFinalCountReentrancySmoke.Run();
    }
}
