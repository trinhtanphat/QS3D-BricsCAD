using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationTargetKnownCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CurrentDriftWinsBeforeTargetValidation();
            MoveNextDriftFailsBeforeCurrent();
            KnownOverYieldFailsBeforeUnexpectedCurrent();
            KnownUnderYieldFailsAtPublicationBoundary();
            StableMultiInterfaceCountRemainsAccepted();
            PureStreamingSourceRemainsAccepted();
        }

        private static void CurrentDriftWinsBeforeTargetValidation()
        {
            var source = new HostileCountCollection(new[] { " " }, 1, DriftPoint.Current);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(Project("E1"), source));
            Contains(error.Message, "target id count changed during enumeration");
            Equal(1, source.CurrentReads, "Current drift Current reads");
        }

        private static void MoveNextDriftFailsBeforeCurrent()
        {
            var source = new HostileCountCollection(new[] { "E1" }, 1, DriftPoint.MoveNext);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(Project("E1"), source));
            Contains(error.Message, "target id count changed during enumeration");
            Equal(0, source.CurrentReads, "MoveNext drift Current reads");
        }

        private static void KnownOverYieldFailsBeforeUnexpectedCurrent()
        {
            var source = new HostileCountCollection(new[] { "E1", "E2" }, 1, DriftPoint.None);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(Project("E1", "E2"), source));
            Contains(error.Message, "target id count changed during enumeration");
            Equal(1, source.CurrentReads, "known over-yield Current reads");
        }

        private static void KnownUnderYieldFailsAtPublicationBoundary()
        {
            var source = new HostileCountCollection(new[] { "E1" }, 2, DriftPoint.None);
            var error = Throws<InvalidOperationException>(() => Engine().RegenerateDirtySubset(Project("E1", "E2"), source));
            Contains(error.Message, "target id count changed during enumeration");
            Equal(1, source.CurrentReads, "known under-yield Current reads");
        }

        private static void StableMultiInterfaceCountRemainsAccepted()
        {
            var source = new HostileCountCollection(new[] { "E1" }, 1, DriftPoint.None);
            var regenerated = Engine().RegenerateDirtySubset(Project("E1"), source);
            Equal(0, regenerated, "stable counted regeneration result");
            Equal(1, source.CurrentReads, "stable counted Current reads");
        }

        private static void PureStreamingSourceRemainsAccepted()
        {
            var regenerated = Engine().RegenerateDirtySubset(Project("E1"), Stream("E1"));
            Equal(0, regenerated, "pure streaming regeneration result");
        }

        private static IEnumerable<string> Stream(params string[] values)
        {
            foreach (var value in values) yield return value;
        }

        private static ProjectState Project(params string[] ids)
        {
            var project = new ProjectState("regen-target-count", "Regeneration Target Count");
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

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private enum DriftPoint
        {
            None,
            MoveNext,
            Current
        }

        private sealed class HostileCountCollection : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly string[] _values;
            private readonly int _reportedCount;
            private readonly DriftPoint _driftPoint;
            private int _driftCountReads;

            internal HostileCountCollection(string[] values, int reportedCount, DriftPoint driftPoint)
            {
                _values = values;
                _reportedCount = reportedCount;
                _driftPoint = driftPoint;
            }

            public int Count
            {
                get
                {
                    if (_driftCountReads > 0)
                    {
                        _driftCountReads--;
                        return _reportedCount + 1;
                    }
                    return _reportedCount;
                }
            }

            int IReadOnlyCollection<string>.Count => Count;
            int ICollection.Count => Count;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => Array.IndexOf(_values, item) >= 0;
            public void CopyTo(string[] array, int arrayIndex) => _values.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index) => _values.CopyTo(array, index);
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();

            private void ArmDrift()
            {
                // ValidateKnownTargetIdCounts reads generic, read-only and non-generic Count in sequence.
                _driftCountReads = 3;
            }

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly HostileCountCollection _owner;
                private int _index = -1;

                internal Enumerator(HostileCountCollection owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._driftPoint == DriftPoint.Current) _owner.ArmDrift();
                        return _owner._values[_index];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _index++;
                    var moved = _index < _owner._values.Length;
                    if (moved && _owner._driftPoint == DriftPoint.MoveNext) _owner.ArmDrift();
                    return moved;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}