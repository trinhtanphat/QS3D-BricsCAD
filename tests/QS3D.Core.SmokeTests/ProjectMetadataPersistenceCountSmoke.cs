using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMetadataPersistenceCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GenericCountDriftFailsAtomically();
            ReadOnlyCountDriftFailsAtomically();
            NonGenericCountDriftFailsAtomically();
            StableCountedInputPublishes();
            PureStreamingInputPublishes();
        }

        private static void GenericCountDriftFailsAtomically()
        {
            var project = SeededProject("generic");
            var input = new GenericDriftCollection(1, 2, Pair("new-generic", "value"));
            ExpectDriftFailure(project, input, "generic Count drift");
            Equal(1, input.YieldedCount, "generic Count drift yielded count");
            AssertSeedUnchanged(project, "generic Count drift");
        }

        private static void ReadOnlyCountDriftFailsAtomically()
        {
            var project = SeededProject("readonly");
            var input = new ReadOnlyDriftCollection(1, 0, Pair("new-readonly", "value"));
            ExpectDriftFailure(project, input, "read-only Count drift");
            Equal(1, input.YieldedCount, "read-only Count drift yielded count");
            AssertSeedUnchanged(project, "read-only Count drift");
        }

        private static void NonGenericCountDriftFailsAtomically()
        {
            var project = SeededProject("nongeneric");
            var input = new NonGenericDriftCollection(1, 3, Pair("new-nongeneric", "value"));
            ExpectDriftFailure(project, input, "non-generic Count drift");
            Equal(1, input.YieldedCount, "non-generic Count drift yielded count");
            AssertSeedUnchanged(project, "non-generic Count drift");
        }

        private static void StableCountedInputPublishes()
        {
            var project = SeededProject("stable");
            var input = new GenericDriftCollection(1, 1, Pair("stable", "published"));
            InvokePersistenceReplacement(project, input);
            Equal(1, input.YieldedCount, "stable counted input yielded count");
            Equal(1, project.Metadata.Count, "stable counted input metadata count");
            Equal("published", project.Metadata["stable"], "stable counted input value");
        }

        private static void PureStreamingInputPublishes()
        {
            var project = SeededProject("streaming");
            InvokePersistenceReplacement(project, Streaming(Pair("stream-a", "a"), Pair("stream-b", "b")));
            Equal(2, project.Metadata.Count, "streaming metadata count");
            Equal("a", project.Metadata["stream-a"], "streaming first value");
            Equal("b", project.Metadata["stream-b"], "streaming second value");
        }

        private static IEnumerable<KeyValuePair<string, string>> Streaming(params KeyValuePair<string, string>[] items)
        {
            for (var i = 0; i < items.Length; i++)
                yield return items[i];
        }

        private static void ExpectDriftFailure(
            ProjectState project,
            IEnumerable<KeyValuePair<string, string>> input,
            string label)
        {
            try
            {
                InvokePersistenceReplacement(project, input);
                throw new InvalidOperationException(label + ": expected post-traversal Count drift rejection.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException failure)
            {
                const string expected = "Project metadata persistence input Count changed during traversal.";
                if (!string.Equals(expected, failure.Message, StringComparison.Ordinal))
                    throw new InvalidOperationException(label + ": wrong failure. Expected '" + expected + "', got '" + failure.Message + "'.");
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
            var project = new ProjectState("metadata-count-stability-" + suffix, "Metadata Count Stability " + suffix);
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

        private sealed class GenericDriftCollection : ICollection<KeyValuePair<string, string>>
        {
            private readonly int _initialCount;
            private readonly int _finalCount;
            private readonly KeyValuePair<string, string> _item;
            private int _countReads;

            internal GenericDriftCollection(int initialCount, int finalCount, KeyValuePair<string, string> item)
            {
                _initialCount = initialCount;
                _finalCount = finalCount;
                _item = item;
            }

            public int Count => _countReads++ == 0 ? _initialCount : _finalCount;
            public bool IsReadOnly => true;
            public int YieldedCount { get; private set; }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                YieldedCount++;
                yield return _item;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyDriftCollection : IReadOnlyCollection<KeyValuePair<string, string>>
        {
            private readonly int _initialCount;
            private readonly int _finalCount;
            private readonly KeyValuePair<string, string> _item;
            private int _countReads;

            internal ReadOnlyDriftCollection(int initialCount, int finalCount, KeyValuePair<string, string> item)
            {
                _initialCount = initialCount;
                _finalCount = finalCount;
                _item = item;
            }

            public int Count => _countReads++ == 0 ? _initialCount : _finalCount;
            public int YieldedCount { get; private set; }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                YieldedCount++;
                yield return _item;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericDriftCollection : IEnumerable<KeyValuePair<string, string>>, ICollection
        {
            private readonly int _initialCount;
            private readonly int _finalCount;
            private readonly KeyValuePair<string, string> _item;
            private int _countReads;

            internal NonGenericDriftCollection(int initialCount, int finalCount, KeyValuePair<string, string> item)
            {
                _initialCount = initialCount;
                _finalCount = finalCount;
                _item = item;
            }

            public int Count => _countReads++ == 0 ? _initialCount : _finalCount;
            public object SyncRoot => this;
            public bool IsSynchronized => false;
            public int YieldedCount { get; private set; }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                YieldedCount++;
                yield return _item;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}
