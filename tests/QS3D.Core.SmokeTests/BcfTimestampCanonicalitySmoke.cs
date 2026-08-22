using System;
using System.Globalization;
using System.IO;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfTimestampCanonicalitySmoke
    {
        private const string TopicId = "11111111-1111-1111-1111-111111111111";
        private const string CommentId = "22222222-2222-2222-2222-222222222222";
        private static readonly DateTime TopicUtc = new DateTime(2026, 8, 15, 2, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime CommentUtc = TopicUtc.AddMinutes(1);

        internal static void Run()
        {
            RejectsOffsetTopicTimestamp();
            RejectsOffsetCommentTimestamp();
            RejectsNonCanonicalUtcTimestamp();
            CanonicalUtcRoundTripsExactly();
        }

        private static void RejectsOffsetTopicTimestamp()
        {
            var canonical = SerializeFixture();
            var canonicalText = TopicUtc.ToString("O", CultureInfo.InvariantCulture);
            var offsetText = new DateTimeOffset(TopicUtc).ToOffset(TimeSpan.FromHours(7d)).ToString("O", CultureInfo.InvariantCulture);
            var mutated = ReplaceExactlyOnce(canonical, "creationDateUtc=\"" + canonicalText + "\"", "creationDateUtc=\"" + offsetText + "\"");
            Throws<InvalidDataException>(() => BcfIssueExchangeSerializer.Deserialize(mutated));
        }

        private static void RejectsOffsetCommentTimestamp()
        {
            var canonical = SerializeFixture();
            var canonicalText = CommentUtc.ToString("O", CultureInfo.InvariantCulture);
            var offsetText = new DateTimeOffset(CommentUtc).ToOffset(TimeSpan.FromHours(7d)).ToString("O", CultureInfo.InvariantCulture);
            var mutated = ReplaceExactlyOnce(canonical, "createdUtc=\"" + canonicalText + "\"", "createdUtc=\"" + offsetText + "\"");
            Throws<InvalidDataException>(() => BcfIssueExchangeSerializer.Deserialize(mutated));
        }

        private static void RejectsNonCanonicalUtcTimestamp()
        {
            var canonical = SerializeFixture();
            var canonicalText = TopicUtc.ToString("O", CultureInfo.InvariantCulture);
            var mutated = ReplaceExactlyOnce(canonical, "creationDateUtc=\"" + canonicalText + "\"", "creationDateUtc=\"2026-08-15T02:30:00Z\"");
            Throws<InvalidDataException>(() => BcfIssueExchangeSerializer.Deserialize(mutated));
        }

        private static void CanonicalUtcRoundTripsExactly()
        {
            var canonical = SerializeFixture();
            var loaded = BcfIssueExchangeSerializer.Deserialize(canonical);
            Require(loaded.Topics.Count == 1, "Canonical BCF timestamp round-trip changed topic count.");
            var topic = loaded.Topics[0];
            Require(topic.CreationDateUtc == TopicUtc && topic.CreationDateUtc.Kind == DateTimeKind.Utc,
                "Canonical BCF topic timestamp changed across deserialize.");
            Require(topic.Comments.Count == 1, "Canonical BCF timestamp round-trip changed comment count.");
            Require(topic.Comments[0].CreatedUtc == CommentUtc && topic.Comments[0].CreatedUtc.Kind == DateTimeKind.Utc,
                "Canonical BCF comment timestamp changed across deserialize.");
            var reserialized = BcfIssueExchangeSerializer.Serialize(loaded);
            Require(reserialized == canonical, "Canonical BCF payload changed across timestamp round-trip.");
        }

        private static string SerializeFixture()
        {
            var comment = new BcfComment(CommentId, "author@example.test", CommentUtc, "Comment", null);
            var topic = new BcfTopic(
                TopicId,
                "Title",
                "Open",
                "Issue",
                "Description",
                "creator@example.test",
                TopicUtc,
                new[] { comment },
                Array.Empty<BcfViewpoint>());
            return BcfIssueExchangeSerializer.Serialize(BcfIssueExchange.Create(new[] { topic }));
        }

        private static string ReplaceExactlyOnce(string value, string oldValue, string newValue)
        {
            var first = value.IndexOf(oldValue, StringComparison.Ordinal);
            if (first < 0) throw new InvalidOperationException("Expected canonical BCF timestamp attribute was not found.");
            if (value.IndexOf(oldValue, first + oldValue.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("Expected canonical BCF timestamp attribute was not unique.");
            return value.Substring(0, first) + newValue + value.Substring(first + oldValue.Length);
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
