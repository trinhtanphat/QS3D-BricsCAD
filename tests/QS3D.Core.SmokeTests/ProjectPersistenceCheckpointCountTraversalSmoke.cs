using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceCheckpointCountTraversalSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsUnderEnumerationAgainstKnownCount();
            RejectsOverEnumerationAgainstKnownCount();
            AcceptsHonestKnownCount();
            AcceptsPureStreamingSource();
        }

        private static void RejectsUnderEnumerationAgainstKnownCount()
        {
            var project = BuildProject("P-CHECKPOINT-COUNT-UNDER", 2);
            var source = new CountedEnumerable(2, new[] { "E1" });

            var error = ThrowsMessage<InvalidOperationException>(() =>
                ProjectPersistenceCheckpoint.Capture(project, source));

            Contains("known element count does not match enumerated element count", error,
                "Checkpoint did not reject Count=2 with one enumerated element.");
            Equal(1, source.CountReads, "Known Count was not snapshotted exactly once for under-enumeration.");
            Equal(1, source.EnumerationCount, "Under-enumerating source was not traversed exactly once.");
        }

        private static void RejectsOverEnumerationAgainstKnownCount()
        {
            var project = BuildProject("P-CHECKPOINT-COUNT-OVER", 2);
            var source = new CountedEnumerable(1, new[] { "E1", "E2" });

            var error = ThrowsMessage<InvalidOperationException>(() =>
                ProjectPersistenceCheckpoint.Capture(project, source));

            Contains("known element count does not match enumerated element count", error,
                "Checkpoint did not reject Count=1 with two enumerated elements.");
            Equal(1, source.CountReads, "Known Count was not snapshotted exactly once for over-enumeration.");
            Equal(1, source.EnumerationCount, "Over-enumerating source was not traversed exactly once.");
        }

        private static void AcceptsHonestKnownCount()
        {
            var project = BuildProject("P-CHECKPOINT-COUNT-HONEST", 2);
            var source = new CountedEnumerable(2, new[] { "e1", "e2" });

            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, source);

            Equal(2, checkpoint.ElementIds.Count, "Honest Count source did not capture both elements.");
            Equal(2, source.CountReads, "Honest known Count was not observed before and after traversal.");
            Equal(1, source.EnumerationCount, "Honest Count source was not traversed exactly once.");
        }

        private static void AcceptsPureStreamingSource()
        {
            var project = BuildProject("P-CHECKPOINT-COUNT-STREAM", 2);

            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, Stream("E1", "E2"));

            Equal(2, checkpoint.ElementIds.Count, "Pure IEnumerable checkpoint source was rejected.");
        }

        private static ProjectState BuildProject(string id, int elementCount)
        {
            var project = new ProjectState(id, "Checkpoint Count traversal fixture");
            for (var index = 1; index <= elementCount; index++)
                project.Elements.Add(new ProjectElement("E" + index, ElementCategory.GlassWall));
            project.Touch();
            return project;
        }

        private static IEnumerable<string> Stream(params string[] values)
        {
            foreach (var value in values)
                yield return value;
        }

        private static string ThrowsMessage<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error.Message;
            }

            throw new Exception("Expected " + typeof(TException).Name + " was not thrown.");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if ((actual ?? string.Empty).IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new Exception(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountedEnumerable : IReadOnlyCollection<string>
        {
            private readonly int _reportedCount;
            private readonly IReadOnlyList<string> _items;

            public CountedEnumerable(int reportedCount, IReadOnlyList<string> items)
            {
                _reportedCount = reportedCount;
                _items = items;
            }

            public int CountReads { get; private set; }
            public int EnumerationCount { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _reportedCount;
                }
            }

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationCount++;
                for (var index = 0; index < _items.Count; index++)
                    yield return _items[index];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
