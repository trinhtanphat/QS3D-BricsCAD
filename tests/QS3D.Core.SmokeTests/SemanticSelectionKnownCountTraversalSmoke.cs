using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Selection;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionKnownCountTraversalSmoke
    {
        internal static void Run()
        {
            ConflictingKnownCountsFailBeforeEnumeration();
            UnderEnumerationFailsClosed();
            OverEnumerationFailsClosed();
            MatchingKnownCountIsAccepted();
            PureStreamingSelectionIsAccepted();
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var project = BuildProject();
            var version = project.ChangeVersion;
            var source = new ConflictingKnownCountSource(1, 2);

            ExpectInvalidOperation(
                () => SemanticSelectionInspector.Inspect(project, source),
                "conflicting known counts");

            Equal(false, source.EnumeratorRequested);
            Equal(version, project.ChangeVersion);
        }

        private static void UnderEnumerationFailsClosed()
        {
            var project = BuildProject();
            var version = project.ChangeVersion;
            var source = new CountedSelectionSource(2, "B-001");

            ExpectInvalidOperation(
                () => SemanticSelectionInspector.Inspect(project, source),
                "known count does not match traversal");

            Equal(version, project.ChangeVersion);
        }

        private static void OverEnumerationFailsClosed()
        {
            var project = BuildProject();
            var version = project.ChangeVersion;
            var source = new CountedSelectionSource(1, "B-001", "B-002");

            ExpectInvalidOperation(
                () => SemanticSelectionInspector.Inspect(project, source),
                "known count does not match traversal");

            Equal(version, project.ChangeVersion);
        }

        private static void MatchingKnownCountIsAccepted()
        {
            var project = BuildProject();
            var version = project.ChangeVersion;
            var result = SemanticSelectionInspector.Inspect(
                project,
                new CountedSelectionSource(2, "B-002", "B-001"));

            Equal(2, result.Count);
            Equal("B-001", result.ElementIds[0]);
            Equal("B-002", result.ElementIds[1]);
            Equal(version, project.ChangeVersion);
        }

        private static void PureStreamingSelectionIsAccepted()
        {
            var project = BuildProject();
            var version = project.ChangeVersion;
            var result = SemanticSelectionInspector.Inspect(project, Stream("B-002", "B-001"));

            Equal(2, result.Count);
            Equal("B-001", result.ElementIds[0]);
            Equal("B-002", result.ElementIds[1]);
            Equal(version, project.ChangeVersion);
        }

        private static IEnumerable<string> Stream(params string[] ids)
        {
            foreach (var id in ids) yield return id;
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-SEL-COUNT", "Selection Count Smoke");
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("B-002", ElementCategory.Beam));
            return project;
        }

        private static void ExpectInvalidOperation(Action action, string messageFragment)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(messageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Unexpected semantic selection Count rejection: " + ex.Message);
                return;
            }

            throw new Exception("Expected semantic selection Count integrity failure containing: " + messageFragment);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }

        private sealed class CountedSelectionSource : ICollection<string>
        {
            private readonly string[] _items;

            internal CountedSelectionSource(int count, params string[] items)
            {
                Count = count;
                _items = items ?? Array.Empty<string>();
            }

            public int Count { get; }
            public bool IsReadOnly => true;

            public IEnumerator<string> GetEnumerator()
            {
                foreach (var item in _items) yield return item;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class ConflictingKnownCountSource : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;

            internal ConflictingKnownCountSource(int genericCount, int readOnlyCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
            }

            int ICollection<string>.Count => _genericCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount;
            public bool IsReadOnly => true;
            internal bool EnumeratorRequested { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Conflicting known Counts must fail before semantic selection enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }
    }
}
