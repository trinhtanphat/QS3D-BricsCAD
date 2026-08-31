using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMetadataKnownCountOverrunSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            KnownCountOverrunWinsBeforeUnexpectedCurrent();
            KnownCountOverrunWinsBeforeNullKeyValidation();
            KnownCountOverrunWinsBeforeDuplicateKeyValidation();
            StableCountedInputRemainsAccepted();
        }

        private static void KnownCountOverrunWinsBeforeUnexpectedCurrent()
        {
            var project = NewProject("current");
            project.Metadata.Add("seed", "original");
            var input = new CountedMetadataCollection(
                1,
                new KeyValuePair<string, string>("first", "v1"),
                new KeyValuePair<string, string>("unexpected", "v2"));
            input.ThrowOnUnexpectedCurrent = true;

            ExpectCountOverrun(project, input, "Current overrun");
            Equal(2, input.MoveNextCalls, "Current overrun MoveNext count");
            Equal(1, input.CurrentAccesses, "Current overrun Current count");
            AssertSeedUnchanged(project, "Current overrun");
        }

        private static void KnownCountOverrunWinsBeforeNullKeyValidation()
        {
            var project = NewProject("null-key");
            project.Metadata.Add("seed", "original");
            var input = new CountedMetadataCollection(
                1,
                new KeyValuePair<string, string>("first", "v1"),
                new KeyValuePair<string, string>(null!, "unexpected"));

            ExpectCountOverrun(project, input, "null-key overrun");
            Equal(2, input.MoveNextCalls, "null-key overrun MoveNext count");
            Equal(1, input.CurrentAccesses, "null-key overrun Current count");
            AssertSeedUnchanged(project, "null-key overrun");
        }

        private static void KnownCountOverrunWinsBeforeDuplicateKeyValidation()
        {
            var project = NewProject("duplicate-key");
            project.Metadata.Add("seed", "original");
            var input = new CountedMetadataCollection(
                1,
                new KeyValuePair<string, string>("duplicate", "v1"),
                new KeyValuePair<string, string>("duplicate", "unexpected"));

            ExpectCountOverrun(project, input, "duplicate-key overrun");
            Equal(2, input.MoveNextCalls, "duplicate-key overrun MoveNext count");
            Equal(1, input.CurrentAccesses, "duplicate-key overrun Current count");
            AssertSeedUnchanged(project, "duplicate-key overrun");
        }

        private static void StableCountedInputRemainsAccepted()
        {
            var project = NewProject("stable");
            var input = new CountedMetadataCollection(
                2,
                new KeyValuePair<string, string>("first", "v1"),
                new KeyValuePair<string, string>("second", "v2"));

            InvokePersistenceReplacement(project, input);
            Equal(3, input.MoveNextCalls, "stable MoveNext count");
            Equal(2, input.CurrentAccesses, "stable Current count");
            Equal(2, project.Metadata.Count, "stable metadata count");
            Equal("v1", project.Metadata["first"], "stable first value");
            Equal("v2", project.Metadata["second"], "stable second value");
        }

        private static ProjectState NewProject(string suffix)
        {
            return new ProjectState("metadata-known-count-overrun-" + suffix, "Metadata Known Count Overrun " + suffix);
        }

        private static void ExpectCountOverrun(
            ProjectState project,
            IEnumerable<KeyValuePair<string, string>> input,
            string label)
        {
            try
            {
                InvokePersistenceReplacement(project, input);
                throw new InvalidOperationException(label + ": expected known-Count overrun rejection.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException failure)
            {
                const string expected = "Project metadata persistence input Count does not match traversal (expected 1, observed 2).";
                if (!string.Equals(expected, failure.Message, StringComparison.Ordinal))
                    throw new InvalidOperationException(label + ": wrong failure precedence. Expected '" + expected + "', got '" + failure.Message + "'.");
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

        private static void AssertSeedUnchanged(ProjectState project, string label)
        {
            Equal(1, project.Metadata.Count, label + " atomic metadata replacement count");
            Equal("original", project.Metadata["seed"], label + " atomic metadata replacement value");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private sealed class CountedMetadataCollection : ICollection<KeyValuePair<string, string>>, IReadOnlyCollection<KeyValuePair<string, string>>, ICollection
        {
            private readonly IReadOnlyList<KeyValuePair<string, string>> _items;
            private readonly int _count;

            internal CountedMetadataCollection(int count, params KeyValuePair<string, string>[] items)
            {
                _count = count;
                _items = items;
            }

            public int Count => _count;
            int IReadOnlyCollection<KeyValuePair<string, string>>.Count => _count;
            int ICollection.Count => _count;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentAccesses { get; private set; }
            internal bool ThrowOnUnexpectedCurrent { get; set; }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<KeyValuePair<string, string>>
            {
                private readonly CountedMetadataCollection _owner;
                private int _index = -1;

                internal Enumerator(CountedMetadataCollection owner)
                {
                    _owner = owner;
                }

                public KeyValuePair<string, string> Current
                {
                    get
                    {
                        _owner.CurrentAccesses++;
                        if (_owner.ThrowOnUnexpectedCurrent && _index >= _owner._count)
                            throw new InvalidOperationException("Unexpected Current was observed past the admitted Count.");
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
    }
}
