using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Export
{
    public sealed class BcfComponentReference
    {
        public BcfComponentReference(string qs3dElementId, string ifcGlobalId)
        {
            Qs3dElementId = IfcRoundTripProjectionContract.RequireCanonicalToken(qs3dElementId, nameof(qs3dElementId));
            IfcGlobalId = IfcRoundTripProjectionContract.RequireCanonicalToken(ifcGlobalId, nameof(ifcGlobalId));
        }

        public string Qs3dElementId { get; }
        public string IfcGlobalId { get; }

        public static BcfComponentReference FromIfcProjection(IfcRoundTripProjection projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            return new BcfComponentReference(projection.Qs3dElementId, projection.IfcGlobalId);
        }
    }

    public sealed class BcfViewpoint
    {
        public BcfViewpoint(string id, IEnumerable<BcfComponentReference> components)
        {
            Id = IfcRoundTripProjectionContract.RequireCanonicalToken(id, nameof(id));
            Components = CanonicalizeComponents(components);
        }

        public string Id { get; }
        public IReadOnlyList<BcfComponentReference> Components { get; }

        private static IReadOnlyList<BcfComponentReference> CanonicalizeComponents(IEnumerable<BcfComponentReference> components)
        {
            if (components == null) throw new ArgumentNullException(nameof(components));
            var items = components.ToList();
            var qs3dIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ifcIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item == null) throw new ArgumentException("Component collection cannot contain null entries.", nameof(components));
                if (!qs3dIds.Add(item.Qs3dElementId)) throw new ArgumentException("Duplicate QS3D component identity: " + item.Qs3dElementId, nameof(components));
                if (!ifcIds.Add(item.IfcGlobalId)) throw new ArgumentException("Duplicate IFC component identity: " + item.IfcGlobalId, nameof(components));
            }
            items.Sort(BcfComponentReferenceComparer.Instance);
            return Array.AsReadOnly(items.ToArray());
        }
    }

    public sealed class BcfComment
    {
        public BcfComment(string id, string author, DateTime createdUtc, string text, string? viewpointId)
        {
            Id = IfcRoundTripProjectionContract.RequireCanonicalToken(id, nameof(id));
            Author = BcfIssueExchangeContract.RequireText(author, nameof(author), false);
            if (createdUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("BCF comment timestamps must be UTC.", nameof(createdUtc));
            CreatedUtc = createdUtc;
            Text = BcfIssueExchangeContract.RequireText(text, nameof(text), false);
            ViewpointId = viewpointId == null ? null : IfcRoundTripProjectionContract.RequireCanonicalToken(viewpointId, nameof(viewpointId));
        }

        public string Id { get; }
        public string Author { get; }
        public DateTime CreatedUtc { get; }
        public string Text { get; }
        public string? ViewpointId { get; }
    }

    public sealed class BcfTopic
    {
        public BcfTopic(
            string id,
            string title,
            string status,
            string type,
            string description,
            IEnumerable<BcfComment> comments,
            IEnumerable<BcfViewpoint> viewpoints)
        {
            Id = IfcRoundTripProjectionContract.RequireCanonicalToken(id, nameof(id));
            Title = BcfIssueExchangeContract.RequireText(title, nameof(title), false);
            Status = IfcRoundTripProjectionContract.RequireCanonicalToken(status, nameof(status));
            Type = IfcRoundTripProjectionContract.RequireCanonicalToken(type, nameof(type));
            Description = BcfIssueExchangeContract.RequireText(description, nameof(description), true);
            Viewpoints = CanonicalizeViewpoints(viewpoints);
            Comments = CanonicalizeComments(comments, Viewpoints);
        }

        public string Id { get; }
        public string Title { get; }
        public string Status { get; }
        public string Type { get; }
        public string Description { get; }
        public IReadOnlyList<BcfComment> Comments { get; }
        public IReadOnlyList<BcfViewpoint> Viewpoints { get; }

        private static IReadOnlyList<BcfViewpoint> CanonicalizeViewpoints(IEnumerable<BcfViewpoint> viewpoints)
        {
            if (viewpoints == null) throw new ArgumentNullException(nameof(viewpoints));
            var items = viewpoints.ToList();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item == null) throw new ArgumentException("Viewpoint collection cannot contain null entries.", nameof(viewpoints));
                if (!ids.Add(item.Id)) throw new ArgumentException("Duplicate BCF viewpoint identity: " + item.Id, nameof(viewpoints));
            }
            items.Sort(BcfViewpointComparer.Instance);
            return Array.AsReadOnly(items.ToArray());
        }

        private static IReadOnlyList<BcfComment> CanonicalizeComments(IEnumerable<BcfComment> comments, IReadOnlyList<BcfViewpoint> viewpoints)
        {
            if (comments == null) throw new ArgumentNullException(nameof(comments));
            var items = comments.ToList();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var viewpointIds = new HashSet<string>(viewpoints.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item == null) throw new ArgumentException("Comment collection cannot contain null entries.", nameof(comments));
                if (!ids.Add(item.Id)) throw new ArgumentException("Duplicate BCF comment identity: " + item.Id, nameof(comments));
                if (item.ViewpointId != null && !viewpointIds.Contains(item.ViewpointId))
                    throw new ArgumentException("BCF comment references an unknown viewpoint: " + item.ViewpointId, nameof(comments));
            }
            items.Sort(BcfCommentComparer.Instance);
            return Array.AsReadOnly(items.ToArray());
        }
    }

    public sealed class BcfIssueExchange
    {
        public const string SchemaVersion = "3.0";

        private BcfIssueExchange(IReadOnlyList<BcfTopic> topics)
        {
            Topics = topics;
        }

        public IReadOnlyList<BcfTopic> Topics { get; }

        public static BcfIssueExchange Create(IEnumerable<BcfTopic> topics)
        {
            if (topics == null) throw new ArgumentNullException(nameof(topics));
            var items = topics.ToList();
            if (items.Count == 0) throw new ArgumentException("At least one BCF topic is required.", nameof(topics));
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item == null) throw new ArgumentException("Topic collection cannot contain null entries.", nameof(topics));
                if (!ids.Add(item.Id)) throw new ArgumentException("Duplicate BCF topic identity: " + item.Id, nameof(topics));
            }
            items.Sort(BcfTopicComparer.Instance);
            return new BcfIssueExchange(Array.AsReadOnly(items.ToArray()));
        }
    }

    internal static class BcfIssueExchangeContract
    {
        internal static string RequireText(string value, string parameterName, bool allowEmpty)
        {
            if (value == null) throw new ArgumentNullException(parameterName);
            if (!allowEmpty && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A non-empty value is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Value must not contain surrounding whitespace.", parameterName);
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsControl(character) && character != '\r' && character != '\n' && character != '\t')
                    throw new ArgumentException("Value must not contain unsupported control characters.", parameterName);
            }
            return value;
        }
    }

    internal sealed class BcfComponentReferenceComparer : IComparer<BcfComponentReference>
    {
        internal static readonly BcfComponentReferenceComparer Instance = new BcfComponentReferenceComparer();
        private BcfComponentReferenceComparer() { }

        public int Compare(BcfComponentReference? x, BcfComponentReference? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var byIfc = StringComparer.Ordinal.Compare(x.IfcGlobalId, y.IfcGlobalId);
            if (byIfc != 0) return byIfc;
            var byQs3d = StringComparer.OrdinalIgnoreCase.Compare(x.Qs3dElementId, y.Qs3dElementId);
            if (byQs3d != 0) return byQs3d;
            return StringComparer.Ordinal.Compare(x.Qs3dElementId, y.Qs3dElementId);
        }
    }

    internal sealed class BcfViewpointComparer : IComparer<BcfViewpoint>
    {
        internal static readonly BcfViewpointComparer Instance = new BcfViewpointComparer();
        private BcfViewpointComparer() { }
        public int Compare(BcfViewpoint? x, BcfViewpoint? y) => CompareIds(x?.Id, y?.Id);

        private static int CompareIds(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var ignoreCase = StringComparer.OrdinalIgnoreCase.Compare(x, y);
            return ignoreCase != 0 ? ignoreCase : StringComparer.Ordinal.Compare(x, y);
        }
    }

    internal sealed class BcfCommentComparer : IComparer<BcfComment>
    {
        internal static readonly BcfCommentComparer Instance = new BcfCommentComparer();
        private BcfCommentComparer() { }

        public int Compare(BcfComment? x, BcfComment? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var byTime = x.CreatedUtc.CompareTo(y.CreatedUtc);
            if (byTime != 0) return byTime;
            var ignoreCase = StringComparer.OrdinalIgnoreCase.Compare(x.Id, y.Id);
            return ignoreCase != 0 ? ignoreCase : StringComparer.Ordinal.Compare(x.Id, y.Id);
        }
    }

    internal sealed class BcfTopicComparer : IComparer<BcfTopic>
    {
        internal static readonly BcfTopicComparer Instance = new BcfTopicComparer();
        private BcfTopicComparer() { }

        public int Compare(BcfTopic? x, BcfTopic? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var ignoreCase = StringComparer.OrdinalIgnoreCase.Compare(x.Id, y.Id);
            return ignoreCase != 0 ? ignoreCase : StringComparer.Ordinal.Compare(x.Id, y.Id);
        }
    }
}
