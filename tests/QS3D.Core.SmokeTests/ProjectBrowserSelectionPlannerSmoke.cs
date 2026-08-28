using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserSelectionPlannerSmoke
    {
        public static void Run()
        {
            SingleSelectionRevealsAncestors();
            CaseInsensitiveSelectionIdentityReveals();
            MultiSelectionUnionsExpansionPaths();
            InvalidSemanticSelectionFailsClosed();
            KnownCountContractFailsClosedBeforeEnumeration();
            KnownCountTraversalMismatchFailsClosed();
            HonestCountedAndStreamingSelectionsRemainSupported();
            PureStreamingSelectionRemainsBounded();
            NodeSelectionUsesDeterministicPaging();
            ResultCollectionsAreImmutable();
        }

        private static void SingleSelectionRevealsAncestors()
        {
            var root = BuildRoot();
            var plan = ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "B-002" });

            Equal(1, plan.SelectedElementIds.Count);
            Equal("B-002", plan.SelectedElementIds[0]);
            Equal("B-002", plan.PrimaryElementId);
            Equal(1, plan.TargetNodePaths.Count);
            Equal(2, plan.ExpansionPaths.Count);
            True(plan.ExpansionPaths[0] == ProjectBrowserVirtualizationPlanner.GetRootPath(root));
            True(plan.TargetNodePaths[0].EndsWith("/category%3ABeam", StringComparison.Ordinal));
            True(!plan.IsMultiSelection);
        }

        private static void CaseInsensitiveSelectionIdentityReveals()
        {
            var root = BuildRoot();
            True(root.ElementIds.Contains("B-001"));
            True(!root.ElementIds.Contains("b-001"));
            var plan = ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "b-001" }, "B-001");

            Equal(1, plan.SelectedElementIds.Count);
            Equal("b-001", plan.SelectedElementIds[0]);
            Equal("b-001", plan.PrimaryElementId);
            Equal(1, plan.TargetNodePaths.Count);
            Equal(2, plan.ExpansionPaths.Count);
            True(plan.TargetNodePaths[0].EndsWith("/category%3ABeam", StringComparison.Ordinal));
        }

        private static void MultiSelectionUnionsExpansionPaths()
        {
            var root = BuildRoot();
            var plan = ProjectBrowserSelectionPlanner.PlanReveal(
                root,
                new[] { "W-001", "C-001" },
                "W-001");

            Equal(2, plan.SelectedElementIds.Count);
            Equal("W-001", plan.PrimaryElementId);
            Equal(2, plan.TargetNodePaths.Count);
            Equal(3, plan.ExpansionPaths.Count);
            Equal(ProjectBrowserVirtualizationPlanner.GetRootPath(root), plan.ExpansionPaths[0]);
            True(plan.ExpansionPaths.Any(x => x.EndsWith("/floor%3AF-01", StringComparison.Ordinal)));
            True(plan.ExpansionPaths.Any(x => x.EndsWith("/floor%3AF-02", StringComparison.Ordinal)));
            True(plan.IsMultiSelection);
        }

        private static void InvalidSemanticSelectionFailsClosed()
        {
            var root = BuildRoot();
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "B-001", "b-001" }));
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "MISSING" }));
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "B-001" }, "C-001"));
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { " B-001" }));
        }

        private static void KnownCountContractFailsClosedBeforeEnumeration()
        {
            var root = BuildRoot();

            var negative = new CountContractEnumerable(-1, -1, -1, new[] { "B-001" });
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, negative));
            True(!negative.Enumerated);

            var oversized = new CountContractEnumerable(10001, 10001, 10001, new[] { "B-001" });
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, oversized));
            True(!oversized.Enumerated);

            var conflicting = new CountContractEnumerable(1, 2, 1, new[] { "B-001" });
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, conflicting));
            True(!conflicting.Enumerated);
        }

        private static void KnownCountTraversalMismatchFailsClosed()
        {
            var root = BuildRoot();

            var shortTraversal = new CountContractEnumerable(2, 2, 2, new[] { "B-001" });
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, shortTraversal));
            True(shortTraversal.Enumerated);

            var longTraversal = new CountContractEnumerable(1, 1, 1, new[] { "B-001", "B-002" });
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(root, longTraversal));
            True(longTraversal.Enumerated);
        }

        private static void HonestCountedAndStreamingSelectionsRemainSupported()
        {
            var root = BuildRoot();
            var counted = new CountContractEnumerable(2, 2, 2, new[] { "B-002", "B-001" });
            var countedPlan = ProjectBrowserSelectionPlanner.PlanReveal(root, counted, "B-002");
            True(counted.Enumerated);
            Equal(2, countedPlan.SelectedElementIds.Count);
            Equal("B-001", countedPlan.SelectedElementIds[0]);
            Equal("B-002", countedPlan.SelectedElementIds[1]);
            Equal("B-002", countedPlan.PrimaryElementId);

            var streamingPlan = ProjectBrowserSelectionPlanner.PlanReveal(root, Stream("C-001"));
            Equal(1, streamingPlan.SelectedElementIds.Count);
            Equal("C-001", streamingPlan.SelectedElementIds[0]);
        }

        private static void PureStreamingSelectionRemainsBounded()
        {
            Throws<InvalidOperationException>(() => ProjectBrowserSelectionPlanner.PlanReveal(BuildRoot(), StreamMany(10001)));
        }

        private static void NodeSelectionUsesDeterministicPaging()
        {
            var root = BuildRoot();
            var reveal = ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "B-001" });
            var beamPath = reveal.TargetNodePaths.Single();

            var first = ProjectBrowserSelectionPlanner.PlanNodeSelection(root, beamPath, 0, 1);
            Equal(2, first.TotalCount);
            Equal(1, first.ElementIds.Count);
            Equal("B-001", first.ElementIds[0]);
            Equal("B-001", first.PrimaryElementId);
            True(!first.HasPrevious);
            True(first.HasNext);

            var second = ProjectBrowserSelectionPlanner.PlanNodeSelection(root, beamPath, 1, 1);
            Equal("B-002", second.ElementIds[0]);
            True(second.HasPrevious);
            True(!second.HasNext);
        }

        private static void ResultCollectionsAreImmutable()
        {
            var root = BuildRoot();
            var plan = ProjectBrowserSelectionPlanner.PlanReveal(root, new[] { "B-001", "C-001" });
            Throws<NotSupportedException>(() => ((IList<string>)plan.SelectedElementIds).Clear());
            Throws<NotSupportedException>(() => ((IList<string>)plan.ExpansionPaths).Clear());
            Throws<NotSupportedException>(() => ((IList<string>)plan.TargetNodePaths).Clear());

            var page = ProjectBrowserSelectionPlanner.PlanNodeSelection(root, plan.TargetNodePaths[0], 0, 1);
            Throws<NotSupportedException>(() => ((IList<string>)page.ElementIds).Clear());
        }

        private static IEnumerable<string> Stream(params string[] values)
        {
            foreach (var value in values) yield return value;
        }

        private static IEnumerable<string> StreamMany(int count)
        {
            for (var i = 0; i < count; i++)
                yield return "STREAM-" + i.ToString("D5");
        }

        private static ProjectBrowserNode BuildRoot()
        {
            var project = new ProjectState("P-SELECTION", "Selection Browser");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Elements.Add(new ProjectElement("B-002", ElementCategory.Beam, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("C-001", ElementCategory.Column, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("W-001", ElementCategory.ArchitecturalWall, string.Empty, "F-01", "Z-A"));
            return ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
        }

        private sealed class CountContractEnumerable : ICollection<string>, IReadOnlyCollection<string>, System.Collections.ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly IReadOnlyList<string> _values;

            internal CountContractEnumerable(int genericCount, int readOnlyCount, int nonGenericCount, IEnumerable<string> values)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _values = values.ToList().AsReadOnly();
            }

            internal bool Enumerated { get; private set; }

            int ICollection<string>.Count => _genericCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount;
            int System.Collections.ICollection.Count => _nonGenericCount;
            bool ICollection<string>.IsReadOnly => true;
            bool System.Collections.ICollection.IsSynchronized => false;
            object System.Collections.ICollection.SyncRoot => this;

            IEnumerator<string> IEnumerable<string>.GetEnumerator()
            {
                Enumerated = true;
                return _values.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<string>)this).GetEnumerator();
            }

            bool ICollection<string>.Contains(string item) => _values.Contains(item);
            void ICollection<string>.CopyTo(string[] array, int arrayIndex)
            {
                for (var i = 0; i < _values.Count; i++) array[arrayIndex + i] = _values[i];
            }

            void System.Collections.ICollection.CopyTo(Array array, int index)
            {
                for (var i = 0; i < _values.Count; i++) array.SetValue(_values[i], index + i);
            }

            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectBrowserSelectionPlannerSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("ProjectBrowserSelectionPlannerSmoke assertion failed.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("ProjectBrowserSelectionPlannerSmoke expected exception " + typeof(T).Name + ".");
        }
    }
}
