using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbProjectXmlTextPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            InvalidMetadataValueFailsAtPublicMutationBoundary();
            InvalidRelationTextFailsBeforeFilesystemMutation();
            PersistedPaddedRelationsFailLoadInsteadOfBeingLaundered();
            LoneSurrogateFailsBeforeFilesystemMutation();
            SupplementaryUnicodeRoundTrips();
        }

        private static void InvalidMetadataValueFailsAtPublicMutationBoundary()
        {
            var project = Project("P-QSDB-XML-METADATA");
            var schema = project.SchemaVersion;
            var updatedUtc = project.UpdatedUtc;
            var changeVersion = project.ChangeVersion;

            Throws<ArgumentException>(() => project.Metadata["Note"] = "bad\u0001value");

            if (project.Metadata.ContainsKey("Note"))
                throw new InvalidOperationException("Invalid XML metadata value was retained after public mutation rejection.");
            if (project.SchemaVersion != schema || project.UpdatedUtc != updatedUtc || project.ChangeVersion != changeVersion)
                throw new InvalidOperationException("Invalid XML metadata mutation changed project persistence state before rejection.");
        }

        private static void InvalidRelationTextFailsBeforeFilesystemMutation()
        {
            var project = Project("P-QSDB-XML-TEXT");
            var element = new ProjectElement("E-QSDB-XML-TEXT", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            AddPersistedDependency(element, "DEP-\u0001");
            project.Elements.Add(element);
            AssertPreflightFailure(project, "invalid-relation-control");
        }

        private static void PersistedPaddedRelationsFailLoadInsteadOfBeingLaundered()
        {
            var root = TempRoot("padded-relations-load");
            var path = Path.Combine(root, "project.qsdb");
            var project = Project("P-QSDB-PADDED-RELATIONS");
            var element = new ProjectElement("E-QSDB-PADDED-RELATIONS", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.SourceHandles.Add("HANDLE-1");
            element.DependsOn.Add("DEP-1");
            project.Elements.Add(element);

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var xml = File.ReadAllText(path);
                xml = xml.Replace("<h>HANDLE-1</h>", "<h> HANDLE-1 </h>")
                         .Replace("<d>DEP-1</d>", "<d> DEP-1 </d>");
                File.WriteAllText(path, xml);

                Throws<InvalidDataException>(() => store.Load(path));
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static void LoneSurrogateFailsBeforeFilesystemMutation()
        {
            var project = Project("P-QSDB-XML-SURROGATE");
            var invalid = new string(new[] { '\uD800' });
            Throws<ArgumentException>(() => project.DrawingPath = invalid);
            var drawingPathField = typeof(ProjectState).GetField(
                "_drawingPath",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectState raw DrawingPath fixture field was not found.");
            drawingPathField.SetValue(project, invalid);
            if (!string.Equals(project.DrawingPath, invalid, StringComparison.Ordinal))
                throw new InvalidOperationException("ProjectState malformed legacy DrawingPath fixture was not injected.");
            AssertPreflightFailure(project, "invalid-drawing-path-surrogate");
        }

        private static void SupplementaryUnicodeRoundTrips()
        {
            var root = TempRoot("valid-supplementary");
            var path = Path.Combine(root, "project.qsdb");
            const string expected = "Valid supplementary \U0001F642 text";
            var project = Project("P-QSDB-XML-SUPPLEMENTARY");
            project.Metadata["Note"] = expected;
            project.Metadata["NullValue"] = null!;

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                if (!loaded.Metadata.TryGetValue("Note", out var actual) ||
                    !string.Equals(actual, expected, StringComparison.Ordinal))
                    throw new InvalidOperationException("Valid supplementary Unicode QSDB metadata did not round-trip exactly.");
                if (!loaded.Metadata.TryGetValue("NullValue", out var nullValue) ||
                    !string.Equals(nullValue, string.Empty, StringComparison.Ordinal))
                    throw new InvalidOperationException("Null QSDB metadata value no longer preserves empty-string serialization semantics.");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static ProjectState Project(string id)
        {
            var project = new ProjectState(id, "QSDB XML Text Project")
            {
                SchemaVersion = ProjectState.CurrentSchemaVersion - 1
            };
            return project;
        }

        private static void AddPersistedDependency(ProjectElement element, string dependency)
        {
            var relationField = typeof(ProjectElement).GetField("_dependsOn", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement persisted dependency backing relation field was not found.");
            var relation = relationField.GetValue(element)
                ?? throw new InvalidOperationException("ProjectElement persisted dependency backing relation was not found.");
            var valuesField = relation.GetType().GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement persisted dependency backing values field was not found.");
            var values = valuesField.GetValue(relation) as List<string>
                ?? throw new InvalidOperationException("ProjectElement persisted dependency backing values were not found.");
            values.Add(dependency);
        }

        private static void AssertPreflightFailure(ProjectState project, string suffix)
        {
            var root = TempRoot(suffix);
            var path = Path.Combine(root, "project.qsdb");
            var schema = project.SchemaVersion;
            var updatedUtc = project.UpdatedUtc;
            var changeVersion = project.ChangeVersion;
            try
            {
                Throws<InvalidDataException>(() => new QsdbProjectStore().SaveNew(project, path));
                if (Directory.Exists(root))
                    throw new InvalidOperationException("Invalid QSDB XML text mutated the filesystem before failing preflight: " + suffix + ".");
                if (project.SchemaVersion != schema || project.UpdatedUtc != updatedUtc || project.ChangeVersion != changeVersion)
                    throw new InvalidOperationException("Invalid QSDB XML text mutated project persistence state before failing preflight: " + suffix + ".");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static string TempRoot(string suffix) =>
            Path.Combine(Path.GetTempPath(), "QS3D-QsdbXmlText-" + suffix + "-" + Guid.NewGuid().ToString("N"));

        private static void DeleteTree(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
