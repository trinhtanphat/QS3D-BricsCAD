using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfZipNumericCanonicalitySmoke
    {
        private const string TopicId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        private const string ViewpointId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

        internal static void Run()
        {
            CanonicalPackageRoundTrips();
            RejectsExponentAlias();
            RejectsRedundantDecimalAlias();
            RejectsLeadingPlusAlias();
            RejectsSurroundingWhitespaceAlias();
        }

        private static void CanonicalPackageRoundTrips()
        {
            var package = BuildCanonicalPackage();
            var exchange = BcfZipPackage.Read(package);
            if (exchange.Topics.Count != 1 || exchange.Topics[0].Viewpoints.Count != 1)
                throw new Exception("Canonical BCF ZIP numeric payload did not round-trip.");

            var rewritten = BcfZipPackage.Write(exchange);
            if (!package.SequenceEqual(rewritten))
                throw new Exception("Canonical BCF ZIP payload changed across read/write round-trip.");
        }

        private static void RejectsExponentAlias()
        {
            ThrowsInvalidData(
                () => BcfZipPackage.Read(ReplaceAspectRatio("1e0")),
                "BCF ZIP numeric exponent aliases must fail closed.");
        }

        private static void RejectsRedundantDecimalAlias()
        {
            ThrowsInvalidData(
                () => BcfZipPackage.Read(ReplaceAspectRatio("1.0")),
                "BCF ZIP redundant decimal aliases must fail closed.");
        }

        private static void RejectsLeadingPlusAlias()
        {
            ThrowsInvalidData(
                () => BcfZipPackage.Read(ReplaceAspectRatio("+1")),
                "BCF ZIP leading-plus numeric aliases must fail closed.");
        }

        private static void RejectsSurroundingWhitespaceAlias()
        {
            ThrowsInvalidData(
                () => BcfZipPackage.Read(ReplaceAspectRatio(" 1 ")),
                "BCF ZIP numeric whitespace aliases must fail closed.");
        }

        private static byte[] BuildCanonicalPackage()
        {
            var camera = new BcfOrthogonalCamera(
                new BcfPoint3(0d, 0d, 0d),
                new BcfPoint3(0d, 0d, -1d),
                new BcfPoint3(0d, 1d, 0d),
                1d,
                1d);
            var viewpoint = new BcfViewpoint(
                ViewpointId,
                camera,
                Array.Empty<BcfComponentReference>());
            var topic = new BcfTopic(
                TopicId,
                "Numeric canonicality",
                "Open",
                "Coordination",
                string.Empty,
                "qa@qs3d",
                new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc),
                Array.Empty<BcfComment>(),
                new[] { viewpoint });
            return BcfZipPackage.Write(BcfIssueExchange.Create(new[] { topic }));
        }

        private static byte[] ReplaceAspectRatio(string replacement)
        {
            var canonical = BuildCanonicalPackage();
            var entries = new List<KeyValuePair<string, string>>();
            var replaced = false;

            using (var input = new MemoryStream(canonical, false))
            using (var archive = new ZipArchive(input, ZipArchiveMode.Read, false))
            {
                foreach (var entry in archive.Entries)
                {
                    string text;
                    using (var reader = new StreamReader(entry.Open(), new UTF8Encoding(false, true), true))
                        text = reader.ReadToEnd();

                    if (entry.FullName.EndsWith(".bcfv", StringComparison.Ordinal))
                    {
                        var next = text.Replace("<AspectRatio>1</AspectRatio>", "<AspectRatio>" + replacement + "</AspectRatio>");
                        if (!string.Equals(next, text, StringComparison.Ordinal)) replaced = true;
                        text = next;
                    }

                    entries.Add(new KeyValuePair<string, string>(entry.FullName, text));
                }
            }

            if (!replaced) throw new Exception("BCF ZIP numeric regression fixture did not locate canonical AspectRatio text.");

            using var output = new MemoryStream();
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
            {
                foreach (var entry in entries)
                {
                    var created = archive.CreateEntry(entry.Key, CompressionLevel.NoCompression);
                    using var stream = created.Open();
                    var bytes = new UTF8Encoding(false, true).GetBytes(entry.Value);
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            return output.ToArray();
        }

        private static void ThrowsInvalidData(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidDataException)
            {
                return;
            }
            throw new Exception(message);
        }
    }
}
