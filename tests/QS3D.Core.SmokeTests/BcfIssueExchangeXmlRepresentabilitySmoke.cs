using System;
using System.Linq;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfIssueExchangeXmlRepresentabilitySmoke
    {
        private const string TopicId = "11111111-1111-1111-1111-111111111111";
        private const string CommentId = "22222222-2222-2222-2222-222222222222";
        private const string ViewpointId = "33333333-3333-3333-3333-333333333333";
        private const string IfcGlobalId = "0123456789ABCDEFGHIJKL";
        private static readonly DateTime TimestampUtc = new DateTime(2026, 8, 15, 2, 30, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            InvalidXmlTextFailsAtConstruction();
            InvalidXmlTokensFailAtConstruction();
            SupplementaryUnicodeRoundTripsExactly();
        }

        private static void InvalidXmlTextFailsAtConstruction()
        {
            const string invalid = "bad\uD800text";

            Throws<ArgumentException>(() => Topic(title: invalid));
            Throws<ArgumentException>(() => Topic(description: invalid));
            Throws<ArgumentException>(() => Topic(creationAuthor: invalid));
            Throws<ArgumentException>(() => new BcfComment(CommentId, invalid, TimestampUtc, "Text", null));
            Throws<ArgumentException>(() => new BcfComment(CommentId, "author@example.test", TimestampUtc, invalid, null));
        }

        private static void InvalidXmlTokensFailAtConstruction()
        {
            const string invalid = "bad\uD800token";

            Throws<ArgumentException>(() => Topic(status: invalid));
            Throws<ArgumentException>(() => Topic(type: invalid));
            Throws<ArgumentException>(() => new BcfComponentReference(invalid, IfcGlobalId));
        }

        private static void SupplementaryUnicodeRoundTripsExactly()
        {
            const string marker = "\U0001F9ED";
            var component = new BcfComponentReference("ELEMENT-" + marker, IfcGlobalId);
            var camera = new BcfOrthogonalCamera(
                new BcfPoint3(1d, 2d, 3d),
                new BcfPoint3(0d, 0d, -1d),
                new BcfPoint3(0d, 1d, 0d),
                2.5d,
                1.6d);
            var viewpoint = new BcfViewpoint(ViewpointId, camera, new[] { component });
            var comment = new BcfComment(
                CommentId,
                "author-" + marker,
                TimestampUtc.AddMinutes(1),
                "Comment " + marker,
                ViewpointId);
            var topic = new BcfTopic(
                TopicId,
                "Title " + marker,
                "Open-" + marker,
                "Issue-" + marker,
                "Description " + marker,
                "creator-" + marker,
                TimestampUtc,
                new[] { comment },
                new[] { viewpoint });
            var exchange = BcfIssueExchange.Create(new[] { topic });

            var payload = BcfIssueExchangeSerializer.Serialize(exchange);
            var loaded = BcfIssueExchangeSerializer.Deserialize(payload);
            Require(loaded.Topics.Count == 1, "BCF supplementary-Unicode round-trip changed topic count.");

            var loadedTopic = loaded.Topics[0];
            Require(loadedTopic.Title == topic.Title, "BCF title changed across supplementary-Unicode round-trip.");
            Require(loadedTopic.Status == topic.Status, "BCF status changed across supplementary-Unicode round-trip.");
            Require(loadedTopic.Type == topic.Type, "BCF type changed across supplementary-Unicode round-trip.");
            Require(loadedTopic.Description == topic.Description, "BCF description changed across supplementary-Unicode round-trip.");
            Require(loadedTopic.CreationAuthor == topic.CreationAuthor, "BCF creation author changed across supplementary-Unicode round-trip.");

            var loadedComment = loadedTopic.Comments.Single();
            Require(loadedComment.Author == comment.Author, "BCF comment author changed across supplementary-Unicode round-trip.");
            Require(loadedComment.Text == comment.Text, "BCF comment text changed across supplementary-Unicode round-trip.");
            Require(loadedComment.ViewpointId == ViewpointId, "BCF comment viewpoint identity changed across round-trip.");

            var loadedComponent = loadedTopic.Viewpoints.Single().Components.Single();
            Require(loadedComponent.Qs3dElementId == component.Qs3dElementId, "BCF QS3D component id changed across supplementary-Unicode round-trip.");
            Require(loadedComponent.IfcGlobalId == IfcGlobalId, "BCF IFC component id changed across round-trip.");
        }

        private static BcfTopic Topic(
            string title = "Title",
            string status = "Open",
            string type = "Issue",
            string description = "Description",
            string creationAuthor = "author@example.test")
        {
            return new BcfTopic(
                TopicId,
                title,
                status,
                type,
                description,
                creationAuthor,
                TimestampUtc,
                Array.Empty<BcfComment>(),
                Array.Empty<BcfViewpoint>());
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
