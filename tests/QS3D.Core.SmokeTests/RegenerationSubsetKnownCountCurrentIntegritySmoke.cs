using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationSubsetKnownCountCurrentIntegritySmoke
    {
        internal static void Run()
        {
            KnownCountOverrunRejectsBeforeUnexpectedCurrent();
            KnownCountUnderYieldStillFails();
            PostTraversalCountDriftFailsClosed();
            MoveNextTransientGrowthRejectsBeforeCurrent();
            MoveNextTransientShrinkRejectsBeforeCurrent();
            MoveNextTransientNegativeRejectsBeforeCurrent();
            MoveNextTransientCrossInterfaceConflictRejectsBeforeCurrent();
            ExactKnownCountRemainsAccepted();
            PureStreamingRemainsAccepted();
        }

        private static void KnownCountOverrunRejectsBeforeUnexpectedCurrent()
        {
            var source = new StableCountedIds(new[] { "E1", "E2" }, 1);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source));
            Contains(error.Message, "count changed during enumeration");
            Equal(5, source.CountReads);
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void KnownCountUnderYieldStillFails()
        {
            var source = new StableCountedIds(new[] { "E1" }, 2);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source));
            Contains(error.Message, "count changed during enumeration");
            Equal(4, source.CountReads);
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void PostTraversalCountDriftFailsClosed()
        {
            var source = new TerminalDriftCountedIds(new[] { "E1" }, 1, 2);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source));
            Contains(error.Message, "count changed during enumeration");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void MoveNextTransientGrowthRejectsBeforeCurrent()
        {
            var source = new MoveNextTransientCountedIds(new[] { "E1" }, 1, 2);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source));
            Contains(error.Message, "count changed during enumeration");
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void MoveNextTransientShrinkRejectsBeforeCurrent()
        {
            var source = new MoveNextTransientCountedIds(new[] { "E1" }, 1, 0);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source));
            Contains(error.Message, "count changed during enumeration");
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void MoveNextTransientNegativeRejectsBeforeCurrent()
        {
            var source = new MoveNextTransientCountedIds(new[] { "E1" }, 1, -1);
            var error = Throws<ArgumentException>(() => Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source));
            Contains(error.Message, "invalid negative known count");
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void MoveNextTransientCrossInterfaceConflictRejectsBeforeCurrent()
        {
            var source = new MoveNextConflictingCountedIds(new[] { "E1" }, 1, 2);
            var error = Throws<ArgumentException>(() => Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source));
            Contains(error.Message, "conflicting known counts");
            Equal(1, source.MoveNextCalls);
            Equal(0, source.CurrentReads);
        }

        private static void ExactKnownCountRemainsAccepted()
        {
            var source = new StableCountedIds(new[] { "E2", "E1" }, 2);
            var regenerated = Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source);
            Equal(0, regenerated);
            Equal(7, source.CountReads);
            Equal(3, source.MoveNextCalls);
            Equal(2, source.CurrentReads);
        }

        private static void PureStreamingRemainsAccepted()
        {
            var regenerated = Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), Streaming("E2", "E1"));
            Equal(0, regenerated);
        }

        private static IEnumerable<string> Streaming(params string[] values)
        {
            foreach (var value in values) yield return value;
        }

        private static ProjectState ProjectWithElements(params string[] ids)
        {
            var project = new ProjectState("regen-subset-count-integrity", "Regeneration Subset Count Integrity");
            foreach (var id in ids)
            {
                var element = new ProjectElement(id, ElementCategory.Grid, string.Empty, string.Empty, string.Empty);
                element.MarkClean(ElementDirtyFlags.All);
                project.Elements.Add(element);
            }
            return project;
        }

        private static RegenerationEngine Engine() =>
            new RegenerationEngine(new DependencyGraph(), Array.Empty<IElementRegenerator>());

        private sealed class StableCountedIds : IReadOnlyCollection<string>
        {
            private readonly string[] _values;
            private readonly int _count;

            internal StableCountedIds(string[] values, int count)
            {
                _values = values;
                _count = count;
            }

            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _count;
                }
            }

            public IEnumerator<string> GetEnumerator() => new TrackingEnumerator(_values, this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TrackingEnumerator : IEnumerator<string>
            {
                private readonly string[] _values;
                private readonly StableCountedIds _owner;
                private int _index = -1;

                internal TrackingEnumerator(string[] values, StableCountedIds owner)
                {
                    _values = values;
                    _owner = owner;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _values[_index];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _values.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class TerminalDriftCountedIds : IReadOnlyCollection<string>
        {
            private readonly string[] _values;
            private readonly int _initialCount;
            private readonly int _terminalCount;

            internal TerminalDriftCountedIds(string[] values, int initialCount, int terminalCount)
            {
                _values = values;
                _initialCount = initialCount;
                _terminalCount = terminalCount;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public int Count => MoveNextCalls > _values.Length ? _terminalCount : _initialCount;
            public IEnumerator<string> GetEnumerator() => new TrackingEnumerator(_values, this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TrackingEnumerator : IEnumerator<string>
            {
                private readonly string[] _values;
                private readonly TerminalDriftCountedIds _owner;
                private int _index = -1;

                internal TrackingEnumerator(string[] values, TerminalDriftCountedIds owner)
                {
                    _values = values;
                    _owner = owner;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _values[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _values.Length;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class MoveNextTransientCountedIds : IReadOnlyCollection<string>
        {
            private readonly string[] _values;
            private readonly int _stableCount;
            private readonly int _transientCount;
            private bool _transient;

            internal MoveNextTransientCountedIds(string[] values, int stableCount, int transientCount)
            {
                _values = values;
                _stableCount = stableCount;
                _transientCount = transientCount;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public int Count => _transient ? _transientCount : _stableCount;
            public IEnumerator<string> GetEnumerator() => new TrackingEnumerator(_values, this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TrackingEnumerator : IEnumerator<string>
            {
                private readonly string[] _values;
                private readonly MoveNextTransientCountedIds _owner;
                private int _index = -1;

                internal TrackingEnumerator(string[] values, MoveNextTransientCountedIds owner)
                {
                    _values = values;
                    _owner = owner;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._transient = false;
                        return _values[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    var moved = _index < _values.Length;
                    if (moved) _owner._transient = true;
                    return moved;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class MoveNextConflictingCountedIds : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly string[] _values;
            private readonly int _stableCount;
            private readonly int _transientReadOnlyCount;
            private bool _transient;

            internal MoveNextConflictingCountedIds(string[] values, int stableCount, int transientReadOnlyCount)
            {
                _values = values;
                _stableCount = stableCount;
                _transientReadOnlyCount = transientReadOnlyCount;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            int ICollection<string>.Count => _stableCount;
            int IReadOnlyCollection<string>.Count => _transient ? _transientReadOnlyCount : _stableCount;
            bool ICollection<string>.IsReadOnly => true;

            public IEnumerator<string> GetEnumerator() => new TrackingEnumerator(_values, this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Contains(string item) => Array.IndexOf(_values, item) >= 0;
            void ICollection<string>.CopyTo(string[] array, int arrayIndex) => _values.CopyTo(array, arrayIndex);
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();

            private sealed class TrackingEnumerator : IEnumerator<string>
            {
                private readonly string[] _values;
                private readonly MoveNextConflictingCountedIds _owner;
                private int _index = -1;

                internal TrackingEnumerator(string[] values, MoveNextConflictingCountedIds owner)
                {
                    _values = values;
                    _owner = owner;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._transient = false;
                        return _values[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    var moved = _index < _values.Length;
                    if (moved) _owner._transient = true;
                    return moved;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static T Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T error) { return error; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Contains(string value, string expected)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected text containing '" + expected + "', got '" + (value ?? string.Empty) + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class RegenerationSubsetKnownCountCurrentIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RegenerationSubsetKnownCountCurrentIntegritySmoke.Run();
    }
}