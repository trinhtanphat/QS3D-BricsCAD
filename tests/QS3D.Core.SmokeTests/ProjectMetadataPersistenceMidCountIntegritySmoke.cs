using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMetadataPersistenceMidCountIntegritySmoke
    {
        private const string CountChanged = "Project metadata persistence input Count changed during traversal.";
        private const string NegativeCount = "Project metadata persistence input exposes an invalid negative Count.";
        private const string ConflictingCount = "Project metadata persistence input exposes conflicting Count contracts.";

        [ModuleInitializer]
        internal static void Initialize()
        {
            CountDriftAfterCurrentFailsBeforeNextMoveNext();
            MoveNextInducedCountDriftFailsBeforeCurrent();
            CrossInterfaceConflictAfterCurrentFailsBeforeNextMoveNext();
            NegativeCountAfterCurrentFailsBeforeNextMoveNext();
            StableMultiInterfaceCountPublishes();
            PureStreamingInputRemainsSupported();
        }

        private static void CountDriftAfterCurrentFailsBeforeNextMoveNext()
        {
            var project = SeededProject("after-current");
            var input = new DriftAfterCurrentCollection(
                2,
                3,
                Pair("first", "a"),
                Pair("second", "b"));

            ExpectFailure(project, input, "Count drift after Current", CountChanged);
            Equal(1, input.MoveNextCalls, "Count drift after Current MoveNext calls");
            Equal(1, input.CurrentReads, "Count drift after Current Current reads");
            AssertSeedUnchanged(project, "Count drift after Current");
        }

        private static void MoveNextInducedCountDriftFailsBeforeCurrent()
        {
            var project = SeededProject("move-next-drift");
            var input = new DriftDuringMoveNextCollection(Pair("never-read", "value"));

            ExpectFailure(project, input, "MoveNext-induced Count drift", CountChanged);
            Equal(1, input.MoveNextCalls, "MoveNext-induced Count drift MoveNext calls");
            Equal(0, input.CurrentReads, "MoveNext-induced Count drift Current reads");
            AssertSeedUnchanged(project, "MoveNext-induced Count drift");
        }

        private static void CrossInterfaceConflictAfterCurrentFailsBeforeNextMoveNext()
        {
            var project = SeededProject("conflict");
            var input = new ConflictAfterCurrentCollection(
                Pair("first", "a"),
                Pair("second", "b"));

            ExpectFailure(project, input, "mid-traversal Count conflict", ConflictingCount);
            Equal(1, input.MoveNextCalls, "mid-traversal Count conflict MoveNext calls");
            Equal(1, input.CurrentReads, "mid-traversal Count conflict Current reads");
            AssertSeedUnchanged(project, "mid-traversal Count conflict");
        }

        private static void NegativeCountAfterCurrentFailsBeforeNextMoveNext()
        {
            var project = SeededProject("negative");
            var input = new NegativeAfterCurrentCollection(
                Pair("first", "a"),
                Pair("second", "b"));

            ExpectFailure(project, input, "mid-traversal negative Count", NegativeCount);
            Equal(1, input.MoveNextCalls, "mid-traversal negative Count MoveNext calls");
            Equal(1, input.CurrentReads, "mid-traversal negative Count Current reads");
            AssertSeedUnchanged(project, "mid-traversal negative Count");
        }

        private static void StableMultiInterfaceCountPublishes()
        {
            var project = SeededProject("stable");
            var input = new StableMultiInterfaceCollection(
                Pair("stable-a", "a"),
                Pair("stable-b", "b"));

            InvokePersistenceReplacement(project, input);

            Equal(2, project.Metadata.Count, "stable multi-interface metadata count");
            Equal("a", project.Metadata["stable-a"], "stable multi-interface first value");
            Equal("b", project.Metadata["stable-b"], "stable multi-interface second value");
            Equal(3, input.MoveNextCalls, "stable multi-interface MoveNext calls");
            Equal(2, input.CurrentReads, "stable multi-interface Current reads");
        }

        private static void PureStreamingInputRemainsSupported()
        {
            var project = SeededProject("streaming");
            InvokePersistenceReplacement(project, Streaming(
                Pair("stream-a", "a"),
                Pair("stream-b", "b")));

            Equal(2, project.Metadata.Count, "streaming metadata count");
            Equal("a", project.Metadata["stream-a"], "streaming first value");
            Equal("b", project.Metadata["stream-b"], "streaming second value");
        }

        private static IEnumerable<KeyValuePair<string, string>> Streaming(params KeyValuePair<string, string>[] items)
        {
            for (var i = 0; i < items.Length; i++)
                yield return items[i];
        }

        private static void ExpectFailure(
            ProjectState project,
            IEnumerable<KeyValuePair<string, string>> input,
            string label,
            string expectedMessage)
        {
            try
            {
                InvokePersistenceReplacement(project, input);
                throw new InvalidOperationException(label + ": expected Count-integrity rejection.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException failure)
            {
                if (!string.Equals(expectedMessage, failure.Message, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        label + ": wrong failure. Expected '" + expectedMessage + "', got '" + failure.Message + "'.");
            }
        }

        private static void InvokePersistenceReplacement(
            ProjectState project,
            IEnumerable<KeyValuePair<string, string>> input)
        {
            var method = project.Metadata.GetType().GetMethod(
                "ReplacePersistenceState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException("Project metadata persistence replacement method was not found.");
            method.Invoke(project.Metadata, new object[] { input });
        }

        private static ProjectState SeededProject(string suffix)
        {
            var project = new ProjectState("metadata-mid-count-" + suffix, "Metadata Mid Count " + suffix);
            project.Metadata.Add("seed", "original");
            return project;
        }

        private static void AssertSeedUnchanged(ProjectState project, string label)
        {
            Equal(1, project.Metadata.Count, label + " atomic metadata count");
            Equal("original", project.Metadata["seed"], label + " atomic metadata value");
        }

        private static KeyValuePair<string, string> Pair(string key, string value) =>
            new KeyValuePair<string, string>(key, value);

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private abstract class TrackingEnumerable : IEnumerable<KeyValuePair<string, string>>
        {
            private readonly KeyValuePair<string, string>[] _items;

            protected TrackingEnumerable(params KeyValuePair<string, string>[] items)
            {
                _items = items;
            }

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }

            protected virtual void OnSuccessfulMoveNext(int index) { }
            protected virtual void OnCurrentRead(int index) { }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return new Enumerator(this, _items);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<KeyValuePair<string, string>>
            {
                private readonly TrackingEnumerable _owner;
                private readonly KeyValuePair<string, string>[] _items;
                private int _index = -1;

                internal Enumerator(TrackingEnumerable owner, KeyValuePair<string, string>[] items)
                {
                    _owner = owner;
                    _items = items;
                }

                public KeyValuePair<string, string> Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner.OnCurrentRead(_index);
                        return _items[_index];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    var next = _index + 1;
                    if (next >= _items.Length)
                        return false;
                    _index = next;
                    _owner.OnSuccessfulMoveNext(_index);
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class DriftAfterCurrentCollection : TrackingEnumerable, ICollection<KeyValuePair<string, string>>
        {
            private readonly int _initialCount;
            private readonly int _driftedCount;
            private bool _drifted;

            internal DriftAfterCurrentCollection(
                int initialCount,
                int driftedCount,
                params KeyValuePair<string, string>[] items)
                : base(items)
            {
                _initialCount = initialCount;
                _driftedCount = driftedCount;
            }

            public int Count => _drifted ? _driftedCount : _initialCount;
            public bool IsReadOnly => true;
            protected override void OnCurrentRead(int index) { if (index == 0) _drifted = true; }
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
        }

        private sealed class DriftDuringMoveNextCollection : TrackingEnumerable, ICollection<KeyValuePair<string, string>>
        {
            private bool _drifted;

            internal DriftDuringMoveNextCollection(KeyValuePair<string, string> item) : base(item) { }

            public int Count => _drifted ? 2 : 1;
            public bool IsReadOnly => true;
            protected override void OnSuccessfulMoveNext(int index) { _drifted = true; }
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
        }

        private sealed class ConflictAfterCurrentCollection : TrackingEnumerable,
            ICollection<KeyValuePair<string, string>>,
            IReadOnlyCollection<KeyValuePair<string, string>>
        {
            private bool _conflicting;

            internal ConflictAfterCurrentCollection(params KeyValuePair<string, string>[] items) : base(items) { }

            int ICollection<KeyValuePair<string, string>>.Count => 2;
            int IReadOnlyCollection<KeyValuePair<string, string>>.Count => _conflicting ? 3 : 2;
            public bool IsReadOnly => true;
            protected override void OnCurrentRead(int index) { if (index == 0) _conflicting = true; }
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
        }

        private sealed class NegativeAfterCurrentCollection : TrackingEnumerable, ICollection<KeyValuePair<string, string>>
        {
            private bool _negative;

            internal NegativeAfterCurrentCollection(params KeyValuePair<string, string>[] items) : base(items) { }

            public int Count => _negative ? -1 : 2;
            public bool IsReadOnly => true;
            protected override void OnCurrentRead(int index) { if (index == 0) _negative = true; }
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
        }

        private sealed class StableMultiInterfaceCollection : TrackingEnumerable,
            ICollection<KeyValuePair<string, string>>,
            IReadOnlyCollection<KeyValuePair<string, string>>,
            ICollection
        {
            internal StableMultiInterfaceCollection(params KeyValuePair<string, string>[] items) : base(items)
            {
                StableCount = items.Length;
            }

            private int StableCount { get; }
            int ICollection<KeyValuePair<string, string>>.Count => StableCount;
            int IReadOnlyCollection<KeyValuePair<string, string>>.Count => StableCount;
            int ICollection.Count => StableCount;
            public bool IsReadOnly => true;
            public object SyncRoot => this;
            public bool IsSynchronized => false;
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}
