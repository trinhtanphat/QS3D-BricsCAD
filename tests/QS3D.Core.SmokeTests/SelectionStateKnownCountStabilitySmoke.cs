using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SelectionStateKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            KnownCountOverrunFailsBeforeCurrentAndThrowingTail();
            EnumeratorAcquisitionCountDriftFailsBeforeTraversal();
            GenericCountDriftFailsWithoutPublication();
            ReadOnlyCountDriftFailsWithoutPublication();
            NonGenericCountDriftFailsWithoutPublication();
            KnownCountUnderYieldStillFailsWithoutPublication();
            StableMultiInterfaceCountAndStreamingInputsRemainSupported();
        }

        private static void KnownCountOverrunFailsBeforeCurrentAndThrowingTail()
        {
            var state = SeededState(out var changed);
            var source = new OverrunThenThrowCollection();

            ThrowsContaining<InvalidOperationException>(
                () => state.Replace(source),
                "more entries than its known Count");

            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
            Equal(0, changed());
            SequenceEqual(new[] { "KEEP" }, state.ElementIds);
        }

        private static void EnumeratorAcquisitionCountDriftFailsBeforeTraversal()
        {
            AssertAcquisitionCountFailure(new AcquisitionCountDriftCollection(1, 2, 2), "known Count changed during traversal");
            AssertAcquisitionCountFailure(new AcquisitionCountDriftCollection(1, 0, 0), "known Count changed during traversal");
            AssertAcquisitionCountFailure(new AcquisitionCountDriftCollection(1, -1, -1), "known Count cannot be negative");
            AssertAcquisitionCountFailure(new AcquisitionCountDriftCollection(1, 1, 2), "exposes conflicting known Counts");
        }

        private static void AssertAcquisitionCountFailure(AcquisitionCountDriftCollection source, string expectedText)
        {
            var state = SeededState(out var changed);

            ThrowsContaining<InvalidOperationException>(() => state.Replace(source), expectedText);

            Equal(1, source.GetEnumeratorCalls);
            Equal(0, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
            Equal(0, changed());
            SequenceEqual(new[] { "KEEP" }, state.ElementIds);
        }

        private static void GenericCountDriftFailsWithoutPublication()
        {
            AssertCountDriftFails(new GenericCountDriftCollection(new[] { "A", "B" }, 2, 1));
        }

        private static void ReadOnlyCountDriftFailsWithoutPublication()
        {
            AssertCountDriftFails(new ReadOnlyCountDriftCollection(new[] { "A", "B" }, 2, 3));
        }

        private static void NonGenericCountDriftFailsWithoutPublication()
        {
            AssertCountDriftFails(new NonGenericCountDriftCollection(new[] { "A", "B" }, 2, 0));
        }

        private static void AssertCountDriftFails(IEnumerable<string> source)
        {
            var state = SeededState(out var changed);

            ThrowsContaining<InvalidOperationException>(
                () => state.Replace(source),
                "known Count changed during traversal");

            Equal(0, changed());
            SequenceEqual(new[] { "KEEP" }, state.ElementIds);
        }

        private static void KnownCountUnderYieldStillFailsWithoutPublication()
        {
            var state = SeededState(out var changed);
            var source = new GenericCountDriftCollection(new[] { "A" }, 2, 2);

            ThrowsContaining<InvalidOperationException>(
                () => state.Replace(source),
                "known Count reported 2 entries but traversal produced 1");

            Equal(0, changed());
            SequenceEqual(new[] { "KEEP" }, state.ElementIds);
        }

        private static void StableMultiInterfaceCountAndStreamingInputsRemainSupported()
        {
            var state = new SelectionState();
            var changed = 0;
            state.Changed += (_, _) => changed++;

            state.Replace(new List<string> { " B ", "A", "a" });
            Equal(1, changed);
            SequenceEqual(new[] { "A", "B" }, state.ElementIds);

            state.Replace(Streaming(" C ", "c", "D"));
            Equal(2, changed);
            SequenceEqual(new[] { "C", "D" }, state.ElementIds);
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

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual)
        {
            if (!expected.SequenceEqual(actual, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Expected selection [" + string.Join(", ", expected) + "] but got [" + string.Join(", ", actual) + "].");
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

        private sealed class AcquisitionCountDriftCollection : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly int _before;
            private readonly int _genericAfter;
            private readonly int _readOnlyAfter;
            private bool _acquired;

            public AcquisitionCountDriftCollection(int before, int genericAfter, int readOnlyAfter)
            {
                _before = before;
                _genericAfter = genericAfter;
                _readOnlyAfter = readOnlyAfter;
            }

            public int GetEnumeratorCalls { get; private set; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count => _acquired ? _genericAfter : _before;
            int IReadOnlyCollection<string>.Count => _acquired ? _readOnlyAfter : _before;
            public bool IsReadOnly => true;

            public IEnumerator<string> GetEnumerator()
            {
                GetEnumeratorCalls++;
                _acquired = true;
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(string item) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly AcquisitionCountDriftCollection _owner;

                public Enumerator(AcquisitionCountDriftCollection owner)
                {
                    _owner = owner;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return "A";
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    return false;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class OverrunThenThrowCollection : ICollection<string>
        {
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count => 1;
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
                private readonly OverrunThenThrowCollection _owner;
                private int _index = -1;

                public Enumerator(OverrunThenThrowCollection owner)
                {
                    _owner = owner;
                }

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
                    if (_index <= 1) return true;
                    throw new Exception("SelectionState advanced beyond the known-count overrun boundary.");
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class GenericCountDriftCollection : ICollection<string>
        {
            private readonly string[] _values;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            public GenericCountDriftCollection(string[] values, int before, int after)
            {
                _values = values;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;
            public bool IsReadOnly => true;

            public IEnumerator<string> GetEnumerator()
            {
                for (var i = 0; i < _values.Length; i++) yield return _values[i];
                _completed = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(string item) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        private sealed class ReadOnlyCountDriftCollection : IReadOnlyCollection<string>
        {
            private readonly string[] _values;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            public ReadOnlyCountDriftCollection(string[] values, int before, int after)
            {
                _values = values;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;

            public IEnumerator<string> GetEnumerator()
            {
                for (var i = 0; i < _values.Length; i++) yield return _values[i];
                _completed = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericCountDriftCollection : IEnumerable<string>, ICollection
        {
            private readonly string[] _values;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            public NonGenericCountDriftCollection(string[] values, int before, int after)
            {
                _values = values;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                for (var i = 0; i < _values.Length; i++) yield return _values[i];
                _completed = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }

    internal static class SelectionStateKnownCountStabilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SelectionStateKnownCountStabilitySmoke.Run();
    }
}