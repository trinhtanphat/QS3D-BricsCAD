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
            ProjectBoundRejectsStreamingInputBeforeUnexpectedCurrent();
            KnownCountUnderYieldStillFails();
            PostTraversalCountDriftFailsClosed();
            ExactKnownCountRemainsAccepted();
        }

        private static void KnownCountOverrunRejectsBeforeUnexpectedCurrent()
        {
            var source = new HostileCountedIds(new[] { "E1", "E2" }, 1);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source));
            Contains(error.Message, "count changed during enumeration");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void ProjectBoundRejectsStreamingInputBeforeUnexpectedCurrent()
        {
            var source = new HostileStreamingIds(new[] { "E1", "E2" });
            var error = Throws<ArgumentException>(() => Engine().RegenerateDirtySubset(ProjectWithElements("E1"), source));
            Contains(error.Message, "cannot exceed project element count of 1");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void KnownCountUnderYieldStillFails()
        {
            var source = new HostileCountedIds(new[] { "E1" }, 2);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source));
            Contains(error.Message, "count changed during enumeration");
            Equal(2, source.CountReads);
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void PostTraversalCountDriftFailsClosed()
        {
            var source = new HostileCountedIds(new[] { "E1" }, 1, 2);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source));
            Contains(error.Message, "count changed during enumeration");
            Equal(2, source.CountReads);
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void ExactKnownCountRemainsAccepted()
        {
            var source = new HostileCountedIds(new[] { "E2", "E1" }, 2);
            var regenerated = Engine().RegenerateDirtySubset(ProjectWithElements("E1", "E2"), source);
            Equal(0, regenerated);
            Equal(2, source.CountReads);
            Equal(3, source.MoveNextCalls);
            Equal(2, source.CurrentReads);
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

        private sealed class HostileCountedIds : IReadOnlyCollection<string>
        {
            private readonly string[] _values;
            private readonly int _initialCount;
            private readonly int _reboundCount;

            internal HostileCountedIds(string[] values, int initialCount, int? reboundCount = null)
            {
                _values = values;
                _initialCount = initialCount;
                _reboundCount = reboundCount ?? initialCount;
            }

            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return CountReads == 1 ? _initialCount : _reboundCount;
                }
            }

            public IEnumerator<string> GetEnumerator() => new TrackingEnumerator(_values, this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TrackingEnumerator : IEnumerator<string>
            {
                private readonly string[] _values;
                private readonly HostileCountedIds _owner;
                private int _index = -1;

                internal TrackingEnumerator(string[] values, HostileCountedIds owner)
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

        private sealed class HostileStreamingIds : IEnumerable<string>
        {
            private readonly string[] _values;

            internal HostileStreamingIds(string[] values) => _values = values;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new TrackingEnumerator(_values, this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TrackingEnumerator : IEnumerator<string>
            {
                private readonly string[] _values;
                private readonly HostileStreamingIds _owner;
                private int _index = -1;

                internal TrackingEnumerator(string[] values, HostileStreamingIds owner)
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
