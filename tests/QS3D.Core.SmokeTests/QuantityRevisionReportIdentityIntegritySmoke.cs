using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRevisionReportIdentityIntegritySmoke
    {
        internal static void Run()
        {
            BuildRejectsMalformedProjectIdentity();
            BuildRejectsMalformedElementIdentity();
            BuildRejectsMalformedQuantityIdentity();
            SummarizeRejectsMalformedQuantityIdentity();
            ValidSupplementaryUnicodeRemainsAccepted();
        }

        private static void BuildRejectsMalformedProjectIdentity()
        {
            var before = Snapshot("PROJECT\uD800", "E1", "Length", 1d);
            var after = Snapshot("PROJECT\uD800", "E1", "Length", 2d);
            ExpectInvalidXml(() => new QuantityRevisionReport().Build(before, after), "project identity");
        }

        private static void BuildRejectsMalformedElementIdentity()
        {
            var before = Snapshot("PROJECT", "E\uD800", "Length", 1d);
            var after = Snapshot("PROJECT", "E\uD800", "Length", 2d);
            ExpectInvalidXml(() => new QuantityRevisionReport().Build(before, after), "element identity");
        }

        private static void BuildRejectsMalformedQuantityIdentity()
        {
            var before = Snapshot("PROJECT", "E1", "Q\uD800", 1d);
            var after = Snapshot("PROJECT", "E1", "Q\uD800", 2d);
            ExpectInvalidXml(() => new QuantityRevisionReport().Build(before, after), "quantity identity");
        }

        private static void SummarizeRejectsMalformedQuantityIdentity()
        {
            var rows = new[]
            {
                new QuantityRevisionRow
                {
                    ElementId = "E1",
                    Category = ElementCategory.StructuralWall.ToString(),
                    QuantityName = "Q\uD800",
                    Change = "Changed",
                    Before = 1d,
                    After = 2d
                }
            };
            ExpectInvalidXml(() => new QuantityRevisionReport().Summarize(rows), "summary quantity identity");
        }

        private static void ValidSupplementaryUnicodeRemainsAccepted()
        {
            const string elementId = "E\U0001F600";
            const string quantityName = "Q\U0001F680";
            var report = new QuantityRevisionReport();
            var rows = report.Build(
                Snapshot("PROJECT\U0001F30D", elementId, quantityName, 1d),
                Snapshot("PROJECT\U0001F30D", elementId, quantityName, 2d));

            if (rows.Count != 1 || rows[0].ElementId != elementId || rows[0].QuantityName != quantityName)
                throw new Exception("Quantity revision report did not preserve valid supplementary-plane identity text exactly.");

            var summary = report.Summarize(rows);
            if (summary.Count != 1 || summary[0].QuantityName != quantityName || summary[0].Before != 1d || summary[0].After != 2d)
                throw new Exception("Quantity revision summary did not preserve valid supplementary-plane quantity identity exactly.");
        }

        private static RevisionSnapshot Snapshot(string projectId, string elementId, string quantityName, double quantityValue)
        {
            var snapshot = new RevisionSnapshot
            {
                Id = "REV",
                CreatedUtc = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                ProjectId = projectId
            };
            var element = new RevisionElementSnapshot
            {
                ElementId = elementId,
                Category = ElementCategory.StructuralWall.ToString()
            };
            element.Quantities[quantityName] = quantityValue;
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private static void ExpectInvalidXml(Action action, string label)
        {
            try
            {
                action();
                throw new Exception("Quantity revision report accepted malformed UTF-16/XML-invalid " + label + ".");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("invalid in XML", StringComparison.OrdinalIgnoreCase) < 0)
                    throw;
            }
        }
    }

    internal static class QuantityRevisionReportIdentityIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityRevisionReportIdentityIntegritySmoke.Run();
    }
}
