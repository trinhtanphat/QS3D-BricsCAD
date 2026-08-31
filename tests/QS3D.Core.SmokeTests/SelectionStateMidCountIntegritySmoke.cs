using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SelectionStateMidCountIntegritySmoke
    {
        internal static void Run()
        {
            DriftAfterCurrentFailsBeforeNextMoveNext();
            MoveNextInducedDriftFailsBeforeCurrent();
            CrossInterfaceConflictFailsBeforeNextMoveNext();
            StableKnownCountAndStreamingRemainAccepted();
        }

        private static void DriftAfterCurrentFailsBeforeNextMoveNext()
        {
            var state = SeededState(out var changed);
            var source = new DriftAfterCurrentCollection();

            ThrowsContaining<InvalidOperationException>(
                () => state.Replace(source),
                "known Count changed during traversal");

            Equal(1, source.MoveNextCalls, "pre-MoveNext drift MoveNext calls");
            Equal(1, source.CurrentReads, "pre-MoveNext drift Current reads");
            Equal(0, changed(), "pre-MoveNext drift Changed events");
            SequenceEqual(new[] { "KEEP" }, state.ElementIds, "pre-MoveNext drift publication");
        }

        private static void MoveNextInducedDriftFailsBeforeCurrent()
        {
            var state = SeededState(out var changed);
            var source = new DriftInsideMoveNextCollection();

            ThrowsContaining<InvalidOperationException>(
                () => state.Replace(source),
                "known Count changed during traversal");

            Equal(2, source.MoveNextCalls, "MoveNext-induced drift MoveNext calls");
            Equal(1, source.CurrentReads, "MoveNext-induced drift Current reads");
            Equal(0, changed(), "MoveNext-induced drift Changed events");
            SequenceEqual(new[] { "KEEP" }, state.ElementIds, "MoveNext-induced drift publication");
        }

        private static void CrossInterfaceConflictFailsBeforeNextMoveNext()
        {
            var state = SeededState(out var changed);
            var source = new CrossInterfaceConflictCollection();

            ThrowsContaining<InvalidOperationException>(
                () => state.Replace(source),
                "conflicting known Counts");

            Equal(1, source.MoveNextCalls, "cross-interface drift MoveNext calls");
            Equal(1, source.CurrentReads, "cross-interface drift Current reads");
            Equal(0, changed(), "cross-interface drift Changed events");
            SequenceEqual(new[] { "KEEP" }, state.ElementIds, "cross-interface drift publication");
        }

        private static void StableKnownCountAndStreamingRemainAccepted()
        {
            var state = new SelectionState();
            var changed = 0;
            state.Changed += (_, _) => changed++;

            state.Replace(new List<string> { " B ", "A", "a" });
            Equal(1, changed, "stable known Count Changed events");
            SequenceEqual(new[] { "A", "B" }, state.ElementIds, "stable known Count output");

            state.Replace(Streaming(" C ", "c", "D"));
            Equal(2, changed, "streaming Changed events");
            SequenceEqual(new[] { "C", "D" }, state.ElementIds, "streaming output");
        }

        private static SelectionState SeededState(out Func<int> changed)
        {
            var state = new SelectionState();
            state.Replace(new[] { "KEEP" });
            var count = 0;
            state.Changed += (_, _) => count++;
            changed = () => count;
            return state;
        }

        private static IEnumerable<string> Streaming(params string[] values)
        {
            foreach (var value in values) yield return value;
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

        private sealed class DriftAfterCurrentCollection : ICollection<string>
        {
            private int _count = 2;
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count => _count;
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
                private readonly DriftAfterCurrentCollection _owner;
                private int _index = -1;

                public Enumerator(DriftAfterCurrentCollection owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index == 0) _owner._count = 1;
                        return _index == 0 ? "A" : "B";
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index <= 1) return true;
                    throw new Exception("SelectionState advanced after Count drift should have been decisive.");
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class DriftInsideMoveNextCollection : ICollection<string>
        {
            private int _count = 2;
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count => _count;
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
                private readonly DriftInsideMoveNextCollection _owner;
                private int _index = -1;

                public Enumerator(DriftInsideMoveNextCollection owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _index == 0 ? "A" : "B";
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index == 1) _owner._count = 1;
                    return _index <= 1;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class CrossInterfaceConflictCollection : ICollection<string>, IReadOnlyCollection<string>
        {
            private bool _conflict;
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            int ICollection<string>.Count => 2;
            int IReadOnlyCollection<string>.Count => _conflict ? 1 : 2;
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
                private readonly CrossInterfaceConflictCollection _owner;
                private int _index = -1;

                public Enumerator(CrossInterfaceConflictCollection owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index == 0) _owner._conflict = true;
                        return _index == 0 ? "A" : "B";
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index <= 1) return true;
                    throw new Exception("SelectionState advanced after cross-interface Count conflict should have been decisive.");
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class SelectionStateMidCountIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SelectionStateMidCountIntegritySmoke.Run();
    }
}
