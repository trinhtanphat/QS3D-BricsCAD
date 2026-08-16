using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfZipPackageSmoke
    {
        private const string TopicA = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        private const string TopicB = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
        private const string Viewpoint = "cccccccc-cccc-cccc-cccc-cccccccccccc";
        private const string Comment = "dddddddd-dddd-dddd-dddd-dddddddddddd";
        private const string IfcA = "2MF28NhmDBiRVyFakgdbCT";
        private const string IfcB = "3$cshxZO9AJBebsni$z9Yk";

        public static void Run()
        {
            PackageIsByteDeterministicAndSchemaShaped();
            PackageRoundTripPreservesSemanticIdentity();
            UnsafeMalformedAndUnsupportedPackagesFailClosed();
            MalformedLeafStructureFailsClosed();
            NonCanonicalXmlNodeFormsFailClosed();
        }

        private static void PackageIsByteDeterministicAndSchemaShaped()
        {
            var exchange = BuildFixture();
            var first = BcfZipPackage.Write(exchange);
            var second = BcfZipPackage.Write(exchange);
            if (!first.SequenceEqual(second)) throw new Exception("BCF package bytes must be deterministic for unchanged semantic input.");

            using var stream = new MemoryStream(first, false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
            var paths = archive.Entries.Select(x => x.FullName).ToArray();
            var expected = new[]
            {
                "bcf.version",
                "extensions.xml",
                TopicA + "/markup.bcf",
                TopicB + "/markup.bcf",
                TopicB + "/" + Viewpoint + ".bcfv"
            };
            if (!paths.SequenceEqual(expected)) throw new Exception("BCF package entries are not emitted in canonical deterministic order.");

            var version = ReadEntry(archive, "bcf.version");
            Require(version, "VersionId=\"3.0\"");
            var extensions = ReadEntry(archive, "extensions.xml");
            Require(extensions, "<TopicType>Coordination</TopicType>");
            Require(extensions, "<TopicStatus>Open</TopicStatus>");
            var markup = ReadEntry(archive, TopicB + "/markup.bcf");
            Require(markup, "Guid=\"" + TopicB + "\"");
            Require(markup, "<CreationDate>2026-08-14T09:00:00.0000000Z</CreationDate>");
            Require(markup, "<CreationAuthor>qa@qs3d</CreationAuthor>");
            Require(markup, "<Viewpoint>" + Viewpoint + ".bcfv</Viewpoint>");
            var bcfv = ReadEntry(archive, TopicB + "/" + Viewpoint + ".bcfv");
            Require(bcfv, "Guid=\"" + Viewpoint + "\"");
            Require(bcfv, "IfcGuid=\"" + IfcA + "\"");
            Require(bcfv, "<OriginatingSystem>QS3D</OriginatingSystem>");
            Require(bcfv, "<AuthoringToolId>E-A</AuthoringToolId>");
            Require(bcfv, "<OrthogonalCamera>");
        }

        private static void PackageRoundTripPreservesSemanticIdentity()
        {
            var exchange = BuildFixture();
            var roundTrip = BcfZipPackage.Read(BcfZipPackage.Write(exchange));
            var expected = BcfIssueExchangeSerializer.Serialize(exchange);
            var actual = BcfIssueExchangeSerializer.Serialize(roundTrip);
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new Exception("BCF package round-trip changed semantic topic/comment/viewpoint identity or camera data.");
        }

        private static void UnsafeMalformedAndUnsupportedPackagesFailClosed()
        {
            ThrowsInvalidData(() => BcfZipPackage.Read(new byte[] { 1, 2, 3, 4 }), "Malformed ZIP payloads must fail closed.");

            var unsupported = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"2.1\" />",
                ["extensions.xml"] = ExtensionsXml()
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(unsupported), "Unsupported BCF versions must fail closed.");

            var unsafePath = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                ["extensions.xml"] = ExtensionsXml(),
                ["../evil"] = "x"
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(unsafePath), "Path traversal entries must fail closed.");

            var missingMarkup = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                ["extensions.xml"] = ExtensionsXml(),
                [TopicA + "/" + Viewpoint + ".bcfv"] = "<VisualizationInfo Guid=\"" + Viewpoint + "\"><Components><Selection /></Components><OrthogonalCamera><CameraViewPoint><X>0</X><Y>0</Y><Z>0</Z></CameraViewPoint><CameraDirection><X>0</X><Y>0</Y><Z>-1</Z></CameraDirection><CameraUpVector><X>0</X><Y>1</Y><Z>0</Z></CameraUpVector><ViewToWorldScale>1</ViewToWorldScale><AspectRatio>1</AspectRatio></OrthogonalCamera></VisualizationInfo>"
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(missingMarkup), "Topic folders without markup.bcf must fail closed.");

            var mismatchedFolder = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                ["extensions.xml"] = ExtensionsXml(),
                [TopicA + "/markup.bcf"] = "<Markup><Topic Guid=\"" + TopicB + "\" TopicType=\"Coordination\" TopicStatus=\"Open\"><Title>Mismatch</Title><CreationDate>2026-08-14T09:00:00Z</CreationDate><CreationAuthor>qa@qs3d</CreationAuthor></Topic></Markup>"
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(mismatchedFolder), "Topic folder and markup GUID mismatch must fail closed.");
        }

        private static void MalformedLeafStructureFailsClosed()
        {
            var attributedExtensionLeaf = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                ["extensions.xml"] = "<Extensions><TopicTypes><TopicType data-extra=\"1\">Coordination</TopicType></TopicTypes><TopicStatuses><TopicStatus>Open</TopicStatus></TopicStatuses></Extensions>",
                [TopicA + "/markup.bcf"] = MinimalMarkup("Unsafe extension leaf")
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(attributedExtensionLeaf), "BCF extension value leaves with attributes must fail closed.");

            var attributedLeaf = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                ["extensions.xml"] = ExtensionsXml(),
                [TopicA + "/markup.bcf"] = "<Markup><Topic Guid=\"" + TopicA + "\" TopicType=\"Coordination\" TopicStatus=\"Open\"><Title data-extra=\"1\">Unsafe</Title><CreationDate>2026-08-14T09:00:00Z</CreationDate><CreationAuthor>qa@qs3d</CreationAuthor></Topic></Markup>"
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(attributedLeaf), "BCF value leaves with attributes must fail closed.");

            var nestedLeaf = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                ["extensions.xml"] = ExtensionsXml(),
                [TopicA + "/markup.bcf"] = "<Markup><Topic Guid=\"" + TopicA + "\" TopicType=\"Coordination\" TopicStatus=\"Open\"><Title><Injected>Unsafe</Injected></Title><CreationDate>2026-08-14T09:00:00Z</CreationDate><CreationAuthor>qa@qs3d</CreationAuthor></Topic></Markup>"
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(nestedLeaf), "BCF value leaves with nested elements must fail closed.");
        }

        private static void NonCanonicalXmlNodeFormsFailClosed()
        {
            var cdataLeaf = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                ["extensions.xml"] = "<Extensions><TopicTypes><TopicType><![CDATA[Coordination]]></TopicType></TopicTypes><TopicStatuses><TopicStatus>Open</TopicStatus></TopicStatuses></Extensions>",
                [TopicA + "/markup.bcf"] = MinimalMarkup("CDATA leaf")
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(cdataLeaf), "BCF value leaves must reject CDATA node forms.");

            var commentedLeaf = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                ["extensions.xml"] = ExtensionsXml(),
                [TopicA + "/markup.bcf"] = MinimalMarkup("Can<!--unexpected-->onical")
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(commentedLeaf), "BCF value leaves must reject comment-mixed text.");

            var processingInstructionLeaf = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                ["extensions.xml"] = ExtensionsXml(),
                [TopicA + "/markup.bcf"] = MinimalMarkup("<?qs3d unexpected?>Unsafe")
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(processingInstructionLeaf), "BCF value leaves must reject processing instructions.");

            var mixedContainer = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                ["extensions.xml"] = "<Extensions><TopicTypes>unexpected<TopicType>Coordination</TopicType></TopicTypes><TopicStatuses><TopicStatus>Open</TopicStatus></TopicStatuses></Extensions>",
                [TopicA + "/markup.bcf"] = MinimalMarkup("Mixed container")
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(mixedContainer), "BCF element-only containers must reject non-whitespace mixed text.");

            var attributedTokenContainer = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                ["extensions.xml"] = "<Extensions><TopicTypes data-extra=\"1\"><TopicType>Coordination</TopicType></TopicTypes><TopicStatuses><TopicStatus>Open</TopicStatus></TopicStatuses></Extensions>",
                [TopicA + "/markup.bcf"] = MinimalMarkup("Attributed token container")
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(attributedTokenContainer), "BCF extension token containers must reject unsupported attributes.");

            var nonEmptyVersion = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\"> </Version>",
                ["extensions.xml"] = ExtensionsXml(),
                [TopicA + "/markup.bcf"] = MinimalMarkup("Non-empty version")
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(nonEmptyVersion), "BCF empty elements must reject all content nodes, including whitespace.");

            var documentComment = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<!--unexpected--><Version VersionId=\"3.0\" />",
                ["extensions.xml"] = ExtensionsXml(),
                [TopicA + "/markup.bcf"] = MinimalMarkup("Document comment")
            });
            ThrowsInvalidData(() => BcfZipPackage.Read(documentComment), "BCF XML documents must reject non-root comments and processing content.");
        }

        private static BcfIssueExchange BuildFixture()
        {
            var camera = new BcfOrthogonalCamera(new BcfPoint3(10d, 20d, 30d), new BcfPoint3(0d, 0d, -1d), new BcfPoint3(0d, 1d, 0d), 25d, 1.5d);
            var viewpoint = new BcfViewpoint(
                Viewpoint,
                camera,
                new[] { new BcfComponentReference("E-B", IfcB), new BcfComponentReference("E-A", IfcA) });
            var comment = new BcfComment(Comment, "review@qs3d", Utc(10), "Review the selected components.", Viewpoint);
            var topicB = new BcfTopic(TopicB, "BCF package", "Open", "Coordination", "Package round-trip.", "qa@qs3d", Utc(9), new[] { comment }, new[] { viewpoint });
            var topicA = new BcfTopic(TopicA, "No viewpoint", "Closed", "Information", string.Empty, "qa@qs3d", Utc(8), Array.Empty<BcfComment>(), Array.Empty<BcfViewpoint>());
            return BcfIssueExchange.Create(new[] { topicB, topicA });
        }

        private static byte[] BuildRawPackage(Dictionary<string, string> entries)
        {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                foreach (var pair in entries)
                {
                    var entry = archive.CreateEntry(pair.Key, CompressionLevel.NoCompression);
                    using var output = entry.Open();
                    var bytes = Encoding.UTF8.GetBytes(pair.Value);
                    output.Write(bytes, 0, bytes.Length);
                }
            }
            return stream.ToArray();
        }

        private static string ExtensionsXml()
        {
            return "<Extensions><TopicTypes><TopicType>Coordination</TopicType></TopicTypes><TopicStatuses><TopicStatus>Open</TopicStatus></TopicStatuses></Extensions>";
        }

        private static string MinimalMarkup(string titleXml)
        {
            return "<Markup><Topic Guid=\"" + TopicA + "\" TopicType=\"Coordination\" TopicStatus=\"Open\"><Title>" + titleXml + "</Title><CreationDate>2026-08-14T09:00:00Z</CreationDate><CreationAuthor>qa@qs3d</CreationAuthor></Topic></Markup>";
        }

        private static string ReadEntry(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path) ?? throw new Exception("Expected BCF package entry: " + path);
            using var stream = entry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            return reader.ReadToEnd();
        }

        private static DateTime Utc(int hour) => new DateTime(2026, 8, 14, hour, 0, 0, DateTimeKind.Utc);

        private static void ThrowsInvalidData(Action action, string message)
        {
            try { action(); }
            catch (InvalidDataException) { return; }
            throw new Exception(message);
        }

        private static void Require(string text, string token)
        {
            if (!text.Contains(token)) throw new Exception("Expected BCF package token: " + token);
        }
    }
}
