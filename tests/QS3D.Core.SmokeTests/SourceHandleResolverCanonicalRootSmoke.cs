using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleResolverCanonicalRootSmoke
    {
        private const int MaxRootCount = 10000;

        public static void Run()
        {
            CanonicalAndBlankRootsRemainCompatible();
            PaddedRootsFailBeforeSemanticTraversal();
            KnownCountBoundFailsBeforeEnumeration();
            StreamingBoundStopsAtEntry10001();
            ExactStreamingBoundaryRemainsAccepted();
        }

        private static ProjectState CreateProject(bool poisonTraversal = false)
        {
            var project = new ProjectState("P1", "Locate canonical root identity");
            var element = new ProjectElement("element-1", ElementCategory.ArchitecturalWall);
            element.SourceHandles.Add("AB12");
            if (poisonTraversal)
                element.DependsOn.Add("   ");
            project.Elements.Add(element);
            return project;
        }

        private static void CanonicalAndBlankRootsRemainCompatible()
        {
            var project = CreateProject();
            var inputVersion = project.ChangeVersion;

            var canonical = SourceHandleResolver.Resolve(project, new[] { "element-1" });
            if (canonical.Count != 1 || !string.Equals(canonical[0], "AB12", StringComparison.Ordinal))
                throw new Exception("Canonical Locate root id must continue resolving its source handle.");

            var canonicalDifferentCase = SourceHandleResolver.Resolve(project, new[] { "ELEMENT-1" });
            if (canonicalDifferentCase.Count != 1 || !string.Equals(canonicalDifferentCase[0], "AB12", StringComparison.Ordinal))
                throw new Exception("Canonical Locate root lookup must retain case-insensitive semantic identity matching.");

            var blank = SourceHandleResolver.Resolve(project, new string[] { null, string.Empty, "   ", "\t" });
            if (blank.Count != 0)
                throw new Exception("Blank Locate root ids must retain existing skip semantics.");
            if (project.ChangeVersion != inputVersion)
                throw new Exception("Locate root materialization must remain read-only for canonical/blank inputs.");
        }

        private static void PaddedRootsFailBeforeSemanticTraversal()
        {
            foreach (var padded in new[] { " element-1", "element-1 ", " element-1 ", "\telement-1", "element-1\t" })
            {
                var project = CreateProject(poisonTraversal: true);
                var inputVersion = project.ChangeVersion;
                ExpectInvalidOperation(
                    () => SourceHandleResolver.Resolve(project, new[] { padded }),
                    "non-canonical semantic element id");
                if (project.ChangeVersion != inputVersion)
                    throw new Exception("Rejected padded Locate roots must not mutate project state.");
            }
        }

        private static void KnownCountBoundFailsBeforeEnumeration()
        {
            var project = CreateProject();
            var source = new OversizedThrowingCollection(MaxRootCount + 1);
            ExpectInvalidOperation(
                () => SourceHandleResolver.Resolve(project, source),
                "cannot exceed 10000 input entries");
            if (source.GetEnumeratorCalls != 0)
                throw new Exception("Known oversized Locate root collection must be rejected before enumeration.");
        }

        private static void StreamingBoundStopsAtEntry10001()
        {
            var project = CreateProject();
            var source = new CountingEnumerable(MaxRootCount + 2, "element-1");
            ExpectInvalidOperation(
                () => SourceHandleResolver.Resolve(project, source),
                "cannot exceed 10000 input entries");
            if (source.MoveNextYieldCount != MaxRootCount + 1)
                throw new Exception("Streaming Locate root bound must stop after observing entry 10001, not consume later entries.");
        }

        private static void ExactStreamingBoundaryRemainsAccepted()
        {
            var project = CreateProject();
            var source = new CountingEnumerable(MaxRootCount, "element-1");
            var handles = SourceHandleResolver.Resolve(project, source);
            if (source.MoveNextYieldCount != MaxRootCount)
                throw new Exception("Exact-bound Locate root input must be fully consumed.");
            if (handles.Count != 1 || !string.Equals(handles[0], "AB12", StringComparison.Ordinal))
                throw new Exception("Exact-bound duplicate canonical roots must resolve deterministically without duplicate handles.");
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment)
        {
            try
            {
                action();
                throw new Exception("Expected Locate validation to fail closed.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Locate validation failed with an unexpected diagnostic: " + ex.Message);
            }
        }

        private sealed class OversizedThrowingCollection : ICollection<string>
        {
            public OversizedThrowingCollection(int count) { Count = count; }
            public int Count { get; }
            public bool IsReadOnly => true;
            public int GetEnumeratorCalls { get; private set; }
            public IEnumerator<string> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new Exception("Oversized known-count source must not be enumerated.");
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(string item) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        private sealed class CountingEnumerable : IEnumerable<string>
        {
            private readonly int _count;
            private readonly string _value;

            public CountingEnumerable(int count, string value)
            {
                _count = count;
                _value = value;
            }

            public int MoveNextYieldCount { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                for (var index = 0; index < _count; index++)
                {
                    MoveNextYieldCount++;
                    yield return _value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
