using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfZipNestedCardinalitySmoke
    {
        private const string Topic = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        private const int MaxViewpointsPerTopic = 256;
        private const int MaxCommentsPerTopic = 1024;
        private const int MaxComponentsPerViewpoint = 1000;

        internal static void Run()
        {
            CommentOverflowFailsAtParseBoundary();
            ViewpointOverflowFailsBeforeFileResolution();
            ComponentOverflowFailsAtParseBoundary();
            ExactCardinalityBoundariesRemainAccepted();
        }

        private static void CommentOverflowFailsAtParseBoundary()
        {
            var package = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                [Topic + "/markup.bcf"] = BuildMarkupWithComments(MaxCommentsPerTopic + 1)
            });

            ThrowsInvalidDataMessage(
                () => BcfZipPackage.Read(package),
                "BCF comment count exceeds the bounded package contract.",
                "BCF ZIP comment overflow must fail at the parser cardinality boundary.");
        }

        private static void ViewpointOverflowFailsBeforeFileResolution()
        {
            var package = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                [Topic + "/markup.bcf"] = BuildMarkupWithViewpoints(MaxViewpointsPerTopic + 1)
            });

            ThrowsInvalidDataMessage(
                () => BcfZipPackage.Read(package),
                "BCF viewpoint count exceeds the bounded package contract.",
                "BCF ZIP viewpoint overflow must fail before referenced .bcfv file resolution.");
        }

        private static void ComponentOverflowFailsAtParseBoundary()
        {
            var viewpointId = GuidAt(1);
            var package = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                [Topic + "/markup.bcf"] = BuildMarkupWithViewpointIds(new[] { viewpointId }),
                [Topic + "/" + viewpointId + ".bcfv"] = BuildViewpoint(viewpointId, MaxComponentsPerViewpoint + 1)
            });

            ThrowsInvalidDataMessage(
                () => BcfZipPackage.Read(package),
                "BCF viewpoint component count exceeds the bounded package contract.",
                "BCF ZIP component overflow must fail at the parser cardinality boundary.");
        }

        private static void ExactCardinalityBoundariesRemainAccepted()
        {
            var comments = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                [Topic + "/markup.bcf"] = BuildMarkupWithComments(MaxCommentsPerTopic)
            });
            var commentExchange = BcfZipPackage.Read(comments);
            if (commentExchange.Topics.Count != 1 || commentExchange.Topics[0].Comments.Count != MaxCommentsPerTopic)
                throw new Exception("BCF ZIP reader must accept the exact comment cardinality boundary.");

            var componentViewpointId = GuidAt(1);
            var components = BuildRawPackage(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />",
                [Topic + "/markup.bcf"] = BuildMarkupWithViewpointIds(new[] { componentViewpointId }),
                [Topic + "/" + componentViewpointId + ".bcfv"] = BuildViewpoint(componentViewpointId, MaxComponentsPerViewpoint)
            });
            var componentExchange = BcfZipPackage.Read(components);
            if (componentExchange.Topics.Count != 1 || componentExchange.Topics[0].Viewpoints.Count != 1 || componentExchange.Topics[0].Viewpoints[0].Components.Count != MaxComponentsPerViewpoint)
                throw new Exception("BCF ZIP reader must accept the exact component cardinality boundary.");

            var entries = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bcf.version"] = "<Version VersionId=\"3.0\" />"
            };
            var viewpointIds = new string[MaxViewpointsPerTopic];
            for (var index = 0; index < viewpointIds.Length; index++)
            {
                var id = GuidAt(index + 1);
                viewpointIds[index] = id;
                entries[Topic + "/" + id + ".bcfv"] = BuildViewpoint(id, 0);
            }
            entries[Topic + "/markup.bcf"] = BuildMarkupWithViewpointIds(viewpointIds);
            var viewpointExchange = BcfZipPackage.Read(BuildRawPackage(entries));
            if (viewpointExchange.Topics.Count != 1 || viewpointExchange.Topics[0].Viewpoints.Count != MaxViewpointsPerTopic)
                throw new Exception("BCF ZIP reader must accept the exact viewpoint cardinality boundary.");
        }

        private static string BuildMarkupWithComments(int count)
        {
            var comments = new StringBuilder();
            comments.Append("<Comments>");
            for (var index = 0; index < count; index++)
            {
                comments.Append("<Comment Guid=\"").Append(GuidAt(index + 1)).Append("\">")
                    .Append("<Date>2026-08-14T10:00:00.0000000Z</Date>")
                    .Append("<Author>qa@qs3d</Author>")
                    .Append("<Comment>bounded comment</Comment>")
                    .Append("</Comment>");
            }
            comments.Append("</Comments>");
            return BuildMarkup(comments.ToString());
        }

        private static string BuildMarkupWithViewpoints(int count)
        {
            var ids = new string[count];
            for (var index = 0; index < count; index++) ids[index] = GuidAt(index + 1);
            return BuildMarkupWithViewpointIds(ids);
        }

        private static string BuildMarkupWithViewpointIds(IReadOnlyList<string> ids)
        {
            var viewpoints = new StringBuilder();
            viewpoints.Append("<Viewpoints>");
            for (var index = 0; index < ids.Count; index++)
            {
                var id = ids[index];
                viewpoints.Append("<ViewPoint Guid=\"").Append(id).Append("\"><Viewpoint>")
                    .Append(id).Append(".bcfv</Viewpoint></ViewPoint>");
            }
            viewpoints.Append("</Viewpoints>");
            return BuildMarkup(viewpoints.ToString());
        }

        private static string BuildMarkup(string nestedXml)
        {
            return "<Markup><Topic Guid=\"" + Topic + "\" TopicType=\"Coordination\" TopicStatus=\"Open\">" +
                   "<Title>Nested cardinality</Title>" +
                   "<CreationDate>2026-08-14T09:00:00.0000000Z</CreationDate>" +
                   "<CreationAuthor>qa@qs3d</CreationAuthor>" +
                   nestedXml +
                   "</Topic></Markup>";
        }

        private static string BuildViewpoint(string id, int componentCount)
        {
            var xml = new StringBuilder();
            xml.Append("<VisualizationInfo Guid=\"").Append(id).Append("\"><Components><Selection>");
            for (var index = 0; index < componentCount; index++)
            {
                xml.Append("<Component IfcGuid=\"").Append(index.ToString("D22", CultureInfo.InvariantCulture)).Append("\">")
                    .Append("<OriginatingSystem>QS3D</OriginatingSystem>")
                    .Append("<AuthoringToolId>E-").Append(index.ToString(CultureInfo.InvariantCulture)).Append("</AuthoringToolId>")
                    .Append("</Component>");
            }
            xml.Append("</Selection></Components>")
                .Append("<OrthogonalCamera>")
                .Append("<CameraViewPoint><X>0</X><Y>0</Y><Z>0</Z></CameraViewPoint>")
                .Append("<CameraDirection><X>0</X><Y>0</Y><Z>-1</Z></CameraDirection>")
                .Append("<CameraUpVector><X>0</X><Y>1</Y><Z>0</Z></CameraUpVector>")
                .Append("<ViewToWorldScale>1</ViewToWorldScale><AspectRatio>1</AspectRatio>")
                .Append("</OrthogonalCamera></VisualizationInfo>");
            return xml.ToString();
        }

        private static string GuidAt(int index)
        {
            return index.ToString("x8", CultureInfo.InvariantCulture) + "-0000-0000-0000-000000000000";
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

        private static void ThrowsInvalidDataMessage(Action action, string expectedMessage, string failureMessage)
        {
            try
            {
                action();
            }
            catch (InvalidDataException exception)
            {
                if (!string.Equals(exception.Message, expectedMessage, StringComparison.Ordinal))
                    throw new Exception(failureMessage + " Actual: " + exception.Message, exception);
                return;
            }
            throw new Exception(failureMessage);
        }
    }
}
