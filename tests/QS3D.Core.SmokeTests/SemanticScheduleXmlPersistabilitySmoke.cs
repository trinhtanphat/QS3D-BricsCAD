using System;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleXmlPersistabilitySmoke
    {
        internal static void Run()
        {
            InvalidPersistedTextFailsBeforeProjectMutation();
            SupplementaryUnicodeRoundTripsExactly();
        }

        private static void InvalidPersistedTextFailsBeforeProjectMutation()
        {
            var project = new ProjectState("SCHEDULE-XML", "Schedule XML persistability");
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeHasPayload = project.Metadata.TryGetValue(SemanticScheduleCatalog.MetadataKey, out var beforePayload);

            var invalid = new[]
            {
                Definition("S-\uD800", "Schedule", "TITLE", "", "", Array.Empty<string>(), Array.Empty<string>(), "Id", "{Id}"),
                Definition("S1", "Schedule \uD800", "TITLE", "", "", Array.Empty<string>(), Array.Empty<string>(), "Id", "{Id}"),
                Definition("S1", "Schedule", "TITLE \uD800", "", "", Array.Empty<string>(), Array.Empty<string>(), "Id", "{Id}"),
                Definition("S1", "Schedule", "TITLE", "F-\uD800", "", Array.Empty<string>(), Array.Empty<string>(), "Id", "{Id}"),
                Definition("S1", "Schedule", "TITLE", "", "Z-\uD800", Array.Empty<string>(), Array.Empty<string>(), "Id", "{Id}"),
                Definition("S1", "Schedule", "TITLE", "", "", new[] { "E-\uD800" }, Array.Empty<string>(), "Id", "{Id}"),
                Definition("S1", "Schedule", "TITLE", "", "", Array.Empty<string>(), new[] { "E-\uD800" }, "Id", "{Id}"),
                Definition("S1", "Schedule", "TITLE", "", "", Array.Empty<string>(), Array.Empty<string>(), "Header \uD800", "{Id}"),
                Definition("S1", "Schedule", "TITLE", "", "", Array.Empty<string>(), Array.Empty<string>(), "Id", "{Id}\uD800")
            };

            foreach (var definition in invalid)
            {
                Throws<ArgumentException>(() => SemanticScheduleCatalog.Save(project, new[] { definition }));
                Require(project.ChangeVersion == beforeVersion, "XML-invalid Semantic Schedule Save changed project revision.");
                Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Semantic Schedule Save changed project timestamp.");
                var afterHasPayload = project.Metadata.TryGetValue(SemanticScheduleCatalog.MetadataKey, out var afterPayload);
                Require(afterHasPayload == beforeHasPayload, "XML-invalid Semantic Schedule Save changed catalog metadata presence.");
                if (beforeHasPayload)
                    Require(string.Equals(beforePayload, afterPayload, StringComparison.Ordinal), "XML-invalid Semantic Schedule Save changed catalog metadata payload.");
            }
        }

        private static void SupplementaryUnicodeRoundTripsExactly()
        {
            const string marker = "\U0001F9ED";
            var project = new ProjectState("SCHEDULE-UNICODE", "Schedule Unicode persistability");
            var definition = Definition(
                "S-" + marker,
                "Schedule " + marker,
                "TITLE " + marker,
                "F-" + marker,
                "Z-" + marker,
                new[] { "E-IN-" + marker },
                new[] { "E-OUT-" + marker },
                "Header " + marker,
                "{Id} " + marker);

            SemanticScheduleCatalog.Save(project, new[] { definition });
            var loaded = SemanticScheduleCatalog.Load(project);

            Require(loaded.Count == 1, "Supplementary-Unicode Semantic Schedule did not round-trip exactly once.");
            var roundTripped = loaded[0];
            Require(roundTripped.Id == definition.Id, "Supplementary-Unicode schedule id changed across catalog round-trip.");
            Require(roundTripped.Name == definition.Name, "Supplementary-Unicode schedule name changed across catalog round-trip.");
            Require(roundTripped.Title == definition.Title, "Supplementary-Unicode schedule title changed across catalog round-trip.");
            Require(roundTripped.FloorId == definition.FloorId, "Supplementary-Unicode Floor id changed across catalog round-trip.");
            Require(roundTripped.ZoneId == definition.ZoneId, "Supplementary-Unicode Zone id changed across catalog round-trip.");
            Require(roundTripped.IncludeElementIds.Count == 1 && roundTripped.IncludeElementIds[0] == definition.IncludeElementIds[0], "Supplementary-Unicode include id changed across catalog round-trip.");
            Require(roundTripped.ExcludeElementIds.Count == 1 && roundTripped.ExcludeElementIds[0] == definition.ExcludeElementIds[0], "Supplementary-Unicode exclude id changed across catalog round-trip.");
            Require(roundTripped.Columns.Count == 1 && roundTripped.Columns[0].Header == definition.Columns[0].Header, "Supplementary-Unicode column header changed across catalog round-trip.");
            Require(roundTripped.Columns[0].Template == definition.Columns[0].Template, "Supplementary-Unicode column template changed across catalog round-trip.");
        }

        private static SemanticScheduleDefinition Definition(
            string id,
            string name,
            string title,
            string floorId,
            string zoneId,
            string[] include,
            string[] exclude,
            string header,
            string template)
        {
            return new SemanticScheduleDefinition(
                id,
                name,
                title,
                new[] { ElementCategory.Beam },
                floorId,
                zoneId,
                include,
                exclude,
                new[] { new SemanticDocumentationColumn(header, template) });
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
