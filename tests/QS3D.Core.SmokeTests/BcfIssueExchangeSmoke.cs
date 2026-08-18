using System;
using System.Linq;
using System.Xml.Linq;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfIssueExchangeSmoke
    {
        private const string TopicA = "11111111-1111-1111-1111-111111111111";
        private const string TopicB = "22222222-2222-2222-2222-222222222222";
        private const string Viewpoint = "33333333-3333-3333-3333-333333333333";
        private const string Comment1 = "44444444-4444-4444-4444-444444444444";
        private const string Comment2 = "55555555-5555-5555-5555-555555555555";
        private const string IfcA = "2MF28NhmDBiRVyFakgdbCT";
        private const string IfcB = "3$cshxZO9AJBebsni$z9Yk";

        public static void Run()
        {
            SerializationIsDeterministicAcrossInputOrdering();
            RoundTripPreservesTopicCommentViewpointCameraAndIfcIdentity();
            BuildingSmartIdentityAndCameraShapesFailClosed();
            DanglingAndDuplicateReferencesFailClosed();
            MalformedPayloadFailsClosed();
            AmbiguousXmlStructureFailsClosed();
        }

        private static void SerializationIsDeterministicAcrossInputOrdering()
        {
            var forward = BcfIssueExchangeSerializer.Serialize(BuildFixture(false));
            var reversed = BcfIssueExchangeSerializer.Serialize(BuildFixture(true));
            if (!string.Equals(forward, reversed, StringComparison.Ordinal))
                throw new Exception("BCF issue serialization must be deterministic for semantically equivalent input ordering.");
            Require(forward, "schemaVersion=\"3.0\"");
            Require(forward, "creationAuthor=\"qa@qs3d\"");
            Require(forward, "qs3dElementId=\"E-A\"");
            Require(forward, "ifcGlobalId=\"" + IfcA + "\"");
        }

        private static void RoundTripPreservesTopicCommentViewpointCameraAndIfcIdentity()
        {
            var original = BuildFixture(true);
            var serialized = BcfIssueExchangeSerializer.Serialize(original);
            var roundTrip = BcfIssueExchangeSerializer.Deserialize(serialized);
            var serializedAgain = BcfIssueExchangeSerializer.Serialize(roundTrip);
            if (!string.Equals(serialized, serializedAgain, StringComparison.Ordinal))
                throw new Exception("BCF issue payload did not round-trip deterministically.");

            if (roundTrip.Topics.Count != 2) throw new Exception("BCF topic count changed during round-trip.");
            if (!string.Equals(roundTrip.Topics[0].Id, TopicA, StringComparison.Ordinal)) throw new Exception("BCF topics were not canonicalized by stable identity.");
            var topic = roundTrip.Topics[1];
            if (!string.Equals(topic.CreationAuthor, "qa@qs3d", StringComparison.Ordinal) || topic.CreationDateUtc != Utc(9))
                throw new Exception("BCF required topic creation metadata was not preserved.");
            if (topic.Viewpoints.Count != 1 || topic.Comments.Count != 2) throw new Exception("BCF viewpoint/comment payload changed during round-trip.");
            if (!string.Equals(topic.Comments[0].Id, Comment1, StringComparison.Ordinal)) throw new Exception("BCF comments were not canonicalized deterministically.");
            if (!string.Equals(topic.Comments[0].ViewpointId, Viewpoint, StringComparison.Ordinal)) throw new Exception("BCF comment viewpoint reference was not preserved.");

            var camera = topic.Viewpoints[0].Camera;
            if (camera.ViewPoint.X != 10d || camera.ViewPoint.Y != 20d || camera.ViewPoint.Z != 30d || camera.ViewToWorldScale != 25d || camera.AspectRatio != 1.5d)
                throw new Exception("BCF camera values were not preserved.");
            var components = topic.Viewpoints[0].Components;
            if (components.Count != 2) throw new Exception("BCF component references changed during round-trip.");
            if (!string.Equals(components[0].Qs3dElementId, "E-A", StringComparison.Ordinal) || !string.Equals(components[0].IfcGlobalId, IfcA, StringComparison.Ordinal))
                throw new Exception("BCF component identity bridge did not preserve QS3D and IFC identities together.");
        }

        private static void BuildingSmartIdentityAndCameraShapesFailClosed()
        {
            ThrowsArgument(() => new BcfViewpoint("VP-NOT-A-GUID", Camera(), Array.Empty<BcfComponentReference>()), "BCF topic/comment/viewpoint identifiers must use buildingSMART canonical GUID form.");
            ThrowsArgument(() => new BcfComponentReference("E-X", "IFC-NOT-COMPRESSED"), "BCF component IFC identities must use the 22-character buildingSMART IfcGuid shape.");
            ThrowsArgument(() => new BcfOrthogonalCamera(new BcfPoint3(0d, 0d, 0d), new BcfPoint3(0d, 0d, 0d), new BcfPoint3(0d, 1d, 0d), 1d, 1d), "BCF camera direction must be explicit and non-zero.");
            ThrowsArgument(() => new BcfOrthogonalCamera(new BcfPoint3(0d, 0d, 0d), new BcfPoint3(1d, 2d, 3d), new BcfPoint3(2d, 4d, 6d), 1d, 1d), "Parallel BCF camera direction/up vectors must fail closed.");
            ThrowsArgument(() => new BcfOrthogonalCamera(new BcfPoint3(0d, 0d, 0d), new BcfPoint3(1d, 2d, 3d), new BcfPoint3(-2d, -4d, -6d), 1d, 1d), "Anti-parallel BCF camera direction/up vectors must fail closed.");
            ThrowsArgument(() => new BcfOrthogonalCamera(new BcfPoint3(0d, 0d, 0d), new BcfPoint3(double.MaxValue, double.MaxValue, 0d), new BcfPoint3(double.MaxValue, double.MaxValue, 0d), 1d, 1d), "Overflow-prone collinear BCF camera vectors must fail closed.");

            var tinyNonCollinear = new BcfOrthogonalCamera(
                new BcfPoint3(0d, 0d, 0d),
                new BcfPoint3(double.Epsilon, 0d, 0d),
                new BcfPoint3(0d, double.Epsilon, 0d),
                1d,
                1d);
            if (tinyNonCollinear.Direction.X != double.Epsilon || tinyNonCollinear.UpVector.Y != double.Epsilon)
                throw new Exception("Underflow-prone but non-collinear BCF camera vectors must remain valid.");

            ThrowsArgument(() => new BcfTopic(TopicA, "Bad UTC", "Open", "Error", string.Empty, "qa@qs3d", DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local), Array.Empty<BcfComment>(), Array.Empty<BcfViewpoint>()), "BCF topic creation timestamps must be UTC.");
        }

        private static void DanglingAndDuplicateReferencesFailClosed()
        {
            ThrowsArgument(
                () => new BcfTopic(
                    "66666666-6666-6666-6666-666666666666",
                    "Broken topic",
                    "Open",
                    "Error",
                    string.Empty,
                    "qa@qs3d",
                    Utc(9),
                    new[] { new BcfComment("77777777-7777-7777-7777-777777777777", "qa@qs3d", Utc(10), "Dangling", "88888888-8888-8888-8888-888888888888") },
                    Array.Empty<BcfViewpoint>()),
                "Dangling BCF comment viewpoint references must fail closed.");

            ThrowsArgument(
                () => new BcfViewpoint(
                    "99999999-9999-9999-9999-999999999999",
                    Camera(),
                    new[] { new BcfComponentReference("E-DUP", "0AAAAAAAAAAAAAAAAAAAAA"), new BcfComponentReference("e-dup", "1BBBBBBBBBBBBBBBBBBBBB") }),
                "Case-insensitive duplicate QS3D component identities must fail closed.");
        }

        private static void MalformedPayloadFailsClosed()
        {
            ThrowsInvalidData(() => BcfIssueExchangeSerializer.Deserialize("<BcfIssueExchange schemaVersion=\"2.1\"></BcfIssueExchange>"), "Unsupported BCF schema versions must fail closed.");
            ThrowsInvalidData(() => BcfIssueExchangeSerializer.Deserialize("<broken"), "Malformed BCF XML must fail closed.");
        }

        private static void AmbiguousXmlStructureFailsClosed()
        {
            var valid = BcfIssueExchangeSerializer.Serialize(BuildFixture(false));
            var title = "<Title>Canonical ordering</Title>";

            ThrowsInvalidData(
                () => BcfIssueExchangeSerializer.Deserialize(valid.Replace(title, title + title)),
                "Duplicate BCF singleton elements must fail closed.");
            ThrowsInvalidData(
                () => BcfIssueExchangeSerializer.Deserialize(valid.Replace(title, "<Title data-extra=\"1\">Canonical ordering</Title>")),
                "Attributed BCF scalar leaves must fail closed.");
            ThrowsInvalidData(
                () => BcfIssueExchangeSerializer.Deserialize(valid.Replace(title, "<Title><Injected>Canonical ordering</Injected></Title>")),
                "Nested BCF scalar leaves must fail closed.");
            ThrowsInvalidData(
                () => BcfIssueExchangeSerializer.Deserialize(valid.Replace("<Viewpoints>", "<Unknown /><Viewpoints>")),
                "Unknown BCF elements must fail closed.");
            ThrowsInvalidData(
                () => BcfIssueExchangeSerializer.Deserialize(valid.Replace("schemaVersion=\"3.0\"", "schemaVersion=\"3.0\" data-extra=\"1\"")),
                "Unknown BCF attributes must fail closed.");
            ThrowsInvalidData(
                () => BcfIssueExchangeSerializer.Deserialize(valid.Replace("<BcfIssueExchange ", "<BcfIssueExchange xmlns=\"urn:unexpected\" ")),
                "Namespaced BCF exchange payloads must fail closed.");

            var mixedContainer = XDocument.Parse(valid);
            mixedContainer.Root!.Elements("Topic").First().Element("Viewpoints")!.AddFirst(new XText("unexpected-text"));
            ThrowsInvalidData(
                () => BcfIssueExchangeSerializer.Deserialize(mixedContainer.ToString(SaveOptions.DisableFormatting)),
                "Mixed text inside BCF element-only containers must fail closed.");

            var nonEmptyComponent = XDocument.Parse(valid);
            nonEmptyComponent.Descendants("Component").First().Add(new XText("unexpected-text"));
            ThrowsInvalidData(
                () => BcfIssueExchangeSerializer.Deserialize(nonEmptyComponent.ToString(SaveOptions.DisableFormatting)),
                "BCF point/component empty elements must reject text content.");

            var cdataScalar = XDocument.Parse(valid);
            var scalar = cdataScalar.Descendants("Title").First();
            scalar.ReplaceNodes(new XCData(scalar.Value));
            ThrowsInvalidData(
                () => BcfIssueExchangeSerializer.Deserialize(cdataScalar.ToString(SaveOptions.DisableFormatting)),
                "BCF scalar leaves must reject CDATA/non-canonical node forms.");
        }

        private static BcfIssueExchange BuildFixture(bool reverse)
        {
            var componentA = BcfComponentReference.FromIfcProjection(BuildProjection("E-A", IfcA));
            var componentB = BcfComponentReference.FromIfcProjection(BuildProjection("E-B", IfcB));
            var viewpoint = new BcfViewpoint(Viewpoint, Camera(), reverse ? new[] { componentB, componentA } : new[] { componentA, componentB });
            var comment1 = new BcfComment(Comment1, "qa@qs3d", Utc(10), "Check element identity.", Viewpoint);
            var comment2 = new BcfComment(Comment2, "review@qs3d", Utc(11), "Identity confirmed.", null);
            var topicB = new BcfTopic(
                TopicB,
                "BCF identity bridge",
                "Open",
                "Coordination",
                "QS3D and IFC identities must survive BCF issue exchange.",
                "qa@qs3d",
                Utc(9),
                reverse ? new[] { comment2, comment1 } : new[] { comment1, comment2 },
                new[] { viewpoint });
            var topicA = new BcfTopic(TopicA, "Canonical ordering", "Closed", "Information", string.Empty, "qa@qs3d", Utc(8), Array.Empty<BcfComment>(), Array.Empty<BcfViewpoint>());
            return BcfIssueExchange.Create(reverse ? new[] { topicB, topicA } : new[] { topicA, topicB });
        }

        private static BcfOrthogonalCamera Camera()
        {
            return new BcfOrthogonalCamera(new BcfPoint3(10d, 20d, 30d), new BcfPoint3(0d, 0d, -1d), new BcfPoint3(0d, 1d, 0d), 25d, 1.5d);
        }

        private static IfcRoundTripProjection BuildProjection(string qs3dElementId, string ifcGlobalId)
        {
            return new IfcRoundTripProjection(qs3dElementId, ifcGlobalId, "Beam", Array.Empty<IfcRoundTripNumericProperty>(), 1d, "m3", new[] { "source:bcf-smoke" });
        }

        private static DateTime Utc(int hour) => new DateTime(2026, 8, 14, hour, 0, 0, DateTimeKind.Utc);

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
