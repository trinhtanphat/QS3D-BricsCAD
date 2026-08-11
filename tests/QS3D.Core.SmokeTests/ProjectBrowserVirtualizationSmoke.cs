using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserVirtualizationSmoke
    {
        public static void Run()
        {
            ExpansionControlsVisibleRows();
            ElementIdsArePagedDeterministically();
            InvalidExpansionFailsClosed();
            ViewportCollectionsAreImmutable();
            NodeCapFailsBeforeIndexMutation();
            ViewportOffsetAtTotalReturnsEmptyFinalPage();
        }

        private static void ExpansionControlsVisibleRows()
        {
            var root = BuildRoot();
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(root);
            var collapsed = ProjectBrowserVirtualizationPlanner.BuildViewport(root, Array.Empty<string>(), 0, 10);
            Equal(1, collapsed.TotalVisibleRows);
            Equal(rootPath, collapsed.Rows[0].Path);
            True(!collapsed.Rows[0].IsExpanded);

            var firstLevel = ProjectBrowserVirtualizationPlanner.BuildViewport(root, new[] { rootPath }, 0, 10);
            Equal(3, firstLevel.TotalVisibleRows);
            Equal(0, firstLevel.Rows[0].Depth);
            Equal(1, firstLevel.Rows[1].Depth);
            Equal("L01", firstLevel.Rows[1].DisplayName);
            Equal("L02", firstLevel.Rows[2].DisplayName);

            var l02 = firstLevel.Rows.Single(x => x.DisplayName == "L02");
            var expanded = ProjectBrowserVirtualizationPlanner.BuildViewport(root, new[] { rootPath, l02.Path }, 0, 10);
            True(expanded.TotalVisibleRows > firstLevel.TotalVisibleRows);
            True(expanded.Rows.Any(x => x.DisplayName == "Beam" && x.Depth == 2));
            True(expanded.Rows.Any(x => x.DisplayName == "Column" && x.Depth == 2));

            var page = ProjectBrowserVirtualizationPlanner.BuildViewport(root, new[] { rootPath, l02.Path }, 1, 2);
            Equal(2, page.Rows.Count);
            True(page.HasPrevious);
            True(page.HasNext);
        }

        private static void ElementIdsArePagedDeterministically()
        {
            var root = BuildRoot();
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(root);
            var levelOne = ProjectBrowserVirtualizationPlanner.BuildViewport(root, new[] { rootPath }, 0, 10);
            var l02 = levelOne.Rows.Single(x => x.DisplayName == "L02");
            var expanded = ProjectBrowserVirtualizationPlanner.BuildViewport(root, new[] { rootPath, l02.Path }, 0, 10);
            var beam = expanded.Rows.Single(x => x.DisplayName == "Beam");

            var first = ProjectBrowserVirtualizationPlanner.GetElementPage(root, beam.Path, 0, 1);
            Equal(2, first.TotalCount);
            Equal(1, first.ElementIds.Count);
            Equal("B-001", first.ElementIds[0]);
            True(first.HasNext);

            var second = ProjectBrowserVirtualizationPlanner.GetElementPage(root, beam.Path, 1, 1);
            Equal("B-002", second.ElementIds[0]);
            True(second.HasPrevious);
            True(!second.HasNext);
        }

        private static void InvalidExpansionFailsClosed()
        {
            var root = BuildRoot();
            Throws<InvalidOperationException>(() => ProjectBrowserVirtualizationPlanner.BuildViewport(root, new[] { "missing/path" }));
            Throws<ArgumentOutOfRangeException>(() => ProjectBrowserVirtualizationPlanner.BuildViewport(root, Array.Empty<string>(), 2, 10));
            Throws<InvalidOperationException>(() => ProjectBrowserVirtualizationPlanner.GetElementPage(root, "missing/path"));
            Throws<ArgumentOutOfRangeException>(() => ProjectBrowserVirtualizationPlanner.GetElementPage(root, ProjectBrowserVirtualizationPlanner.GetRootPath(root), 0, 1001));
        }

        private static void ViewportCollectionsAreImmutable()
        {
            var root = BuildRoot();
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(root);
            var viewport = ProjectBrowserVirtualizationPlanner.BuildViewport(root, new[] { rootPath });
            Throws<NotSupportedException>(() => ((IList<ProjectBrowserVisibleRow>)viewport.Rows).Clear());
            var elements = ProjectBrowserVirtualizationPlanner.GetElementPage(root, rootPath, 0, 2);
            Throws<NotSupportedException>(() => ((IList<string>)elements.ElementIds).Clear());
        }

        private static void NodeCapFailsBeforeIndexMutation()
        {
            var indexNode = typeof(ProjectBrowserVirtualizationPlanner).GetMethod(
                "IndexNode",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (indexNode == null) throw new Exception("Could not locate ProjectBrowserVirtualizationPlanner.IndexNode.");

            try
            {
                indexNode.Invoke(null, new object[] { BuildRoot(), string.Empty, 0, new AtCapacityIndex() });
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException;
                if (inner is InvalidOperationException invalid &&
                    invalid.Message.IndexOf("at most 500000 tree nodes", StringComparison.Ordinal) >= 0)
                    return;

                throw new Exception("Node-cap guard did not fail before touching the saturated index.", inner ?? ex);
            }

            throw new Exception("Expected saturated Project Browser index to fail closed before mutation.");
        }

        private static void ViewportOffsetAtTotalReturnsEmptyFinalPage()
        {
            var root = BuildRoot();
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(root);
            var first = ProjectBrowserVirtualizationPlanner.BuildViewport(root, new[] { rootPath }, 0, 10);
            var final = ProjectBrowserVirtualizationPlanner.BuildViewport(root, new[] { rootPath }, first.TotalVisibleRows, 10);

            Equal(first.TotalVisibleRows, final.TotalVisibleRows);
            Equal(first.TotalVisibleRows, final.Offset);
            Equal(0, final.Rows.Count);
            True(final.HasPrevious);
            True(!final.HasNext);
        }

        private static ProjectBrowserNode BuildRoot()
        {
            var project = new ProjectState("P-VIRTUAL", "Virtual Browser");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Elements.Add(new ProjectElement("B-002", ElementCategory.Beam, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("C-001", ElementCategory.Column, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("W-001", ElementCategory.ArchitecturalWall, string.Empty, "F-01", "Z-A"));
            return ProjectBrowserPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private sealed class AtCapacityIndex : IDictionary<string, ProjectBrowserNode>
        {
            public int Count => 500000;
            public bool IsReadOnly => false;
            public ICollection<string> Keys => throw Touched();
            public ICollection<ProjectBrowserNode> Values => throw Touched();
            public ProjectBrowserNode this[string key]
            {
                get => throw Touched();
                set => throw Touched();
            }

            public void Add(string key, ProjectBrowserNode value) => throw Touched();
            public bool ContainsKey(string key) => throw Touched();
            public bool Remove(string key) => throw Touched();
            public bool TryGetValue(string key, out ProjectBrowserNode value) => throw Touched();
            public void Add(KeyValuePair<string, ProjectBrowserNode> item) => throw Touched();
            public void Clear() => throw Touched();
            public bool Contains(KeyValuePair<string, ProjectBrowserNode> item) => throw Touched();
            public void CopyTo(KeyValuePair<string, ProjectBrowserNode>[] array, int arrayIndex) => throw Touched();
            public bool Remove(KeyValuePair<string, ProjectBrowserNode> item) => throw Touched();
            public IEnumerator<KeyValuePair<string, ProjectBrowserNode>> GetEnumerator() => throw Touched();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private static Exception Touched() =>
                new Exception("Saturated Project Browser index was touched before the node-cap guard fired.");
        }
    }
}
