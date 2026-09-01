using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleCatalogSaveKnownCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectKnownCountOverrunBeforeUnexpectedCurrent();
            RejectTransientMoveNextCountDrift();
            RejectTransientCurrentCountDrift();
            RejectKnownCountUnderYield();
            StableCountedSaveStillPersists();
            PureStreamingSaveStillPersists();
        }

        private static void RejectKnownCountOverrunBeforeUnexpectedCurrent()
        {
            var project = Project("P-SCHEDULE-SAVE-OVERRUN");
            var source = new HostileCountedDefinitions(new[] { Definition("S-OVERRUN") }, admittedCount: 0);
            var beforeVersion = project.ChangeVersion;

            var error = ExpectInvalidOperation(() => SemanticScheduleCatalog.Save(project, source));
            Contains("known Count does not match traversal", error.Message, "Known Count overrun must retain a deterministic cardinality diagnostic.");
            Equal(1, source.MoveNextCalls, "Known Count zero must discover the unexpected item with one MoveNext call.");
            Equal(0, source.CurrentReads, "Known Count overrun must fail before unexpected Current is observed.");
            Unchanged(project, beforeVersion, "Known Count overrun");
        }

        private static void RejectTransientMoveNextCountDrift()
        {
            var project = Project("P-SCHEDULE-SAVE-MOVENEXT");
            var source = new HostileCountedDefinitions(
                new[] { Definition("S-MOVENEXT") },
                admittedCount: 1,
                driftAfterMoveNext: true);
            var beforeVersion = project.ChangeVersion;

            var error = ExpectInvalidOperation(() => SemanticScheduleCatalog.Save(project, source));
            Contains("Count", error.Message, "Transient MoveNext Count drift must fail closed on Count evidence.");
            Equal(1, source.MoveNextCalls, "Transient MoveNext drift must be detected on the first item boundary.");
            Equal(0, source.CurrentReads, "MoveNext Count drift must be rejected before Current.");
            Unchanged(project, beforeVersion, "Transient MoveNext Count drift");
        }

        private static void RejectTransientCurrentCountDrift()
        {
            var project = Project("P-SCHEDULE-SAVE-CURRENT");
            var source = new HostileCountedDefinitions(
                new[] { Definition("S-CURRENT") },
                admittedCount: 1,
                driftAfterCurrent: true);
            var beforeVersion = project.ChangeVersion;

            var error = ExpectInvalidOperation(() => SemanticScheduleCatalog.Save(project, source));
            Contains("Count", error.Message, "Transient Current Count drift must fail closed on Count evidence.");
            Equal(1, source.MoveNextCalls, "Transient Current drift must reach exactly one admitted item.");
            Equal(1, source.CurrentReads, "Transient Current drift must read Current exactly once before rebound.");
            Unchanged(project, beforeVersion, "Transient Current Count drift");
        }

        private static void RejectKnownCountUnderYield()
        {
            var project = Project("P-SCHEDULE-SAVE-UNDERYIELD");
            var source = new HostileCountedDefinitions(new[] { Definition("S-UNDERYIELD") }, admittedCount: 2);
            var beforeVersion = project.ChangeVersion;

            var error = ExpectInvalidOperation(() => SemanticScheduleCatalog.Save(project, source));
            Contains("known Count does not match traversal", error.Message, "Known Count under-yield must fail closed after terminal MoveNext.");
            Equal(2, source.MoveNextCalls, "One-item under-yield must observe terminal MoveNext=false.");
            Equal(1, source.CurrentReads, "One-item under-yield must read only the single yielded definition.");
            Unchanged(project, beforeVersion, "Known Count under-yield");
        }

        private static void StableCountedSaveStillPersists()
        {
            var project = Project("P-SCHEDULE-SAVE-STABLE");
            var source = new HostileCountedDefinitions(
                new[] { Definition("S-STABLE-1"), Definition("S-STABLE-2") },
                admittedCount: 2);

            SemanticScheduleCatalog.Save(project, source);

            var loaded = SemanticScheduleCatalog.Load(project);
            Equal(2, loaded.Count, "Stable counted Save must persist every admitted definition.");
            Equal(3, source.MoveNextCalls, "Stable two-item counted Save must terminate normally.");
            Equal(2, source.CurrentReads, "Stable counted Save must observe Current once per definition.");
        }

        private static void PureStreamingSaveStillPersists()
        {
            var project = Project("P-SCHEDULE-SAVE-STREAM");
            var source = new StreamingDefinitions(new[] { Definition("S-STREAM") });

            SemanticScheduleCatalog.Save(project, source);

            var loaded = SemanticScheduleCatalog.Load(project);
            Equal(1, loaded.Count, "Pure streaming Save must remain supported.");
            Equal("S-STREAM", loaded[0].Id, "Pure streaming Save must preserve semantic identity.");
            Equal(2, source.MoveNextCalls, "Single-item streaming Save must observe terminal MoveNext=false.");
            Equal(1, source.CurrentReads, "Single-item streaming Save must observe Current once.");
        }

        private static ProjectState Project(string id) => new ProjectState(id, id);

        private static SemanticScheduleDefinition Definition(string id) => new SemanticScheduleDefinition(
            id,
            id,
            id,
            Array.Empty<ElementCategory>(),
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[] { new SemanticDocumentationColumn("Id", "{Id}") });

        private static void Unchanged(ProjectState project, long beforeVersion, string label)
        {
            Equal(beforeVersion, project.ChangeVersion, label + " must not change project version.");
            Equal(false, project.Metadata.ContainsKey(SemanticScheduleCatalog.MetadataKey), label + " must not publish metadata.");
        }

        private static InvalidOperationException ExpectInvalidOperation(Action action)
        {
            try { action(); }
            catch (InvalidOperationException ex) { return ex; }
            throw new InvalidOperationException("Expected InvalidOperationException.");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual=" + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private sealed class HostileCountedDefinitions : ICollection<SemanticScheduleDefinition>, IReadOnlyCollection<SemanticScheduleDefinition>
        {
            private readonly SemanticScheduleDefinition[] _items;
            private readonly int _admittedCount;
            private readonly bool _driftAfterMoveNext;
            private readonly bool _driftAfterCurrent;
            private bool _pendingDrift;

            internal HostileCountedDefinitions(
                SemanticScheduleDefinition[] items,
                int admittedCount,
                bool driftAfterMoveNext = false,
                bool driftAfterCurrent = false)
            {
                _items = items;
                _admittedCount = admittedCount;
                _driftAfterMoveNext = driftAfterMoveNext;
                _driftAfterCurrent = driftAfterCurrent;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            int ICollection<SemanticScheduleDefinition>.Count => ReadCount();
            int IReadOnlyCollection<SemanticScheduleDefinition>.Count => ReadCount();
            bool ICollection<SemanticScheduleDefinition>.IsReadOnly => true;

            private int ReadCount()
            {
                if (!_pendingDrift) return _admittedCount;
                _pendingDrift = false;
                return checked(_admittedCount + 1);
            }

            public IEnumerator<SemanticScheduleDefinition> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(SemanticScheduleDefinition item) => ((ICollection<SemanticScheduleDefinition>)_items).Contains(item);
            public void CopyTo(SemanticScheduleDefinition[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(SemanticScheduleDefinition item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(SemanticScheduleDefinition item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<SemanticScheduleDefinition>
            {
                private readonly HostileCountedDefinitions _owner;
                private int _index = -1;

                internal Enumerator(HostileCountedDefinitions owner) { _owner = owner; }

                public SemanticScheduleDefinition Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _owner._items.Length) throw new InvalidOperationException("Current outside valid position.");
                        if (_owner._driftAfterCurrent) _owner._pendingDrift = true;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    var moved = _index < _owner._items.Length;
                    if (moved && _owner._driftAfterMoveNext) _owner._pendingDrift = true;
                    return moved;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StreamingDefinitions : IEnumerable<SemanticScheduleDefinition>
        {
            private readonly SemanticScheduleDefinition[] _items;
            internal StreamingDefinitions(SemanticScheduleDefinition[] items) { _items = items; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<SemanticScheduleDefinition> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<SemanticScheduleDefinition>
            {
                private readonly StreamingDefinitions _owner;
                private int _index = -1;
                internal Enumerator(StreamingDefinitions owner) { _owner = owner; }

                public SemanticScheduleDefinition Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _owner._items.Length) throw new InvalidOperationException("Current outside valid position.");
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public bool MoveNext() { _owner.MoveNextCalls++; _index++; return _index < _owner._items.Length; }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}