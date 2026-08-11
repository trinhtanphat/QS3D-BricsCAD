using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionDependencyFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DependencyOnlyMutationIsDetected();
            EquivalentDependencySetsDoNotDiff();
            DependenciesRoundTripAndLegacyLoads();
            MalformedDependencyXmlFailsClosed();
        }

        private static void DependencyOnlyMutationIsDetected()
        {
            var service = new RevisionService();
            var project = NewProject(out var element);
            element.DependsOn.Add("HOST-A");
            var before = service.Capture(project, "before");

            element.DependsOn.Clear();
            element.DependsOn.Add("HOST-B");
            var after = service.Capture(project, "after");

            var deltas = service.Compare(before, after);
            Require(deltas.Count == 1, "dependency-only mutation was not detected");
            var field = deltas[0].Fields.SingleOrDefault(x => string.Equals(x.Field, "Dependencies", StringComparison.Ordinal));
            Require(field != null, "dependency-only mutation did not emit a Dependencies field delta");
            Require(string.Equals(field.Before, "HOST-A", StringComparison.Ordinal), "unexpected dependency before value");
            Require(string.Equals(field.After, "HOST-B", StringComparison.Ordinal), "unexpected dependency after value");
        }

        private static void EquivalentDependencySetsDoNotDiff()
        {
            var service = new RevisionService();
            var project = NewProject(out var element);
            element.DependsOn.Add(" host-b ");
            element.DependsOn.Add(string.Empty);
            element.DependsOn.Add("HOST-A");
            element.DependsOn.Add("HOST-B");
            var before = service.Capture(project, "before");

            Require(before.Elements.Single().Dependencies.Count == 2, "capture did not canonicalize dependency set");
            element.DependsOn.Clear();
            element.DependsOn.Add("host-a");
            element.DependsOn.Add("HOST-B");
            element.DependsOn.Add("HOST-A");
            var after = service.Capture(project, "after");

            Require(service.Compare(before, after).Count == 0, "equivalent dependency sets produced a false revision diff");
        }

        private static void DependenciesRoundTripAndLegacyLoads()
        {
            var directory = TempDirectory();
            try
            {
                var service = new RevisionService();
                var project = NewProject(out var element);
                element.DependsOn.Add("HOST-B");
                element.DependsOn.Add("HOST-A");
                var snapshot = service.Capture(project, "roundtrip");
                var path = Path.Combine(directory, "roundtrip.qsrev");
                var store = new RevisionSnapshotStore();
                store.Save(snapshot, path);

                var loaded = store.Load(path);
                Require(loaded.Elements.Single().Dependencies.SequenceEqual(new[] { "HOST-A", "HOST-B" }, StringComparer.OrdinalIgnoreCase),
                    "revision dependency XML did not round-trip deterministically");
                Require(service.Compare(snapshot, loaded).Count == 0, "round-tripped dependency snapshot changed semantically");

                var document = XDocument.Load(path);
                Require(document.Descendants("dependencies").Count() == 1 && document.Descendants("d").Count() == 2,
                    "serialized revision did not emit dependency XML");

                var legacyPath = Path.Combine(directory, "legacy.qsrev");
                File.WriteAllText(legacyPath,
                    "<qs3dRevision id='legacy' createdUtc='2026-08-11T00:00:00Z'><elements><element id='E1' category='Beam' familyId='' floorId='' zoneId=''><properties/><quantities/><sourceHandles/></element></elements></qs3dRevision>");
                var legacy = store.Load(legacyPath);
                Require(legacy.Elements.Single().Dependencies.Count == 0, "legacy revision without dependencies did not load as an empty dependency set");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void MalformedDependencyXmlFailsClosed()
        {
            var directory = TempDirectory();
            try
            {
                var store = new RevisionSnapshotStore();
                Reject(store, directory, "duplicate-values.qsrev",
                    "<dependencies><d value='HOST-A'/><d value='host-a'/></dependencies>");
                Reject(store, directory, "padded-value.qsrev",
                    "<dependencies><d value=' HOST-A '/></dependencies>");
                Reject(store, directory, "unknown-child.qsrev",
                    "<dependencies><future value='HOST-A'/></dependencies>");
                Reject(store, directory, "duplicate-container.qsrev",
                    "<dependencies/><dependencies/>");

                var invalid = new RevisionSnapshot { Id = "invalid", CreatedUtc = DateTime.UtcNow };
                var item = new RevisionElementSnapshot { ElementId = "E1", Category = "Beam" };
                item.Dependencies.Add(" HOST-A ");
                invalid.Elements.Add(item);
                Throws<InvalidDataException>(() => store.Save(invalid, Path.Combine(directory, "invalid-save.qsrev")));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void Reject(RevisionSnapshotStore store, string directory, string fileName, string dependencyXml)
        {
            var path = Path.Combine(directory, fileName);
            File.WriteAllText(path,
                "<qs3dRevision id='R' createdUtc='2026-08-11T00:00:00Z'><elements><element id='E1' category='Beam' familyId='' floorId='' zoneId=''><properties/><quantities/><sourceHandles/>" +
                dependencyXml + "</element></elements></qs3dRevision>");
            Throws<InvalidDataException>(() => store.Load(path));
        }

        private static ProjectState NewProject(out ProjectElement element)
        {
            var project = new ProjectState("revision-dependency", "Revision Dependency");
            element = new ProjectElement("E1", ElementCategory.Beam);
            project.Elements.Add(element);
            return project;
        }

        private static string TempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-revision-dependency-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("RevisionDependencyFreshnessSmoke expected " + typeof(T).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("RevisionDependencyFreshnessSmoke: " + message);
        }
    }
}
