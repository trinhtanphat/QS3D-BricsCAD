using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfZipTimestampCanonicalitySmoke
    {
        private const string TopicId = "11111111-1111-1111-1111-111111111111";
        private const string CommentId = "22222222-2222-2222-2222-222222222222";
        private static readonly DateTime TopicUtc = new DateTime(2026, 8, 16, 4, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime CommentUtc = TopicUtc.AddMinutes(1);

        internal static void Run()
        {
            RejectsOffsetTopicTimestamp();
            RejectsOffsetCommentTimestamp();
            RejectsNonCanonicalUtcTimestamp();
            CanonicalPackageRoundTripsExactly();
        }

        private static void RejectsOffsetTopicTimestamp()
        {
            var canonical = BuildPackage();
            var canonicalText = TopicUtc.ToString("O", CultureInfo.InvariantCulture);
            var offsetText = new DateTimeOffset(TopicUtc).ToOffset(TimeSpan.FromHours(7d)).ToString("O", CultureInfo.InvariantCulture);
            var mutated = RewriteMarkup(canonical, value => ReplaceExactlyOnce(value, "<CreationDate>" + canonicalText + "</CreationDate>", "<CreationDate>" + offsetText + "</CreationDate>"));
            ThrowsInvalidData(() => BcfZipPackage.Read(mutated), "BCF ZIP topic timestamps with explicit offsets must fail closed.");
        }

        private static void RejectsOffsetCommentTimestamp()
        {
            var canonical = BuildPackage();
            var canonicalText = CommentUtc.ToString("O", CultureInfo.InvariantCulture);
            var offsetText = new DateTimeOffset(CommentUtc).ToOffset(TimeSpan.FromHours(7d)).ToString("O", CultureInfo.InvariantCulture);
            var mutated = RewriteMarkup(canonical, value => ReplaceExactlyOnce(value, "<Date>" + canonicalText + "</Date>", "<Date>" + offsetText + "</Date>"));
            ThrowsInvalidData(() => BcfZipPackage.Read(mutated), "BCF ZIP comment timestamps with explicit offsets must fail closed.");
        }

        private static void RejectsNonCanonicalUtcTimestamp()
        {
            var canonical = BuildPackage();
            var canonicalText = TopicUtc.ToString("O", CultureInfo.InvariantCulture);
            var shortened = TopicUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            var mutated = RewriteMarkup(canonical, value => ReplaceExactlyOnce(value, "<CreationDate>" + canonicalText + "</CreationDate>", "<CreationDate>" + shortened + "</CreationDate>"));
            ThrowsInvalidData(() => BcfZipPackage.Read(mutated), "BCF ZIP shortened/non-canonical UTC timestamps must fail closed.");
        }

        private static void CanonicalPackageRoundTripsExactly()
        {
            var canonical = BuildPackage();
            var loaded = BcfZipPackage.Read(canonical);
            if (loaded.Topics.Count != 1) throw new Exception("Canonical BCF ZIP timestamp round-trip changed topic count.");
            var topic = loaded.Topics[0];
            if (topic.CreationDateUtc != TopicUtc || topic.CreationDateUtc.Kind != DateTimeKind.Utc)
                throw new Exception("Canonical BCF ZIP topic timestamp changed across read.");
            if (topic.Comments.Count != 1 || topic.Comments[0].CreatedUtc != CommentUtc || topic.Comments[0].CreatedUtc.Kind != DateTimeKind.Utc)
                throw new Exception("Canonical BCF ZIP comment timestamp changed across read.");
            var rewritten = BcfZipPackage.Write(loaded);
            if (!rewritten.SequenceEqual(canonical)) throw new Exception("Canonical BCF ZIP bytes changed across timestamp round-trip.");
        }

        private static byte[] BuildPackage()
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
            return BcfZipPackage.Write(BcfIssueExchange.Create(new[] { topic }));
        }

        private static byte[] RewriteMarkup(byte[] package, Func<string, string> rewrite)
        {
            using var inputStream = new MemoryStream(package, false);
            using var input = new ZipArchive(inputStream, ZipArchiveMode.Read, false);
            using var outputStream = new MemoryStream();
            using (var output = new ZipArchive(outputStream, ZipArchiveMode.Create, true))
            {
                foreach (var source in input.Entries)
                {
                    var target = output.CreateEntry(source.FullName, CompressionLevel.NoCompression);
                    using var sourceStream = source.Open();
                    using var reader = new StreamReader(sourceStream, new UTF8Encoding(false, true), true);
                    var text = reader.ReadToEnd();
                    if (string.Equals(source.FullName, TopicId + "/markup.bcf", StringComparison.Ordinal)) text = rewrite(text);
                    using var targetStream = target.Open();
                    var bytes = new UTF8Encoding(false, true).GetBytes(text);
                    targetStream.Write(bytes, 0, bytes.Length);
                }
            }
            return outputStream.ToArray();
        }

        private static string ReplaceExactlyOnce(string value, string oldValue, string newValue)
        {
            var first = value.IndexOf(oldValue, StringComparison.Ordinal);
            if (first < 0) throw new InvalidOperationException("Expected canonical BCF ZIP timestamp was not found.");
            if (value.IndexOf(oldValue, first + oldValue.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException("Expected canonical BCF ZIP timestamp was not unique.");
            return value.Substring(0, first) + newValue + value.Substring(first + oldValue.Length);
        }

        private static void ThrowsInvalidData(Action action, string message)
        {
            try { action(); }
            catch (InvalidDataException) { return; }
            throw new Exception(message);
        }
    }
}
