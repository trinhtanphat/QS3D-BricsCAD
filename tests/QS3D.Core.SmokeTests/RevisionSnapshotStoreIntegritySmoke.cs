using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotStoreIntegritySmoke
    {
        public static void Run()
        {
            CanonicalUtcLoadsAndNonCanonicalTimestampsFailClosed();
            SaveRequiresUtcAndCanonicalDefinedCategory();
            FreeTextRoundTripsAndInvalidSavePreservesExistingFile();
            MalformedMapsAndSourceHandlesFailClosed();
            V2RoundTripsProjectIdentityAndLegacyV1LoadsWithoutIdentity();
            V2SchemaRequiresIdentityAndVersionPairing();
        }

        private static void CanonicalUtcLoadsAndNonCanonicalTimestampsFailClosed()
        {
            var directory = TempDirectory();
            try
            {
                var store = new RevisionSnapshotStore();
                var canonicalPath = Path.Combine(directory, "canonical.qsrev");
                RevisionDocument("2026-08-10T05:00:00.0000000Z").Save(canonicalPath, SaveOptions.DisableFormatting);
                var loaded = store.Load(canonicalPath);
                Equal(new DateTime(2026, 8, 10, 5, 0, 0, DateTimeKind.Utc), loaded.CreatedUtc);
                Equal(DateTimeKind.Utc, loaded.CreatedUtc.Kind);

                var offsetPath = Path.Combine(directory, "offset.qsrev");
                RevisionDocument("2026-08-10T12:00:00.0000000+07:00").Save(offsetPath, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(offsetPath));

                var zeroOffsetPath = Path.Combine(directory, "zero-offset.qsrev");
                RevisionDocument("2026-08-10T05:00:00.0000000+00:00").Save(zeroOffsetPath, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(zeroOffsetPath));

                var missingOffsetPath = Path.Combine(directory, "missing-offset.qsrev");
                RevisionDocument("2026-08-10T05:00:00.0000000").Save(missingOffsetPath, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(missingOffsetPath));

                var shortUtcPath = Path.Combine(directory, "short-utc.qsrev");
                RevisionDocument("2026-08-10T05:00:00Z").Save(shortUtcPath, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(shortUtcPath));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void SaveRequiresUtcAndCanonicalDefinedCategory()
        {
            var directory = TempDirectory();
            try
            {
                var store = new RevisionSnapshotStore();
                var unspecifiedPath = Path.Combine(directory, "unspecified.qsrev");
                var unspecified = Snapshot("unspecified", "Beam");
                unspecified.CreatedUtc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Unspecified);
                Throws<InvalidDataException>(() => store.Save(unspecified, unspecifiedPath));
                False(File.Exists(unspecifiedPath), "non-UTC revision save published a file");

                var localPath = Path.Combine(directory, "local.qsrev");
                var local = Snapshot("local", "Beam");
                local.CreatedUtc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Local);
                Throws<InvalidDataException>(() => store.Save(local, localPath));
                False(File.Exists(localPath), "local revision save published a file");

                var undefinedPath = Path.Combine(directory, "undefined-category.qsrev");
                Throws<InvalidDataException>(() => store.Save(Snapshot("undefined", "999"), undefinedPath));
                False(File.Exists(undefinedPath), "undefined revision category was persisted");

                var nonCanonicalPath = Path.Combine(directory, "noncanonical-category.qsrev");
                Throws<InvalidDataException>(() => store.Save(Snapshot("noncanonical", "beam"), nonCanonicalPath));
                False(File.Exists(nonCanonicalPath), "non-canonical revision category was persisted");

                var loadedUndefinedPath = Path.Combine(directory, "load-undefined.qsrev");
                RevisionDocument("2026-08-10T05:00:00.0000000Z", "999").Save(loadedUndefinedPath, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(loadedUndefinedPath));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void FreeTextRoundTripsAndInvalidSavePreservesExistingFile()
        {
            var directory = TempDirectory();
            try
            {
                var path = Path.Combine(directory, "atomic.qsrev");
                var store = new RevisionSnapshotStore();
                var valid = Snapshot("valid", "Beam");
                valid.Elements[0].Properties["Note"] = "  intentional free text  ";
                valid.Elements[0].SourceHandles.Add("AA");
                store.Save(valid, path);
                var before = File.ReadAllBytes(path);

                var loaded = store.Load(path);
                Equal("  intentional free text  ", loaded.Elements.Single().Properties["Note"]);
                Equal("AA", loaded.Elements.Single().SourceHandles.Single());

                var invalid = Snapshot("invalid", "Beam");
                invalid.Elements[0].Properties[" PaddedKey "] = "must not publish";
                Throws<InvalidDataException>(() => store.Save(invalid, path));
                True(before.SequenceEqual(File.ReadAllBytes(path)), "failed revision save replaced the existing file");
                Equal("  intentional free text  ", store.Load(path).Elements.Single().Properties["Note"]);
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void MalformedMapsAndSourceHandlesFailClosed()
        {
            var directory = TempDirectory();
            try
            {
                var store = new RevisionSnapshotStore();
                var duplicateMapPath = Path.Combine(directory, "duplicate-map.qsrev");
                var duplicateProperties = new XElement("properties",
                    new XElement("p", new XAttribute("name", "Note"), new XAttribute("value", "first")),
                    new XElement("p", new XAttribute("name", "note"), new XAttribute("value", "second")));
                RevisionDocument("2026-08-10T05:00:00.0000000Z", "Beam", duplicateProperties).Save(duplicateMapPath, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(duplicateMapPath));

                var paddedMapPath = Path.Combine(directory, "padded-map.qsrev");
                var paddedProperties = new XElement("properties",
                    new XElement("p", new XAttribute("name", " Note "), new XAttribute("value", "value")));
                RevisionDocument("2026-08-10T05:00:00.0000000Z", "Beam", paddedProperties).Save(paddedMapPath, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(paddedMapPath));

                var paddedHandle = Snapshot("padded-handle", "Beam");
                paddedHandle.Elements[0].SourceHandles.Add(" AA ");
                Throws<InvalidDataException>(() => store.Save(paddedHandle, Path.Combine(directory, "padded-handle.qsrev")));

                var duplicateHandlePath = Path.Combine(directory, "duplicate-handle.qsrev");
                RevisionDocument(
                    "2026-08-10T05:00:00.0000000Z",
                    "Beam",
                    null,
                    new XElement("sourceHandles",
                        new XElement("h", new XAttribute("value", "AA")),
                        new XElement("h", new XAttribute("value", "aa"))))
                    .Save(duplicateHandlePath, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(duplicateHandlePath));
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void V2RoundTripsProjectIdentityAndLegacyV1LoadsWithoutIdentity()
        {
            var directory = TempDirectory();
            try
            {
                var project = new ProjectState("revision-store-v2-project", "Revision Store V2 Project");
                var element = new ProjectElement("E1", ElementCategory.Beam);
                project.Elements.Add(element);
                var snapshot = new RevisionService().Capture(project, "v2");

                var v2Path = Path.Combine(directory, "v2.qsrev");
                var store = new RevisionSnapshotStore();
                store.Save(snapshot, v2Path);

                var document = XDocument.Load(v2Path);
                Equal("2", document.Root?.Attribute("schemaVersion")?.Value);
                Equal(project.ProjectId, document.Root?.Attribute("projectId")?.Value);

                var loaded = store.Load(v2Path);
                Equal(project.ProjectId, loaded.ProjectId);

                var legacyPath = Path.Combine(directory, "legacy.qsrev");
                RevisionDocument("2026-08-10T05:00:00.0000000Z", "Beam").Save(legacyPath, SaveOptions.DisableFormatting);
                var legacy = store.Load(legacyPath);
                Equal(string.Empty, legacy.ProjectId);
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void V2SchemaRequiresIdentityAndVersionPairing()
        {
            var directory = TempDirectory();
            try
            {
                var store = new RevisionSnapshotStore();

                var missingIdentity = Path.Combine(directory, "missing-identity.qsrev");
                RevisionDocument("2026-08-10T05:00:00.0000000Z", "Beam", null, null, "2", null)
                    .Save(missingIdentity, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(missingIdentity));

                var missingVersion = Path.Combine(directory, "missing-version.qsrev");
                RevisionDocument("2026-08-10T05:00:00.0000000Z", "Beam", null, null, null, "PROJECT-A")
                    .Save(missingVersion, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(missingVersion));

                var v1WithIdentity = Path.Combine(directory, "v1-with-identity.qsrev");
                RevisionDocument("2026-08-10T05:00:00.0000000Z", "Beam", null, null, "1", "PROJECT-A")
                    .Save(v1WithIdentity, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(v1WithIdentity));

                var unsupportedVersion = Path.Combine(directory, "unsupported-version.qsrev");
                RevisionDocument("2026-08-10T05:00:00.0000000Z", "Beam", null, null, "3", null)
                    .Save(unsupportedVersion, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(unsupportedVersion));

                var protectedPath = Path.Combine(directory, "protected.qsrev");
                var project = new ProjectState("revision-store-protected", "Revision Store Protected");
                project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam));
                store.Save(new RevisionService().Capture(project, "valid"), protectedPath);
                var before = File.ReadAllBytes(protectedPath);
                var invalid = new RevisionSnapshot
                {
                    Id = "invalid",
                    CreatedUtc = new DateTime(2026, 8, 10, 5, 0, 0, DateTimeKind.Utc),
                    ProjectId = " PROJECT-A "
                };
                Throws<InvalidDataException>(() => store.Save(invalid, protectedPath));
                True(before.SequenceEqual(File.ReadAllBytes(protectedPath)), "invalid project identity replaced an existing valid baseline");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static RevisionSnapshot Snapshot(string id, string category)
        {
            var snapshot = new RevisionSnapshot
            {
                Id = id,
                CreatedUtc = new DateTime(2026, 8, 10, 5, 0, 0, DateTimeKind.Utc)
            };
            snapshot.Elements.Add(new RevisionElementSnapshot { ElementId = "E1", Category = category });
            return snapshot;
        }

        private static XDocument RevisionDocument(
            string createdUtc,
            string? category = null,
            XElement? properties = null,
            XElement? sourceHandles = null,
            string? schemaVersion = null,
            string? projectId = null)
        {
            var elements = new XElement("elements");
            if (category != null)
            {
                elements.Add(new XElement("element",
                    new XAttribute("id", "E1"),
                    new XAttribute("category", category),
                    properties ?? new XElement("properties"),
                    new XElement("quantities"),
                    sourceHandles ?? new XElement("sourceHandles"),
                    new XElement("dependencies")));
            }

            var root = new XElement("qs3dRevision",
                new XAttribute("id", "revision"),
                new XAttribute("createdUtc", createdUtc));

            if (schemaVersion != null)
                root.Add(new XAttribute("schemaVersion", schemaVersion));
            if (projectId != null)
                root.Add(new XAttribute("projectId", projectId));

            root.Add(elements);
            return new XDocument(root);
        }

        private static string TempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-revision-integrity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void False(bool condition, string message) => True(!condition, message);

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}