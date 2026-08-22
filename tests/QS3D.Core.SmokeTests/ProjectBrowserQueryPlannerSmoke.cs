using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserQueryPlannerSmoke
    {
        public static void Run()
        {
            SearchMatchesSemanticCatalogNames();
            DirtyAndCategoryFiltersCompose();
            FloorAndZoneFiltersCompose();
            EmptySearchReturnsWholeTree();
            MissingFamilyReferenceFailsClosed();
            FamilyCategoryMismatchFailsClosed();
            UnfilteredMissingFamilyReferenceFailsClosed();
            UnfilteredFamilyCategoryMismatchFailsClosed();
            FilteredPathStillValidatesUnmatchedReferences();
            InvalidFilterReferenceFailsClosed();
            OversizedKnownCountFailsBeforeEnumeration();
            ExactKnownCountLimitIsAccepted();
            DishonestLowKnownCountStillHitsStreamingLimit();
        }

        private static void SearchMatchesSemanticCatalogNames()
        {
            var project = BuildProject();
            var byFamily = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.FloorThenCategory,
                new ProjectBrowserQueryOptions("Concrete Beam"));
            Equal(2, byFamily.MatchedCount);

            var byFloor = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.FloorThenCategory,
                new ProjectBrowserQueryOptions("Level 02"));
            Equal(3, byFloor.MatchedCount);

            var byZone = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.ZoneThenCategory,
                new ProjectBrowserQueryOptions("East Wing"));
            Equal(3, byZone.MatchedCount);

            var byId = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.Category,
                new ProjectBrowserQueryOptions("B-002"));
            Equal(1, byId.MatchedCount);
            Equal("B-002", byId.Root.ElementIds[0]);
        }

        private static void DirtyAndCategoryFiltersCompose()
        {
            var project = BuildProject();
            project.Elements[0].MarkClean(ElementDirtyFlags.All);
            var result = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.Category,
                new ProjectBrowserQueryOptions(
                    dirtyOnly: true,
                    categories: new[] { ElementCategory.Beam }));
            Equal(1, result.MatchedCount);
            Equal("B-001", result.Root.ElementIds[0]);
        }

        private static void FloorAndZoneFiltersCompose()
        {
            var project = BuildProject();
            var result = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.FloorThenCategory,
                new ProjectBrowserQueryOptions(
                    floorIds: new[] { "F-02" },
                    zoneIds: new[] { "Z-EAST" }));
            Equal(3, result.MatchedCount);
            Equal(1, result.Root.Children.Count);
            Equal("Level 02", result.Root.Children[0].DisplayName);
        }

        private static void EmptySearchReturnsWholeTree()
        {
            var project = BuildProject();
            var result = ProjectBrowserQueryPlanner.Build(
                project,
                ProjectBrowserGrouping.Category,
                new ProjectBrowserQueryOptions("   "));
            Equal(false, result.IsFiltered);
            Equal(project.Elements.Count, result.MatchedCount);
            Equal(project.Elements.Count, result.TotalCount);
        }

        private static void MissingFamilyReferenceFailsClosed()
        {
            var project = BuildProject();
            project.Elements.Add(new ProjectElement("BAD-1", ElementCategory.Beam, "FAM-404", "F-02", "Z-EAST"));
            MustFail(
                () => ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.Category, new ProjectBrowserQueryOptions("beam")),
                "Search must not silently hide an element with a missing family reference.");
        }

        private static void FamilyCategoryMismatchFailsClosed()
        {
            var project = BuildProject();
            var bad = new ProjectElement("BAD-FAMILY-CATEGORY", ElementCategory.Beam, "FAM-C", "F-02", "Z-EAST");
            bad.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(bad);
            MustFail(
                () => ProjectBrowserQueryPlanner.Build(
                    project,
                    ProjectBrowserGrouping.Category,
                    new ProjectBrowserQueryOptions(dirtyOnly: true, categories: new[] { ElementCategory.Column })),
                "Filtered browser query must reject Family/category corruption even when the corrupt element would not match the filter.");
        }

        private static void UnfilteredMissingFamilyReferenceFailsClosed()
        {
            var project = BuildProject();
            project.Elements.Add(new ProjectElement("BAD-UNFILTERED-FAMILY", ElementCategory.Beam, "FAM-404", "F-02", "Z-EAST"));
            MustFail(
                () => ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.Category),
                "Unfiltered browser query must reject a missing Family reference instead of bypassing query integrity preflight.");
        }

        private static void UnfilteredFamilyCategoryMismatchFailsClosed()
        {
            var project = BuildProject();
            project.Elements.Add(new ProjectElement("BAD-UNFILTERED-CATEGORY", ElementCategory.Beam, "FAM-C", "F-02", "Z-EAST"));
            MustFail(
                () => ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.Category, new ProjectBrowserQueryOptions("   ")),
                "Unfiltered browser query must reject a Family/category mismatch instead of bypassing query integrity preflight.");
        }

        private static void FilteredPathStillValidatesUnmatchedReferences()
        {
            var project = BuildProject();
            var bad = new ProjectElement("BAD-REF", ElementCategory.Column, "FAM-C", "F-404", "Z-EAST");
            bad.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(bad);
            MustFail(
                () => ProjectBrowserQueryPlanner.Build(
                    project,
                    ProjectBrowserGrouping.Category,
                    new ProjectBrowserQueryOptions(dirtyOnly: true, categories: new[] { ElementCategory.Beam })),
                "Filtered browser path must validate corrupt references even when the corrupt element would not match the filter.");
        }

        private static void InvalidFilterReferenceFailsClosed()
        {
            var project = BuildProject();
            MustFail(
                () => ProjectBrowserQueryPlanner.Build(
                    project,
                    ProjectBrowserGrouping.Category,
                    new ProjectBrowserQueryOptions(floorIds: new[] { "F-404" })),
                "Unknown explicit browser filter IDs must fail closed.");
        }

        private static void OversizedKnownCountFailsBeforeEnumeration()
        {
            var source = new CountedEnumerable<ElementCategory>(10001, 0, ElementCategory.Beam);
            MustFail(
                () => new ProjectBrowserQueryOptions(categories: source),
                "A query filter whose known Count exceeds 10,000 must fail closed.");
            Equal(0, source.GetEnumeratorCalls);
            Equal(0, source.MoveNextCalls);
        }

        private static void ExactKnownCountLimitIsAccepted()
        {
            var source = new CountedEnumerable<ElementCategory>(10000, 10000, ElementCategory.Beam);
            var options = new ProjectBrowserQueryOptions(categories: source);
            Equal(10000, options.Categories.Count);
            Equal(1, source.GetEnumeratorCalls);
            Equal(10001, source.MoveNextCalls);
        }

        private static void DishonestLowKnownCountStillHitsStreamingLimit()
        {
            var source = new CountedEnumerable<ElementCategory>(1, 10001, ElementCategory.Beam);
            MustFail(
                () => new ProjectBrowserQueryOptions(categories: source),
                "A dishonest low Count must not bypass the streaming 10,000-value ceiling.");
            Equal(1, source.GetEnumeratorCalls);
            Equal(10001, source.MoveNextCalls);
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-BROWSER-QUERY", "Browser Query Smoke");
            project.Floors.Add(new FloorDefinition("F-01", "Level 01", 0d));
            project.Floors.Add(new FloorDefinition("F-02", "Level 02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-EAST", "East Wing"));
            project.Zones.Add(new ZoneDefinition("Z-WEST", "West Wing"));
            project.Families.Add(new ProjectFamily("FAM-B", "Concrete Beam 300x500", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("FAM-C", "Concrete Column 400x400", ElementCategory.Column));

            project.Elements.Add(new ProjectElement("B-002", ElementCategory.Beam, "FAM-B", "F-02", "Z-EAST"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, "FAM-B", "F-02", "Z-EAST"));
            project.Elements.Add(new ProjectElement("C-001", ElementCategory.Column, "FAM-C", "F-02", "Z-EAST"));
            project.Elements.Add(new ProjectElement("C-000", ElementCategory.Column, "FAM-C", "F-01", "Z-WEST"));
            return project;
        }

        private static void MustFail(Action action, string message)
        {
            var failed = false;
            try { action(); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed) throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }

        private sealed class CountedEnumerable<T> : ICollection<T>
        {
            private readonly int _yieldCount;
            private readonly T _value;

            internal CountedEnumerable(int reportedCount, int yieldCount, T value)
            {
                Count = reportedCount;
                _yieldCount = yieldCount;
                _value = value;
            }

            public int Count { get; }
            public bool IsReadOnly => true;
            internal int GetEnumeratorCalls { get; private set; }
            internal int MoveNextCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return new CountingEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();

            private sealed class CountingEnumerator : IEnumerator<T>
            {
                private readonly CountedEnumerable<T> _owner;
                private int _index = -1;

                internal CountingEnumerator(CountedEnumerable<T> owner) => _owner = owner;
                public T Current => _owner._value;
                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._yieldCount;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}