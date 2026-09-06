using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotCardinalitySmoke
    {
        private const int MaximumEntries = 100000;

        internal static void Run()
        {
            SaveAcceptsExactNestedBoundary();
            SaveRejectsOversizedNestedCollectionBeforePublication();
            SaveRejectsOversizedElementCollectionBeforePublication();
            LoadRejectsOversizedNestedCollection();
        }

        private static void SaveAcceptsExactNestedBoundary()
        {
            var path = TempPath("exact");
            try
            {
                var snapshot = Snapshot("REV-EXACT");
                var element = Element("E-1");
                for (var index = 0; index < MaximumEntries; index++)
                    element.Properties["P" + index.ToString("D6", CultureInfo.InvariantCulture)] = "V";
                snapshot.Elements.Add(element);

                var store = new RevisionSnapshotStore();
                store.Save(snapshot, path);
                var loaded = store.Load(path);

                Equal(MaximumEntries, loaded.Elements[0].Properties.Count, "Revision persistence must accept exactly 100,000 nested entries.");
            }
            finally
            {
                Delete(path);
                Delete(path + ".bak");
            }
        }

        private static void SaveRejectsOversizedNestedCollectionBeforePublication()
        {
            var path = TempPath("nested-oversize");
            try
            {
                var snapshot = Snapshot("REV-NESTED-OVERSIZE");
                var element = Element("E-1");
                for (var index = 0; index <= MaximumEntries; index++)
                    element.Properties["P" + index.ToString("D6", CultureInfo.InvariantCulture)] = "V";
                snapshot.Elements.Add(element);

                var error = Capture<InvalidDataException>(() => new RevisionSnapshotStore().Save(snapshot, path));

                Contains("100000", error.Message, "Oversized nested persistence failure must report the supported bound.");
                False(File.Exists(path), "Oversized nested state must fail before publishing the primary revision file.");
                False(File.Exists(path + ".bak"), "Oversized nested state must fail before publishing a backup revision file.");
            }
            finally
            {
                Delete(path);
                Delete(path + ".bak");
            }
        }

        private static void SaveRejectsOversizedElementCollectionBeforePublication()
        {
            var path = TempPath("element-oversize");
            try
            {
                var snapshot = Snapshot("REV-ELEMENT-OVERSIZE");
                for (var index = 0; index <= MaximumEntries; index++)
                    snapshot.Elements.Add(Element("E-" + index.ToString("D6", CultureInfo.InvariantCulture)));

                var error = Capture<InvalidDataException>(() => new RevisionSnapshotStore().Save(snapshot, path));

                Contains("100000", error.Message, "Oversized element persistence failure must report the supported bound.");
                False(File.Exists(path), "Oversized element state must fail before publishing the primary revision file.");
            }
            finally
            {
                Delete(path);
                Delete(path + ".bak");
            }
        }

        private static void LoadRejectsOversizedNestedCollection()
        {
            var path = TempPath("load-oversize");
            try
            {
                using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    writer.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?><qs3dRevision schemaVersion=\"2\" projectId=\"P-1\" id=\"REV-LOAD-OVERSIZE\" createdUtc=\"2026-01-01T00:00:00.0000000Z\"><elements><element id=\"E-1\" category=\"Foundation\" familyId=\"\" floorId=\"\" zoneId=\"\"><properties>");
                    for (var index = 0; index <= MaximumEntries; index++)
                    {
                        writer.Write("<p name=\"P");
                        writer.Write(index.ToString("D6", CultureInfo.InvariantCulture));
                        writer.Write("\" value=\"V\" />");
                    }
                    writer.Write("</properties><quantities></quantities><sourceHandles></sourceHandles><dependencies></dependencies></element></elements></qs3dRevision>");
                }

                var error = Capture<InvalidDataException>(() => new RevisionSnapshotStore().Load(path));
                Contains("100000", error.Message, "Oversized loaded persistence state must report the supported bound.");
            }
            finally
            {
                Delete(path);
            }
        }

        private static RevisionSnapshot Snapshot(string id) => new RevisionSnapshot
        {
            Id = id,
            ProjectId = "P-1",
            CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        private static RevisionElementSnapshot Element(string id) => new RevisionElementSnapshot
        {
            ElementId = id,
            Category = "Foundation"
        };

        private static string TempPath(string label) => Path.Combine(
            Path.GetTempPath(),
            "qs3d-revision-cardinality-" + label + "-" + Guid.NewGuid().ToString("N") + ".xml");

        private static T Capture<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T error)
            {
                return error;
            }
            throw new InvalidOperationException("Expected " + typeof(T).Name + " was not thrown.");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + (actual ?? "<null>"));
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + " Actual=" + actual + ".");
        }

        private static void False(bool value, string message)
        {
            if (value) throw new InvalidOperationException(message);
        }

        private static void Delete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }
    }

    internal static class RevisionSnapshotCardinalityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RevisionSnapshotCardinalitySmoke.Run();
        }
    }
}
