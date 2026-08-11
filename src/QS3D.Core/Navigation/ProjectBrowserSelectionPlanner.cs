using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Navigation
{
    public sealed class ProjectBrowserSelectionRevealPlan
    {
        internal ProjectBrowserSelectionRevealPlan(
            IEnumerable<string> selectedElementIds,
            IEnumerable<string> expansionPaths,
            IEnumerable<string> targetNodePaths,
            string primaryElementId)
        {
            SelectedElementIds = (selectedElementIds ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
            ExpansionPaths = (expansionPaths ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
            TargetNodePaths = (targetNodePaths ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
            PrimaryElementId = primaryElementId ?? string.Empty;
        }

        public IReadOnlyList<string> SelectedElementIds { get; }
        public IReadOnlyList<string> ExpansionPaths { get; }
        public IReadOnlyList<string> TargetNodePaths { get; }
        public string PrimaryElementId { get; }
        public bool HasSelection => SelectedElementIds.Count > 0;
        public bool IsMultiSelection => SelectedElementIds.Count > 1;
    }

    public sealed class ProjectBrowserNodeSelectionPlan
    {
        internal ProjectBrowserNodeSelectionPlan(ProjectBrowserElementPage page)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));
            NodePath = page.NodePath;
            Offset = page.Offset;
            PageSize = page.PageSize;
            TotalCount = page.TotalCount;
            ElementIds = page.ElementIds.ToList().AsReadOnly();
        }

        public string NodePath { get; }
        public int Offset { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public IReadOnlyList<string> ElementIds { get; }
        public bool HasPrevious => Offset > 0;
        public bool HasNext => Offset + ElementIds.Count < TotalCount;
        public string PrimaryElementId => ElementIds.Count == 0 ? string.Empty : ElementIds[0];
    }

    public static class ProjectBrowserSelectionPlanner
    {
        private const int MaxSelectedElementIds = 10000;
        private const int MaxNodes = 500000;
        private const int MaxDepth = 32;

        public static ProjectBrowserSelectionRevealPlan PlanReveal(
            ProjectBrowserNode root,
            IEnumerable<string> selectedElementIds,
            string? primaryElementId = null)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            var selected = NormalizeSelection(selectedElementIds);
            var index = BuildIndex(root);

            foreach (var elementId in selected)
                if (!index.Root.ElementIds.Contains(elementId, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Project browser selection references missing semantic element id: " + elementId + ".");

            var primary = NormalizePrimary(primaryElementId, selected);
            if (selected.Count == 0)
                return new ProjectBrowserSelectionRevealPlan(selected, Array.Empty<string>(), Array.Empty<string>(), primary);

            var targetPaths = new SortedSet<string>(StringComparer.Ordinal);
            var expansion = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var elementId in selected)
            {
                if (!index.Memberships.TryGetValue(elementId, out var matches) || matches.Count == 0)
                    throw new InvalidOperationException("Project browser tree lost semantic element id: " + elementId + ".");

                var deepest = matches.Max(x => x.Depth);
                NodeEntry? target = null;
                var deepestCount = 0;
                foreach (var match in matches)
                {
                    if (match.Depth != deepest) continue;
                    target = match;
                    deepestCount++;
                }
                if (deepestCount != 1 || target == null)
                    throw new InvalidOperationException("Project browser selection is ambiguous for semantic element id: " + elementId + ".");

                targetPaths.Add(target.Path);
                var parentPath = target.ParentPath;
                while (parentPath.Length > 0)
                {
                    if (!index.ByPath.TryGetValue(parentPath, out var parent))
                        throw new InvalidOperationException("Project browser selection found a broken ancestor path: " + parentPath + ".");
                    if (!expansion.ContainsKey(parent.Path)) expansion.Add(parent.Path, parent.Depth);
                    parentPath = parent.ParentPath;
                }
            }

            var expansionPaths = expansion
                .OrderBy(x => x.Value)
                .ThenBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => x.Key)
                .ToList()
                .AsReadOnly();

            return new ProjectBrowserSelectionRevealPlan(selected, expansionPaths, targetPaths, primary);
        }

        public static ProjectBrowserNodeSelectionPlan PlanNodeSelection(
            ProjectBrowserNode root,
            string nodePath,
            int offset = 0,
            int pageSize = 200)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            var page = ProjectBrowserVirtualizationPlanner.GetElementPage(root, nodePath, offset, pageSize);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in page.ElementIds)
            {
                var elementId = CanonicalRequired(raw, "project browser node selection element id");
                if (!seen.Add(elementId))
                    throw new InvalidOperationException("Project browser node selection contains duplicate semantic element id: " + elementId + ".");
            }
            return new ProjectBrowserNodeSelectionPlan(page);
        }

        private static IReadOnlyList<string> NormalizeSelection(IEnumerable<string> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in values)
            {
                if (result.Count >= MaxSelectedElementIds)
                    throw new InvalidOperationException("Project browser selection supports at most " + MaxSelectedElementIds + " semantic element ids.");
                var elementId = CanonicalRequired(raw, "project browser selected element id");
                if (!seen.Add(elementId))
                    throw new InvalidOperationException("Project browser selection contains duplicate semantic element id: " + elementId + ".");
                result.Add(elementId);
            }
            result.Sort(CompareCanonicalIds);
            return result.AsReadOnly();
        }

        private static string NormalizePrimary(string? raw, IReadOnlyList<string> selected)
        {
            if (raw == null || string.IsNullOrWhiteSpace(raw)) return selected.Count == 0 ? string.Empty : selected[0];
            var primary = CanonicalRequired(raw, "project browser primary selected element id");
            if (!selected.Contains(primary, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Project browser primary selection must belong to the selected semantic element set: " + primary + ".");
            return selected.First(x => string.Equals(x, primary, StringComparison.OrdinalIgnoreCase));
        }

        private static TreeIndex BuildIndex(ProjectBrowserNode root)
        {
            var byPath = new Dictionary<string, NodeEntry>(StringComparer.Ordinal);
            var memberships = new Dictionary<string, List<NodeEntry>>(StringComparer.OrdinalIgnoreCase);
            var rootEntry = IndexNode(root, string.Empty, 0, null, byPath, memberships);
            return new TreeIndex(rootEntry, byPath, memberships);
        }

        private static NodeEntry IndexNode(
            ProjectBrowserNode node,
            string parentPath,
            int depth,
            HashSet<string>? parentElementIds,
            IDictionary<string, NodeEntry> byPath,
            IDictionary<string, List<NodeEntry>> memberships)
        {
            if (node == null) throw new InvalidOperationException("Project browser selection found a null node.");
            if (depth > MaxDepth)
                throw new InvalidOperationException("Project browser selection tree exceeds maximum supported depth " + MaxDepth + ".");
            if (string.IsNullOrWhiteSpace(node.Key))
                throw new InvalidOperationException("Project browser selection requires non-empty node keys.");
            if (string.IsNullOrWhiteSpace(node.DisplayName))
                throw new InvalidOperationException("Project browser selection requires non-empty node display names: " + node.Key + ".");
            if (node.ElementIds == null || node.Children == null)
                throw new InvalidOperationException("Project browser selection requires node collections: " + node.Key + ".");

            var path = parentPath.Length == 0 ? Segment(node.Key) : parentPath + "/" + Segment(node.Key);
            if (byPath.ContainsKey(path))
                throw new InvalidOperationException("Project browser selection found duplicate node path: " + path + ".");
            if (byPath.Count >= MaxNodes)
                throw new InvalidOperationException("Project browser selection supports at most " + MaxNodes + " tree nodes.");

            var elementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in node.ElementIds)
            {
                var elementId = CanonicalRequired(raw, "project browser node element id");
                if (!elementIds.Add(elementId))
                    throw new InvalidOperationException("Project browser node contains duplicate semantic element id: " + node.Key + "/" + elementId + ".");
                if (parentElementIds != null && !parentElementIds.Contains(elementId))
                    throw new InvalidOperationException("Project browser child contains semantic element outside its parent: " + elementId + ".");
            }

            var entry = new NodeEntry(node, path, parentPath, depth, elementIds);
            byPath.Add(path, entry);
            foreach (var elementId in elementIds)
            {
                if (!memberships.TryGetValue(elementId, out var elementMemberships))
                {
                    elementMemberships = new List<NodeEntry>();
                    memberships.Add(elementId, elementMemberships);
                }
                elementMemberships.Add(entry);
            }

            foreach (var child in node.Children)
                IndexNode(child, path, depth + 1, elementIds, byPath, memberships);
            return entry;
        }

        private static string CanonicalRequired(string? value, string label)
        {
            var raw = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException(label + " is required.");
            if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException(label + " must not contain surrounding whitespace: " + raw + ".");
            return raw;
        }

        private static int CompareCanonicalIds(string left, string right)
        {
            var insensitive = StringComparer.OrdinalIgnoreCase.Compare(left, right);
            return insensitive != 0 ? insensitive : StringComparer.Ordinal.Compare(left, right);
        }

        private static string Segment(string key) => Uri.EscapeDataString(key ?? string.Empty);

        private sealed class TreeIndex
        {
            internal TreeIndex(
                NodeEntry root,
                IDictionary<string, NodeEntry> byPath,
                IDictionary<string, List<NodeEntry>> memberships)
            {
                Root = root;
                ByPath = byPath;
                Memberships = memberships;
            }

            internal NodeEntry Root { get; }
            internal IDictionary<string, NodeEntry> ByPath { get; }
            internal IDictionary<string, List<NodeEntry>> Memberships { get; }
        }

        private sealed class NodeEntry
        {
            internal NodeEntry(ProjectBrowserNode node, string path, string parentPath, int depth, HashSet<string> elementIds)
            {
                Node = node;
                Path = path;
                ParentPath = parentPath ?? string.Empty;
                Depth = depth;
                ElementIds = elementIds;
            }

            internal ProjectBrowserNode Node { get; }
            internal string Path { get; }
            internal string ParentPath { get; }
            internal int Depth { get; }
            internal HashSet<string> ElementIds { get; }
        }
    }
}
