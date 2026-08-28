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
            KnownCountOverrunWinsBeforeNullKeyValidation();
            KnownCountOverrunWinsBeforeDuplicateKeyValidation();
        }

        private static void KnownCountOverrunWinsBeforeNullKeyValidation()
        {
            var project = NewProject("null-key");
            project.Metadata.Add("seed", "original");
            var input = new UnderreportedMetadataCollection(
                new KeyValuePair<string, string>("first", "v1"),
                new KeyValuePair<string, string>(null!, "unexpected"));

            ExpectCountOverrun(project, input, "null-key overrun");
            Equal(2, input.YieldedCount, "null-key overrun yielded count");
            AssertSeedUnchanged(project, "null-key overrun");
        }

        private static void KnownCountOverrunWinsBeforeDuplicateKeyValidation()
        {
            var project = NewProject("duplicate-key");
            project.Metadata.Add("seed", "original");
            var input = new UnderreportedMetadataCollection(
                new KeyValuePair<string, string>("duplicate", "v1"),
                new KeyValuePair<string, string>("duplicate", "unexpected"));

            ExpectCountOverrun(project, input, "duplicate-key overrun");
            Equal(2, input.YieldedCount, "duplicate-key overrun yielded count");
            AssertSeedUnchanged(project, "duplicate-key overrun");
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

        private sealed class UnderreportedMetadataCollection : ICollection<KeyValuePair<string, string>>
        {
            private readonly KeyValuePair<string, string> _first;
            private readonly KeyValuePair<string, string> _unexpected;

            internal UnderreportedMetadataCollection(
                KeyValuePair<string, string> first,
                KeyValuePair<string, string> unexpected)
            {
                _first = first;
                _unexpected = unexpected;
            }

            public int Count => 1;
            public bool IsReadOnly => true;
            public int YieldedCount { get; private set; }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                YieldedCount++;
                yield return _first;
                YieldedCount++;
                yield return _unexpected;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
        }
    }
}
