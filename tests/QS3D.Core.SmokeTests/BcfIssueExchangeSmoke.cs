using System;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfIssueExchangeSmoke
    {
        public static void Run()
        {
            SerializationIsDeterministicAcrossInputOrdering();
            RoundTripPreservesTopicCommentViewpointAndIfcIdentity();
            DanglingAndDuplicateReferencesFailClosed();
            MalformedPayloadFailsClosed();
        }

        private static void SerializationIsDeterministicAcrossInputOrdering()
        {
            var forward = BcfIssueExchangeSerializer.Serialize(BuildFixture(false));
            var reversed = BcfIssueExchangeSerializer.Serialize(BuildFixture(true));
            if (!string.Equals(forward, reversed, StringComparison.Ordinal))
                throw new Exception("BCF issue serialization must be deterministic for semantically equivalent input ordering.");
            Require(forward, "schemaVersion=\"3.0\"");
            Require(forward, "qs3dElementId=\"E-A\"");
            Require(forward, "ifcGlobalId=\"IFC-A\"");
        }

        private static void RoundTripPreservesTopicCommentViewpointAndIfcIdentity()
        {
            var original = BuildFixture(true);
            var serialized = BcfIssueExchangeSerializer.Serialize(original);
            var roundTrip = BcfIssueExchangeSerializer.Deserialize(serialized);
            var serializedAgain = BcfIssueExchangeSerializer.Serialize(roundTrip);
            if (!string.Equals(serialized, serializedAgain, StringComparison.Ordinal))
                throw new Exception("BCF issue payload did not round-trip deterministically.");

            if (roundTrip.Topics.Count != 2) throw new Exception("BCF topic count changed during round-trip.");
            if (!string.Equals(roundTrip.Topics[0].Id, "TOPIC-A", StringComparison.Ordinal))
                throw new Exception("BCF topics were not canonicalized by stable identity.");

            var topic = roundTrip.Topics[1];
            if (!string.Equals(topic.Id, "TOPIC-B", StringComparison.Ordinal)) throw new Exception("BCF topic identity was not preserved.");
            if (topic.Viewpoints.Count != 1 || topic.Comments.Count != 2) throw new Exception("BCF viewpoint/comment payload changed during round-trip.");
            if (!string.Equals(topic.Comments[0].Id, "COMMENT-1", StringComparison.Ordinal)) throw new Exception("BCF comments were not canonicalized deterministically.");
            if (!string.Equals(topic.Comments[0].ViewpointId, "VP-1", StringComparison.Ordinal)) throw new Exception("BCF comment viewpoint reference was not preserved.");

            var components = topic.Viewpoints[0].Components;
            if (components.Count != 2) throw new Exception("BCF component references changed during round-trip.");
            if (!string.Equals(components[0].Qs3dElementId, "E-A", StringComparison.Ordinal) ||
                !string.Equals(components[0].IfcGlobalId, "IFC-A", StringComparison.Ordinal))
                throw new Exception("BCF component identity bridge did not preserve QS3D and IFC identities together.");
        }

        private static void DanglingAndDuplicateReferencesFailClosed()
        {
            ThrowsArgument(
                () => new BcfTopic(
                    "TOPIC-X",
                    "Broken topic",
                    "Open",
                    "Error",
                    string.Empty,
                    new[] { new BcfComment("COMMENT-X", "qa@qs3d", Utc(10), "Dangling", "VP-MISSING") },
                    Array.Empty<BcfViewpoint>()),
                "Dangling BCF comment viewpoint references must fail closed.");

            ThrowsArgument(
                () => new BcfViewpoint(
                    "VP-X",
                    new[]
                    {
                        new BcfComponentReference("E-DUP", "IFC-1"),
                        new BcfComponentReference("e-dup", "IFC-2")
                    }),
                "Case-insensitive duplicate QS3D component identities must fail closed.");
        }

        private static void MalformedPayloadFailsClosed()
        {
            ThrowsInvalidData(
                () => BcfIssueExchangeSerializer.Deserialize("<BcfIssueExchange schemaVersion=\"2.1\"></BcfIssueExchange>"),
                "Unsupported BCF schema versions must fail closed.");
            ThrowsInvalidData(
                () => BcfIssueExchangeSerializer.Deserialize("<broken"),
                "Malformed BCF XML must fail closed.");
        }

        private static BcfIssueExchange BuildFixture(bool reverse)
        {
            var componentA = BcfComponentReference.FromIfcProjection(BuildProjection("E-A", "IFC-A"));
            var componentB = BcfComponentReference.FromIfcProjection(BuildProjection("E-B", "IFC-B"));
            var viewpoint = new BcfViewpoint(
                "VP-1",
                reverse ? new[] { componentB, componentA } : new[] { componentA, componentB });

            var comment1 = new BcfComment("COMMENT-1", "qa@qs3d", Utc(10), "Check element identity.", "VP-1");
            var comment2 = new BcfComment("COMMENT-2", "review@qs3d", Utc(11), "Identity confirmed.", null);
            var topicB = new BcfTopic(
                "TOPIC-B",
                "BCF identity bridge",
                "Open",
                "Coordination",
                "QS3D and IFC identities must survive BCF issue exchange.",
                reverse ? new[] { comment2, comment1 } : new[] { comment1, comment2 },
                new[] { viewpoint });
            var topicA = new BcfTopic(
                "TOPIC-A",
                "Canonical ordering",
                "Closed",
                "Information",
                string.Empty,
                Array.Empty<BcfComment>(),
                Array.Empty<BcfViewpoint>());

            return BcfIssueExchange.Create(reverse ? new[] { topicB, topicA } : new[] { topicA, topicB });
        }

        private static IfcRoundTripProjection BuildProjection(string qs3dElementId, string ifcGlobalId)
        {
            return new IfcRoundTripProjection(
                qs3dElementId,
                ifcGlobalId,
                "Beam",
                Array.Empty<IfcRoundTripNumericProperty>(),
                1d,
                "m3",
                new[] { "source:bcf-smoke" });
        }

        private static DateTime Utc(int hour)
        {
            return new DateTime(2026, 8, 14, hour, 0, 0, DateTimeKind.Utc);
        }

        private static void ThrowsArgument(Action action, string message)
        {
            try { action(); }
            catch (ArgumentException) { return; }
            throw new Exception(message);
        }

        private static void ThrowsInvalidData(Action action, string message)
        {
            try { action(); }
            catch (System.IO.InvalidDataException) { return; }
            throw new Exception(message);
        }

        private static void Require(string text, string token)
        {
            if (!text.Contains(token)) throw new Exception("Expected BCF token: " + token);
        }
    }
}
