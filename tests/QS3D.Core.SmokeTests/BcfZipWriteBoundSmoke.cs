using System;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfZipWriteBoundSmoke
    {
        private const int MaxAdmittedTopics = 256;

        internal static void Run()
        {
            AggregatePackageCrossingArchiveCeilingFailsClosed();
            OrdinaryPackageStillRoundTrips();
        }

        private static void AggregatePackageCrossingArchiveCeilingFailsClosed()
        {
            var topics = new List<BcfTopic>();
            var title = new string('T', 40000);
            var description = new string('D', 40000);
            for (var index = 1; index <= MaxAdmittedTopics; index++)
            {
                topics.Add(new BcfTopic(
                    "00000000-0000-0000-0000-" + index.ToString("D12"),
                    title,
                    "Open",
                    "Coordination",
                    description,
                    "qa@qs3d",
                    new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    Array.Empty<BcfComment>(),
                    Array.Empty<BcfViewpoint>()));
            }

            try
            {
                BcfZipPackage.Write(BcfIssueExchange.Create(topics));
            }
            catch (InvalidDataException exception)
            {
                if (string.Equals(exception.Message, "BCF package exceeds the bounded archive size.", StringComparison.Ordinal))
                    return;
                throw new Exception("Aggregate BCF ZIP overflow must fail through the archive write ceiling.", exception);
            }

            throw new Exception("Aggregate-valid BCF content above the archive ceiling must fail closed.");
        }

        private static void OrdinaryPackageStillRoundTrips()
        {
            const string topicId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
            var topic = new BcfTopic(
                topicId,
                "Bounded package",
                "Open",
                "Coordination",
                "Ordinary control",
                "qa@qs3d",
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                Array.Empty<BcfComment>(),
                Array.Empty<BcfViewpoint>());
            var package = BcfZipPackage.Write(BcfIssueExchange.Create(new[] { topic }));
            if (package.Length <= 0 || package.Length > BcfZipPackage.MaxArchiveBytes)
                throw new Exception("Canonical BCF ZIP control must remain inside the package ceiling.");
            var roundTrip = BcfZipPackage.Read(package);
            if (roundTrip.Topics.Count != 1 || !string.Equals(roundTrip.Topics[0].Id, topicId, StringComparison.Ordinal))
                throw new Exception("Bounded BCF ZIP write changed canonical topic identity.");
        }
    }
}
