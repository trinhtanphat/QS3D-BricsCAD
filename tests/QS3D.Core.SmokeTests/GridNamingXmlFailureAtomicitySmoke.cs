using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingXmlFailureAtomicitySmoke
    {
        internal static void Run()
        {
            InvalidPrefixFailsBeforeAnyMutation();
            InvalidSuffixFailsBeforeAnyMutation();
            SupplementaryUnicodeRoundTripsThroughGridLabels();
        }

        private static void InvalidPrefixFailsBeforeAnyMutation()
        {
            var project = CreateProject(out var first, out var second);
            AssertInvalidAffixIsAtomic(
                project,
                first,
                second,
                new GridNamingOptions { Prefix = "G-\uD800", Suffix = "-X", StartIndex = 1, NumericPadding = 2 },
                "prefix");
        }

        private static void InvalidSuffixFailsBeforeAnyMutation()
        {
            var project = CreateProject(out var first, out var second);
            AssertInvalidAffixIsAtomic(
                project,
                first,
                second,
                new GridNamingOptions { Prefix = "G-", Suffix = "\uD800-X", StartIndex = 1, NumericPadding = 2 },
                "suffix");
        }

        private static void AssertInvalidAffixIsAtomic(
            ProjectState project,
            ProjectElement first,
            ProjectElement second,
            GridNamingOptions options,
            string label)
        {
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var firstDirty = first.Dirty;
            var secondDirty = second.Dirty;
            var firstUpdatedUtc = first.UpdatedUtc;
            var secondUpdatedUtc = second.UpdatedUtc;
            var firstLabel = first.Properties[GridNamingService.GridLabelKey];
            var secondLabel = second.Properties[GridNamingService.GridLabelKey];
            var firstSequence = first.Properties[GridNamingService.GridSequenceIndexKey];
            var secondSequence = second.Properties[GridNamingService.GridSequenceIndexKey];

            Throws<ArgumentException>(() => GridNamingService.Renumber(project, new[] { first.Id, second.Id }, options));

            Require(project.ChangeVersion == beforeVersion, "XML-invalid Grid naming " + label + " changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Grid naming " + label + " changed project timestamp.");
            Require(first.Dirty == firstDirty && second.Dirty == secondDirty, "XML-invalid Grid naming " + label + " changed Grid dirty flags.");
            Require(first.UpdatedUtc == firstUpdatedUtc && second.UpdatedUtc == secondUpdatedUtc, "XML-invalid Grid naming " + label + " changed Grid timestamps.");
            Require(first.Properties[GridNamingService.GridLabelKey] == firstLabel, "XML-invalid Grid naming " + label + " changed the first Grid label.");
            Require(second.Properties[GridNamingService.GridLabelKey] == secondLabel, "XML-invalid Grid naming " + label + " changed the second Grid label.");
            Require(first.Properties[GridNamingService.GridSequenceIndexKey] == firstSequence, "XML-invalid Grid naming " + label + " changed the first Grid sequence index.");
            Require(second.Properties[GridNamingService.GridSequenceIndexKey] == secondSequence, "XML-invalid Grid naming " + label + " changed the second Grid sequence index.");
        }

        private static void SupplementaryUnicodeRoundTripsThroughGridLabels()
        {
            const string marker = "\U0001F9ED";
            var project = CreateProject(out var first, out var second);
            var assignments = GridNamingService.Renumber(
                project,
                new[] { first.Id, second.Id },
                new GridNamingOptions
                {
                    Prefix = marker + "-",
                    Suffix = "-" + marker,
                    StartIndex = 7,
                    NumericPadding = 2
                });

            Require(assignments.Count == 2, "Supplementary-Unicode Grid naming returned an unexpected assignment count.");
            Require(assignments[0].Label == marker + "-07-" + marker, "Supplementary-Unicode first Grid label was not preserved in the plan.");
            Require(assignments[1].Label == marker + "-08-" + marker, "Supplementary-Unicode second Grid label was not preserved in the plan.");
            Require(first.Properties[GridNamingService.GridLabelKey] == assignments[0].Label, "Supplementary-Unicode first Grid label was not applied exactly.");
            Require(second.Properties[GridNamingService.GridLabelKey] == assignments[1].Label, "Supplementary-Unicode second Grid label was not applied exactly.");

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-grid-naming-xml-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var loadedFirst = loaded.FindElement(first.Id) ?? throw new InvalidOperationException("First Grid was not found after QSDB round-trip.");
                var loadedSecond = loaded.FindElement(second.Id) ?? throw new InvalidOperationException("Second Grid was not found after QSDB round-trip.");
                Require(loadedFirst.Properties[GridNamingService.GridLabelKey] == assignments[0].Label, "Supplementary-Unicode first Grid label changed across QSDB round-trip.");
                Require(loadedSecond.Properties[GridNamingService.GridLabelKey] == assignments[1].Label, "Supplementary-Unicode second Grid label changed across QSDB round-trip.");
                Require(loadedFirst.Properties[GridNamingService.GridSequenceIndexKey] == "7", "First Grid sequence index changed across QSDB round-trip.");
                Require(loadedSecond.Properties[GridNamingService.GridSequenceIndexKey] == "8", "Second Grid sequence index changed across QSDB round-trip.");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static ProjectState CreateProject(out ProjectElement first, out ProjectElement second)
        {
            var project = new ProjectState("GRID-NAMING-XML", "Grid naming XML atomicity");
            first = new ProjectElement("GRID-A", ElementCategory.Grid);
            second = new ProjectElement("GRID-B", ElementCategory.Grid);
            first.SetProperty(GridNamingService.GridLabelKey, "OLD-A");
            first.SetProperty(GridNamingService.GridSequenceIndexKey, "90");
            second.SetProperty(GridNamingService.GridLabelKey, "OLD-B");
            second.SetProperty(GridNamingService.GridSequenceIndexKey, "91");
            first.MarkClean(ElementDirtyFlags.All);
            second.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(first);
            project.Elements.Add(second);
            return project;
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
