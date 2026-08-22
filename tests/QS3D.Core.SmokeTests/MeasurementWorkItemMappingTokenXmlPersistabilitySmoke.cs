using System;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemMappingTokenXmlPersistabilitySmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidTokensAtCanonicalBoundary();
            SupplementaryUnicodeRoundTripsThroughProjectMappingPersistence();
        }

        private static void RejectsXmlInvalidTokensAtCanonicalBoundary()
        {
            Throws<ArgumentException>(() => new MeasurementWorkItemMapping(
                "MAP-\uD800", ElementCategory.Beam, "MEASURE-1", "CLASS-1", "WORK-1"));
            Throws<ArgumentException>(() => new MeasurementWorkItemMapping(
                "MAP-1", ElementCategory.Beam, "MEASURE-\uD800", "CLASS-1", "WORK-1"));
            Throws<ArgumentException>(() => new MeasurementWorkItemMapping(
                "MAP-1", ElementCategory.Beam, "MEASURE-1", "CLASS-\uD800", "WORK-1"));
            Throws<ArgumentException>(() => new MeasurementWorkItemMapping(
                "MAP-1", ElementCategory.Beam, "MEASURE-1", "CLASS-1", "WORK-\uD800"));

            var emptyCatalog = new MeasurementWorkItemMappingCatalog(Array.Empty<MeasurementWorkItemMapping>());
            Throws<ArgumentException>(() => emptyCatalog.Resolve(ElementCategory.Beam, "MEASURE-\uD800"));
        }

        private static void SupplementaryUnicodeRoundTripsThroughProjectMappingPersistence()
        {
            const string compass = "\U0001F9ED";
            var mapping = new MeasurementWorkItemMapping(
                "MAP-" + compass,
                ElementCategory.Beam,
                "MEASURE-" + compass,
                "CLASS-" + compass,
                "WORK-" + compass);
            var project = new ProjectState("MAPPING-XML", "Mapping token XML persistability");
            var beforeVersion = project.ChangeVersion;

            project.MeasurementWorkItemMappings.Add(mapping);

            Require(project.ChangeVersion == beforeVersion + 1L, "Valid mapping add did not advance project revision exactly once.");
            Require(project.MeasurementWorkItemMappings.Count == 1, "Valid mapping add did not persist exactly one canonical mapping.");

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-mapping-token-xml-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var roundTripped = loaded.MeasurementWorkItemMappings.Single();

                Equal(mapping.MappingId, roundTripped.MappingId, "MappingId QSDB round-trip");
                Require(mapping.Category == roundTripped.Category, "Mapping category changed across QSDB round-trip.");
                Equal(mapping.MeasurementItemId, roundTripped.MeasurementItemId, "MeasurementItemId QSDB round-trip");
                Equal(mapping.ClassificationId, roundTripped.ClassificationId, "ClassificationId QSDB round-trip");
                Equal(mapping.WorkItemId, roundTripped.WorkItemId, "WorkItemId QSDB round-trip");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " mismatch.");
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
