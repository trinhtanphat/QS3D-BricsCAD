using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbReferenceCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalReferencesRoundTrip();
            RejectsPaddedActiveZoneReference();
            RejectsPaddedActiveFloorReference();
            RejectsPaddedElementFamilyReference();
            RejectsPaddedElementFloorReference();
            RejectsPaddedElementZoneReference();
            RejectsWhitespaceOnlyOptionalReference();
        }

        private static void CanonicalReferencesRoundTrip()
        {
            WithSavedProject((store, path) =>
            {
                var loaded = store.Load(path);
                Equal("zone-1", loaded.ActiveZoneId, "active zone");
                Equal("floor-1", loaded.ActiveFloorId, "active floor");
                Equal("family-1", loaded.Elements[0].FamilyId, "element family");
                Equal("floor-1", loaded.Elements[0].FloorId, "element floor");
                Equal("zone-1", loaded.Elements[0].ZoneId, "element zone");
            });
        }

        private static void RejectsPaddedActiveZoneReference() =>
            RejectMutatedAttribute("activeZoneId=\"zone-1\"", "activeZoneId=\" zone-1 \"");

        private static void RejectsPaddedActiveFloorReference() =>
            RejectMutatedAttribute("activeFloorId=\"floor-1\"", "activeFloorId=\" floor-1 \"");

        private static void RejectsPaddedElementFamilyReference() =>
            RejectMutatedAttribute("familyId=\"family-1\"", "familyId=\" family-1 \"");

        private static void RejectsPaddedElementFloorReference() =>
            RejectMutatedAttribute("floorId=\"floor-1\"", "floorId=\" floor-1 \"");

        private static void RejectsPaddedElementZoneReference() =>
            RejectMutatedAttribute("zoneId=\"zone-1\"", "zoneId=\" zone-1 \"");

        private static void RejectsWhitespaceOnlyOptionalReference() =>
            RejectMutatedAttribute("familyId=\"family-1\"", "familyId=\"   \"");

        private static void RejectMutatedAttribute(string canonical, string malformed)
        {
            WithSavedProject((store, path) =>
            {
                var xml = File.ReadAllText(path);
                if (xml.IndexOf(canonical, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("QsdbReferenceCanonicalitySmoke fixture attribute was not found: " + canonical);
                File.WriteAllText(path, xml.Replace(canonical, malformed));
                Throws<InvalidDataException>(() => store.Load(path));
            });
        }

        private static void WithSavedProject(Action<QsdbProjectStore, string> action)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-reference-canonicality-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var project = new ProjectState("project-1", "Reference canonicality");
                project.Zones.Add(new ZoneDefinition("zone-1", "Zone 1"));
                project.Floors.Add(new FloorDefinition("floor-1", "Floor 1", 0d));
                project.Families.Add(new ProjectFamily("family-1", "Family 1", ElementCategory.Room));
                project.ActiveZoneId = "zone-1";
                project.ActiveFloorId = "floor-1";
                project.Elements.Add(new ProjectElement("element-1", ElementCategory.Room, "family-1", "floor-1", "zone-1"));

                var store = new QsdbProjectStore();
                store.Save(project, path);
                action(store, path);
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
                foreach (var temp in Directory.GetFiles(Path.GetDirectoryName(path) ?? Path.GetTempPath(), Path.GetFileName(path) + ".*.tmp"))
                    SafeDelete(temp);
            }
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("QsdbReferenceCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
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

            throw new InvalidOperationException("QsdbReferenceCanonicalitySmoke expected " + typeof(TException).Name + ".");
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
