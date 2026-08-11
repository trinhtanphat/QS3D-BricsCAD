using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Navigation
{
    public sealed class ProjectBrowserVisibleRow
    {
        internal ProjectBrowserVisibleRow(
            string path,
            int depth,
            ProjectBrowserNode node,
            bool isExpanded)
        {
            Path = path ?? string.Empty;
            Depth = depth;
            Key = node?.Key ?? string.Empty;
            DisplayName = node?.DisplayName ?? string.Empty;
            Kind = node == null ? ProjectBrowserNodeKind.Root : node.Kind;
            Count = node?.Count ?? 0;
            DirtyCount = node?.DirtyCount ?? 0;
            HasChildren = node != null && node.Children.Count > 0;
            IsExpanded = isExpanded;
        }

        public string Path { get; }
        public int Depth { get; }
        public string Key { get; }
        public string DisplayName { get; }
        public ProjectBrowserNodeKind Kind { get; }
        public int Count { get; }
        public int DirtyCount { get; }
        public bool HasChildren { get; }
        public bool IsExpanded { get; }
    }

    public sealed class ProjectBrowserViewport
    {
        internal ProjectBrowserViewport(
            int offset,
            int pageSize,
            int totalVisibleRows,
            IEnumerable<ProjectBrowserVisibleRow> rows)
        {
            Offset = offset;
            PageSize = pageSize;
            TotalVisibleRows = totalVisibleRows;
            Rows = (rows ?? Enumerable.Empty<ProjectBrowserVisibleRow>()).ToList().AsReadOnly();
        }

        public int Offset { get; }
        public int PageSize { get; }
        public int TotalVisibleRows { get; }
        public IReadOnlyList<ProjectBrowserVisibleRow> Rows { get; }
        public bool HasPrevious => Offset > 0;
        public bool HasNext => Offset + Rows.Count < TotalVisibleRows;
    }

    public sealed class ProjectBrowserElementPage
    {
        internal ProjectBrowserElementPage(string nodePath, int offset, int pageSize, int totalCount, IEnumerable<string> elementIds)
        {
            NodePath = nodePath ?? string.Empty;
            Offset = offset;
            PageSize = pageSize;
            TotalCount = totalCount;
            ElementIds = (elementIds ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
        }

        public string NodePath { get; }
        public int Offset { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public IReadOnlyList<string> ElementIds { get; }
        public bool HasPrevious => Offset > 0;
        public bool HasNext => Offset + ElementIds.Count < TotalCount;
    }

    public static class ProjectBrowserVirtualizationPlanner
    {
        private const int MaxPageSize = 1000;
        private const int MaxExpandedPaths = 50000;
        private const int MaxNodes = 500000;
        private const int MaxDepth = 32;

        public static string GetRootPath(ProjectBrowserNode root)
        {
            ValidateNode(root, 0);
            return Segment(root.Key);
        }

        public static ProjectBrowserViewport BuildViewport(
            ProjectBrowserNode root,
            IEnumerable<string> expandedPaths,
            int offset = 0,
            int pageSize = 200)
        {
            ValidatePaging(offset, pageSize);
            var index = BuildIndex(root);
            var expanded = NormalizeExpanded(expandedPaths, index);
            var visible = new List<ProjectBrowserVisibleRow>();
            AppendVisible(root, Segment(root.Key), 0, expanded, visible);
            if (offset > visible.Count)
                throw new ArgumentOutOfRangeException(nameof(offset), "Project browser viewport offset exceeds visible row count.");

            return new ProjectBrowserViewport(
                offset,
                pageSize,
                visible.Count,
                visible.Skip(offset).Take(pageSize));
        }

        public static ProjectBrowserElementPage GetElementPage(
            ProjectBrowserNode root,
            string nodePath,
            int offset = 0,
            int pageSize = 200)
        {
            ValidatePaging(offset, pageSize);
            if (string.IsNullOrWhiteSpace(nodePath)) throw new ArgumentException("Project browser node path is required.", nameof(nodePath));
            if (!string.Equals(nodePath, nodePath.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Project browser node path must not contain surrounding whitespace.", nameof(nodePath));

            var index = BuildIndex(root);
            if (!index.TryGetValue(nodePath, out var node))
                throw new InvalidOperationException("Project browser node path does not exist: " + nodePath + ".");
            if (offset > node.ElementIds.Count)
                throw new ArgumentOutOfRangeException(nameof(offset), "Project browser element-page offset exceeds node element count.");

            return new ProjectBrowserElementPage(
                nodePath,
                offset,
                pageSize,
                node.ElementIds.Count,
                node.ElementIds.Skip(offset).Take(pageSize));
        }

        private static Dictionary<string, ProjectBrowserNode> BuildIndex(ProjectBrowserNode root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            var result = new Dictionary<string, ProjectBrowserNode>(StringComparer.Ordinal);
            IndexNode(root, string.Empty, 0, result);
            return result;
        }

        private static void IndexNode(
            ProjectBrowserNode node,
            string parentPath,
            int depth,
            IDictionary<string, ProjectBrowserNode> index)
        {
            ValidateNode(node, depth);
            if (index.Count >= MaxNodes)
                throw new InvalidOperationException("Project browser virtualization supports at most " + MaxNodes + " tree nodes.");

            var path = parentPath.Length == 0 ? Segment(node.Key) : parentPath + "/" + Segment(node.Key);
            if (index.ContainsKey(path))
                throw new InvalidOperationException("Project browser virtualization found duplicate node path: " + path + ".");
            index.Add(path, node);
            foreach (var child in node.Children)
                IndexNode(child, path, depth + 1, index);
        }

        private static void ValidateNode(ProjectBrowserNode node, int depth)
        {
            if (node == null) throw new InvalidOperationException("Project browser virtualization found a null node.");
            if (depth > MaxDepth) throw new InvalidOperationException("Project browser tree exceeds maximum supported depth " + MaxDepth + ".");
            if (string.IsNullOrWhiteSpace(node.Key)) throw new InvalidOperationException("Project browser node key is required.");
            if (string.IsNullOrWhiteSpace(node.DisplayName)) throw new InvalidOperationException("Project browser node display name is required: " + node.Key + ".");
            if (!Enum.IsDefined(typeof(ProjectBrowserNodeKind), node.Kind)) throw new InvalidOperationException("Project browser node has an undefined kind: " + node.Key + ".");
            if (node.DirtyCount < 0 || node.DirtyCount > node.Count) throw new InvalidOperationException("Project browser node dirty count is invalid: " + node.Key + ".");
            if (node.Children == null || node.ElementIds == null) throw new InvalidOperationException("Project browser node collections are required: " + node.Key + ".");
        }

        private static HashSet<string> NormalizeExpanded(
            IEnumerable<string> expandedPaths,
            IDictionary<string, ProjectBrowserNode> index)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (expandedPaths == null) return result;
            var count = 0;
            foreach (var raw in expandedPaths)
            {
                count++;
                if (count > MaxExpandedPaths)
                    throw new InvalidOperationException("Project browser supports at most " + MaxExpandedPaths + " expanded node paths.");
                if (string.IsNullOrWhiteSpace(raw)) throw new ArgumentException("Project browser expanded node path is required.", nameof(expandedPaths));
                if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                    throw new ArgumentException("Project browser expanded node path must not contain surrounding whitespace.", nameof(expandedPaths));
                if (!result.Add(raw)) throw new InvalidOperationException("Project browser contains duplicate expanded node path: " + raw + ".");
                if (!index.ContainsKey(raw)) throw new InvalidOperationException("Project browser expanded node path does not exist: " + raw + ".");
            }
            return result;
        }

        private static void AppendVisible(
            ProjectBrowserNode node,
            string path,
            int depth,
            ISet<string> expanded,
            ICollection<ProjectBrowserVisibleRow> rows)
        {
            var isExpanded = expanded.Contains(path);
            rows.Add(new ProjectBrowserVisibleRow(path, depth, node, isExpanded));
            if (!isExpanded) return;
            foreach (var child in node.Children)
            {
                var childPath = path + "/" + Segment(child.Key);
                AppendVisible(child, childPath, depth + 1, expanded, rows);
            }
        }

        private static string Segment(string key) => Uri.EscapeDataString(key ?? string.Empty);

        private static void ValidatePaging(int offset, int pageSize)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (pageSize <= 0 || pageSize > MaxPageSize)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Project browser page size must be between 1 and " + MaxPageSize + ".");
        }
    }
}
