using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleCatalogRevisionSmoke
    {
        internal static void Run()
        {
            CatalogMutationTouchesProjectExactlyOnce();
            CatalogUsesLastAvailableRevision();
            ScheduleBuildRejectsMoreThanFiveThousandMatchesBeforeTableMaterialization();
            ScheduleBuildCountsOnlyMatchingRowsAgainstTheLimit();
        }

        private static void CatalogMutationTouchesProjectExactlyOnce()
        {
            var project = Project();
            var definition = Definition("S1", "Beam schedule", "BEAMS");

            Equal(0L, project.ChangeVersion);
            SemanticScheduleCatalog.Save(project, new[] { definition });
            Equal(1L, project.ChangeVersion);
            True(project.Metadata.ContainsKey(SemanticScheduleCatalog.MetadataKey));

            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            SemanticScheduleCatalog.Save(project, new[] { definition });
            Equal(version, project.ChangeVersion);
            Equal(updatedUtc, project.UpdatedUtc);

            SemanticScheduleCatalog.Save(project, Array.Empty<SemanticScheduleDefinition>());
            Equal(version + 1L, project.ChangeVersion);
            True(!project.Metadata.ContainsKey(SemanticScheduleCatalog.MetadataKey));
        }

        private static void CatalogUsesLastAvailableRevision()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-semantic-schedule-revision-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(Project(), path);

                var document = XDocument.Load(path, LoadOptions.None);
                var root = document.Root ?? throw new Exception("Serialized QSDB root was not found for semantic schedule revision-ceiling fixture.");
                root.SetAttributeValue(
                    "changeVersion",
                    (long.MaxValue - 1L).ToString(CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);

                var project = store.Load(path);
                Equal(long.MaxValue - 1L, project.ChangeVersion);

                var definition = Definition("S1", "Beam schedule", "BEAMS");
                SemanticScheduleCatalog.Save(project, new[] { definition });

                Equal(long.MaxValue, project.ChangeVersion);
                True(project.Metadata.ContainsKey(SemanticScheduleCatalog.MetadataKey));

                var beforeRejectedUpdatedUtc = project.UpdatedUtc;
                var beforeRejectedMetadata = project.Metadata[SemanticScheduleCatalog.MetadataKey];

                var rejectedRewrite = false;
                try
                {
                    SemanticScheduleCatalog.Save(project, new[] { Definition("S1", "Changed schedule", "CHANGED") });
                }
                catch (OverflowException)
                {
                    rejectedRewrite = true;
                }

                True(rejectedRewrite);
                Equal(long.MaxValue, project.ChangeVersion);
                Equal(beforeRejectedUpdatedUtc, project.UpdatedUtc);
                Equal(beforeRejectedMetadata, project.Metadata[SemanticScheduleCatalog.MetadataKey]);

                var rejectedClear = false;
                try
                {
                    SemanticScheduleCatalog.Save(project, Array.Empty<SemanticScheduleDefinition>());
                }
                catch (OverflowException)
                {
                    rejectedClear = true;
                }

                True(rejectedClear);
                Equal(long.MaxValue, project.ChangeVersion);
                Equal(beforeRejectedUpdatedUtc, project.UpdatedUtc);
                Equal(beforeRejectedMetadata, project.Metadata[SemanticScheduleCatalog.MetadataKey]);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void ScheduleBuildRejectsMoreThanFiveThousandMatchesBeforeTableMaterialization()
        {
            var project = new ProjectState("SCHEDULE-MATCH-LIMIT", "Schedule match limit");
            for (var i = 0; i < 5001; i++)
                project.Elements.Add(new ProjectElement("B" + i.ToString("D4", CultureInfo.InvariantCulture), ElementCategory.Beam, string.Empty, string.Empty, string.Empty));

            var message = ThrowsMessage<InvalidOperationException>(() =>
                SemanticScheduleCatalog.Build(project, Definition("S1", "Beam schedule", "BEAMS")));

            Equal("Semantic schedule supports at most 5000 matching elements.", message);
        }

        private static void ScheduleBuildCountsOnlyMatchingRowsAgainstTheLimit()
        {
            var project = new ProjectState("SCHEDULE-NONMATCH-LIMIT", "Schedule nonmatch limit");
            for (var i = 0; i < 5001; i++)
                project.Elements.Add(new ProjectElement("C" + i.ToString("D4", CultureInfo.InvariantCulture), ElementCategory.Column, string.Empty, string.Empty, string.Empty));
            project.Elements.Add(new ProjectElement("B0001", ElementCategory.Beam, string.Empty, string.Empty, string.Empty));

            var table = SemanticScheduleCatalog.Build(project, Definition("S1", "Beam schedule", "BEAMS"));

            Equal(1, table.Rows.Count);
            Equal("B0001", table.Rows[0].ElementId);
        }

        private static SemanticScheduleDefinition Definition(string id, string name, string title)
        {
            return new SemanticScheduleDefinition(
                id,
                name,
                title,
                new[] { ElementCategory.Beam },
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[]
                {
                    new SemanticDocumentationColumn("Id", "{Id}"),
                    new SemanticDocumentationColumn("Mark", "{P:Mark}")
                });
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("SCHEDULE-REV", "Schedule revision");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty));
            project.Elements.Single().Properties["Mark"] = "B1";
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition) throw new Exception("Expected condition to be true.");
        }

        private static string ThrowsMessage<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                return ex.Message;
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
