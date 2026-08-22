using System;
using System.IO;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfNumericCanonicalitySmoke
    {
        private const string TopicId = "11111111-1111-1111-1111-111111111111";
        private const string ViewpointId = "22222222-2222-2222-2222-222222222222";
        private static readonly DateTime TopicUtc = new DateTime(2026, 8, 17, 15, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            RejectsExponentAlias();
            RejectsDecimalAlias();
            RejectsLeadingPlusAlias();
            RejectsWhitespaceAlias();
            CanonicalNumbersRoundTripExactly();
        }

        private static void RejectsExponentAlias()
        {
            var canonical = SerializeFixture();
            var mutated = ReplaceExactlyOnce(
                canonical,
                "<ViewToWorldScale>1</ViewToWorldScale>",
                "<ViewToWorldScale>1e0</ViewToWorldScale>");
            Throws<InvalidDataException>(() => BcfIssueExchangeSerializer.Deserialize(mutated));
        }

        private static void RejectsDecimalAlias()
        {
            var canonical = SerializeFixture();
            var mutated = ReplaceExactlyOnce(canonical, "x=\"1\"", "x=\"1.0\"");
            Throws<InvalidDataException>(() => BcfIssueExchangeSerializer.Deserialize(mutated));
        }

        private static void RejectsLeadingPlusAlias()
        {
            var canonical = SerializeFixture();
            var mutated = ReplaceExactlyOnce(
                canonical,
                "<AspectRatio>1.5</AspectRatio>",
                "<AspectRatio>+1.5</AspectRatio>");
            Throws<InvalidDataException>(() => BcfIssueExchangeSerializer.Deserialize(mutated));
        }

        private static void RejectsWhitespaceAlias()
        {
            var canonical = SerializeFixture();
            var mutated = ReplaceExactlyOnce(canonical, "y=\"2\"", "y=\" 2 \"");
            Throws<InvalidDataException>(() => BcfIssueExchangeSerializer.Deserialize(mutated));
        }

        private static void CanonicalNumbersRoundTripExactly()
        {
            var canonical = SerializeFixture();
            var loaded = BcfIssueExchangeSerializer.Deserialize(canonical);
            Require(loaded.Topics.Count == 1, "Canonical BCF numeric round-trip changed topic count.");
            Require(loaded.Topics[0].Viewpoints.Count == 1, "Canonical BCF numeric round-trip changed viewpoint count.");
            var reserialized = BcfIssueExchangeSerializer.Serialize(loaded);
            Require(reserialized == canonical, "Canonical BCF numeric payload changed across deserialize/serialize round-trip.");
        }

        private static string SerializeFixture()
        {
            var camera = new BcfOrthogonalCamera(
                new BcfPoint3(1d, 2d, 3d),
                new BcfPoint3(0d, 0d, -1d),
                new BcfPoint3(0d, 1d, 0d),
                1d,
                1.5d);
            var viewpoint = new BcfViewpoint(ViewpointId, camera, Array.Empty<BcfComponentReference>());
            var topic = new BcfTopic(
                TopicId,
                "Title",
                "Open",
                "Issue",
                "Description",
                "creator@example.test",
                TopicUtc,
                Array.Empty<BcfComment>(),
                new[] { viewpoint });
            return BcfIssueExchangeSerializer.Serialize(BcfIssueExchange.Create(new[] { topic }));
        }

        private static string ReplaceExactlyOnce(string value, string oldValue, string newValue)
        {
            var first = value.IndexOf(oldValue, StringComparison.Ordinal);
            if (first < 0) throw new InvalidOperationException("Expected canonical BCF numeric text was not found.");
            if (value.IndexOf(oldValue, first + oldValue.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("Expected canonical BCF numeric text was not unique.");
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
