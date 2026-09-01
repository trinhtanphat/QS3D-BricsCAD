using System;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfXmlEntryWriteBoundSmoke
    {
        internal static void Run()
        {
            OversizedMarkupFailsClosedAtEntryCeiling();
            OrdinaryMarkupStillRoundTrips();
        }

        private static void OversizedMarkupFailsClosedAtEntryCeiling()
        {
            var comments = new List<BcfComment>();
            var text = new string('X', 4096);
            for (var index = 1; index <= 600; index++)
            {
                comments.Add(new BcfComment(
                    "10000000-0000-0000-0000-" + index.ToString("D12"),
                    "qa@qs3d",
                    new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    text,
                    null));
            }

            var topic = new BcfTopic(
                "20000000-0000-0000-0000-000000000001",
                "Oversized markup",
                "Open",
                "Coordination",
                string.Empty,
                "qa@qs3d",
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                comments,
                Array.Empty<BcfViewpoint>());

            try
            {
                BcfZipPackage.Write(BcfIssueExchange.Create(new[] { topic }));
            }
            catch (InvalidDataException exception)
            {
                if (exception.Message.StartsWith("BCF package entry exceeds the bounded size:", StringComparison.Ordinal))
                    return;
                throw new Exception("Oversized BCF markup must fail through the canonical entry-size contract.", exception);
            }

            throw new Exception("Aggregate-valid BCF markup above MaxEntryBytes must fail closed.");
        }

        private static void OrdinaryMarkupStillRoundTrips()
        {
            const string topicId = "30000000-0000-0000-0000-000000000001";
            var topic = new BcfTopic(
                topicId,
                "Canonical topic",
                "Open",
                "Coordination",
                "Canonical description",
                "qa@qs3d",
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                Array.Empty<BcfComment>(),
                Array.Empty<BcfViewpoint>());

            var package = BcfZipPackage.Write(BcfIssueExchange.Create(new[] { topic }));
            var roundTrip = BcfZipPackage.Read(package);
            if (roundTrip.Topics.Count != 1 || !string.Equals(roundTrip.Topics[0].Id, topicId, StringComparison.Ordinal))
                throw new Exception("Bounded BCF XML entry serialization changed canonical topic identity.");
        }
    }
}
