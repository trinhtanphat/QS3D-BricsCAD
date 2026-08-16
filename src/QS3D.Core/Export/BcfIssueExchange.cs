using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace QS3D.Core.Export
{
    public sealed class BcfComponentReference
    {
        public BcfComponentReference(string qs3dElementId, string ifcGlobalId)
        {
            Qs3dElementId = BcfIssueExchangeContract.RequireToken(qs3dElementId, nameof(qs3dElementId));
            IfcGlobalId = BcfIssueExchangeContract.RequireIfcGuid(ifcGlobalId, nameof(ifcGlobalId));
        }

        public string Qs3dElementId { get; }
        public string IfcGlobalId { get; }

        public static BcfComponentReference FromIfcProjection(IfcRoundTripProjection projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            return new BcfComponentReference(projection.Qs3dElementId, projection.IfcGlobalId);
        }
    }

    public sealed class BcfPoint3
    {
        public BcfPoint3(double x, double y, double z)
        {
            X = IfcRoundTripProjectionContract.RequireFinite(x, nameof(x));
            Y = IfcRoundTripProjectionContract.RequireFinite(y, nameof(y));
            Z = IfcRoundTripProjectionContract.RequireFinite(z, nameof(z));
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        internal bool IsZero => X == 0d && Y == 0d && Z == 0d;
    }

    public sealed class BcfOrthogonalCamera
    {
        public BcfOrthogonalCamera(
            BcfPoint3 viewPoint,
            BcfPoint3 direction,
            BcfPoint3 upVector,
            double viewToWorldScale,
            double aspectRatio)
        {
            ViewPoint = viewPoint ?? throw new ArgumentNullException(nameof(viewPoint));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            UpVector = upVector ?? throw new ArgumentNullException(nameof(upVector));
            if (Direction.IsZero) throw new ArgumentException("BCF camera direction must be non-zero.", nameof(direction));
            if (UpVector.IsZero) throw new ArgumentException("BCF camera up vector must be non-zero.", nameof(upVector));
            ViewToWorldScale = BcfIssueExchangeContract.RequirePositiveFinite(viewToWorldScale, nameof(viewToWorldScale));
            AspectRatio = BcfIssueExchangeContract.RequirePositiveFinite(aspectRatio, nameof(aspectRatio));
        }

        public BcfPoint3 ViewPoint { get; }
        public BcfPoint3 Direction { get; }
        public BcfPoint3 UpVector { get; }
        public double ViewToWorldScale { get; }
        public double AspectRatio { get; }
    }

    public sealed class BcfViewpoint
    {
        public BcfViewpoint(string id, BcfOrthogonalCamera camera, IEnumerable<BcfComponentReference> components)
        {
            Id = BcfIssueExchangeContract.RequireBcfGuid(id, nameof(id));
            Camera = camera ?? throw new ArgumentNullException(nameof(camera));
            Components = CanonicalizeComponents(components);
        }

        public string Id { get; }
        public BcfOrthogonalCamera Camera { get; }
        public IReadOnlyList<BcfComponentReference> Components { get; }

        private static IReadOnlyList<BcfComponentReference> CanonicalizeComponents(IEnumerable<BcfComponentReference> components)
        {
            var items = BcfIssueExchangeContract.MaterializeBounded(
                components,
                BcfIssueExchangeContract.MaxComponentsPerViewpoint,
                nameof(components),
                "BCF viewpoint component count exceeds the bounded package contract.");
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
            Id = BcfIssueExchangeContract.RequireBcfGuid(id, nameof(id));
            Author = BcfIssueExchangeContract.RequireText(author, nameof(author), false);
            CreatedUtc = BcfIssueExchangeContract.RequireUtc(createdUtc, nameof(createdUtc));
            Text = BcfIssueExchangeContract.RequireText(text, nameof(text), false);
            ViewpointId = viewpointId == null ? null : BcfIssueExchangeContract.RequireBcfGuid(viewpointId, nameof(viewpointId));
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
            string creationAuthor,
            DateTime creationDateUtc,
            IEnumerable<BcfComment> comments,
            IEnumerable<BcfViewpoint> viewpoints)
        {
            Id = BcfIssueExchangeContract.RequireBcfGuid(id, nameof(id));
            Title = BcfIssueExchangeContract.RequireText(title, nameof(title), false);
            Status = BcfIssueExchangeContract.RequireToken(status, nameof(status));
            Type = BcfIssueExchangeContract.RequireToken(type, nameof(type));
            Description = BcfIssueExchangeContract.RequireText(description, nameof(description), true);
            CreationAuthor = BcfIssueExchangeContract.RequireText(creationAuthor, nameof(creationAuthor), false);
            CreationDateUtc = BcfIssueExchangeContract.RequireUtc(creationDateUtc, nameof(creationDateUtc));
            Viewpoints = CanonicalizeViewpoints(viewpoints);
            Comments = CanonicalizeComments(comments, Viewpoints);
        }

        public string Id { get; }
        public string Title { get; }
        public string Status { get; }
        public string Type { get; }
        public string Description { get; }
        public string CreationAuthor { get; }
        public DateTime CreationDateUtc { get; }
        public IReadOnlyList<BcfComment> Comments { get; }
        public IReadOnlyList<BcfViewpoint> Viewpoints { get; }

        private static IReadOnlyList<BcfViewpoint> CanonicalizeViewpoints(IEnumerable<BcfViewpoint> viewpoints)
        {
            var items = BcfIssueExchangeContract.MaterializeBounded(
                viewpoints,
                BcfIssueExchangeContract.MaxViewpointsPerTopic,
                nameof(viewpoints),
                "BCF viewpoint count exceeds the bounded package contract.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
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
            var items = BcfIssueExchangeContract.MaterializeBounded(
                comments,
                BcfIssueExchangeContract.MaxCommentsPerTopic,
                nameof(comments),
                "BCF comment count exceeds the bounded package contract.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var viewpointIds = new HashSet<string>(viewpoints.Select(x => x.Id), StringComparer.Ordinal);
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
            var items = BcfIssueExchangeContract.MaterializeBounded(
                topics,
                BcfIssueExchangeContract.MaxTopics,
                nameof(topics),
                "BCF topic count exceeds the bounded package contract.");
            if (items.Count == 0) throw new ArgumentException("At least one BCF topic is required.", nameof(topics));
            var ids = new HashSet<string>(StringComparer.Ordinal);
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
        internal const int MaxTopics = 256;
        internal const int MaxViewpointsPerTopic = 256;
        internal const int MaxCommentsPerTopic = 1024;
        internal const int MaxComponentsPerViewpoint = 1000;

        internal static List<T> MaterializeBounded<T>(
            IEnumerable<T> values,
            int maximumCount,
            string parameterName,
            string overflowMessage)
        {
            if (values == null) throw new ArgumentNullException(parameterName);

            if (values is ICollection<T> collection && collection.Count > maximumCount)
                throw new ArgumentException(overflowMessage, parameterName);
            if (values is IReadOnlyCollection<T> readOnlyCollection && readOnlyCollection.Count > maximumCount)
                throw new ArgumentException(overflowMessage, parameterName);

            var items = new List<T>();
            var observedCount = 0;
            foreach (var value in values)
            {
                observedCount++;
                if (observedCount > maximumCount)
                    throw new ArgumentException(overflowMessage, parameterName);
                items.Add(value);
            }

            return items;
        }

        internal static string RequireBcfGuid(string value, string parameterName)
        {
            var token = IfcRoundTripProjectionContract.RequireCanonicalToken(value, parameterName);
            if (!Guid.TryParseExact(token, "D", out var parsed) || !string.Equals(token, parsed.ToString("D"), StringComparison.Ordinal))
                throw new ArgumentException("BCF GUIDs must use canonical lowercase 8-4-4-4-12 form.", parameterName);
            return token;
        }

        internal static string RequireIfcGuid(string value, string parameterName)
        {
            var token = IfcRoundTripProjectionContract.RequireCanonicalToken(value, parameterName);
            if (token.Length != 22) throw new ArgumentException("BCF IFC GUIDs must contain exactly 22 characters.", parameterName);
            for (var index = 0; index < token.Length; index++)
            {
                var character = token[index];
                if ((character >= '0' && character <= '9') ||
                    (character >= 'A' && character <= 'Z') ||
                    (character >= 'a' && character <= 'z') ||
                    character == '_' || character == '$')
                    continue;
                throw new ArgumentException("BCF IFC GUIDs contain only alphanumeric, underscore, or dollar characters.", parameterName);
            }
            return token;
        }

        internal static string RequireToken(string value, string parameterName)
        {
            var token = IfcRoundTripProjectionContract.RequireCanonicalToken(value, parameterName);
            return RequireXmlText(token, parameterName);
        }

        internal static DateTime RequireUtc(DateTime value, string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc) throw new ArgumentException("BCF timestamps must be UTC.", parameterName);
            return value;
        }

        internal static double RequirePositiveFinite(double value, string parameterName)
        {
            value = IfcRoundTripProjectionContract.RequireFinite(value, parameterName);
            if (value <= 0d) throw new ArgumentOutOfRangeException(parameterName, "Value must be positive.");
            return value;
        }

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
            return RequireXmlText(value, parameterName);
        }

        private static string RequireXmlText(string value, string parameterName)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException exception)
            {
                throw new ArgumentException("Value contains characters that are invalid in XML.", parameterName, exception);
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
        public int Compare(BcfViewpoint? x, BcfViewpoint? y) => StringComparer.Ordinal.Compare(x?.Id, y?.Id);
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
            return byTime != 0 ? byTime : StringComparer.Ordinal.Compare(x.Id, y.Id);
        }
    }

    internal sealed class BcfTopicComparer : IComparer<BcfTopic>
    {
        internal static readonly BcfTopicComparer Instance = new BcfTopicComparer();
        private BcfTopicComparer() { }
        public int Compare(BcfTopic? x, BcfTopic? y) => StringComparer.Ordinal.Compare(x?.Id, y?.Id);
    }
}
