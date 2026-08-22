using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeJsonUnknownMemberSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            UnknownObjectMembersFailClosed();
            DeclaredDictionaryExtensionsRemainPortable();
        }

        private static void UnknownObjectMembersFailClosed()
        {
            var canonical = BuildCanonicalJson();
            var cases = new[]
            {
                new KeyValuePair<string, string>("$", InsertAfter(canonical, "{\n", "  \"unexpectedRoot\":true,\n")),
                new KeyValuePair<string, string>("$.units", InsertAfter(canonical, "\"units\": {", "\"unexpectedUnits\":true,")),
                new KeyValuePair<string, string>("$.project", InsertAfter(canonical, "\"project\": {\n", "    \"unexpectedProject\":true,\n")),
                new KeyValuePair<string, string>("$.zones[0]", InsertAfter(canonical, "\"zones\": [\n    {", "\"unexpectedZone\":true,")),
                new KeyValuePair<string, string>("$.floors[0]", InsertAfter(canonical, "\"floors\": [\n    {", "\"unexpectedFloor\":true,")),
                new KeyValuePair<string, string>("$.families[0]", InsertAfter(canonical, "\"families\": [\n    {", "\"unexpectedFamily\":true,")),
                new KeyValuePair<string, string>("$.elements[0]", InsertAfter(canonical, "\"elements\": [\n    {\n", "      \"unexpectedElement\":true,\n"))
            };

            foreach (var item in cases)
            {
                var validation = ProjectInterchangeJsonValidator.Validate(item.Value);
                if (validation.IsValid || !validation.Issues.Any(x =>
                        string.Equals(x.Code, "JSON_UNKNOWN_MEMBER", StringComparison.Ordinal) &&
                        string.Equals(x.Path, item.Key, StringComparison.Ordinal)))
                    throw new InvalidOperationException("Unknown interchange member must fail at object path " + item.Key + ".");

                Throws<InvalidDataException>(() => ProjectInterchangeValidatedSnapshotReader.Read(item.Value));
            }
        }

        private static void DeclaredDictionaryExtensionsRemainPortable()
        {
            var canonical = BuildCanonicalJson();
            var validation = ProjectInterchangeJsonValidator.Validate(canonical);
            if (!validation.IsValid)
                throw new InvalidOperationException("Canonical interchange dictionary extensions must remain valid.");

            var snapshot = ProjectInterchangeValidatedSnapshotReader.Read(canonical);
            Equal("family-value", snapshot.Families[0].Properties["VendorExtension"]);
            Equal("element-value", snapshot.Elements[0].Properties["VendorExtension"]);
            Equal(4.25d, snapshot.Elements[0].Quantities["VendorMetric"]);
        }

        private static string BuildCanonicalJson()
        {
            var project = new ProjectState("P-UNKNOWN-MEMBER", "Unknown Member Smoke")
            {
                DrawingFingerprint = "DWG-UNKNOWN-MEMBER",
                UpdatedUtc = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("Z-1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("FL-1", "Floor 1", 0d));
            var family = new ProjectFamily("FAM-1", "Beam Family", ElementCategory.Beam);
            family.Properties["VendorExtension"] = "family-value";
            project.Families.Add(family);

            var element = new ProjectElement("E-1", ElementCategory.Beam, family.Id, "FL-1", "Z-1")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            element.SetProperty("VendorExtension", "element-value");
            element.SetQuantity("VendorMetric", 4.25d);
            project.Elements.Add(element);
            return ProjectInterchangeJsonExporter.Build(project);
        }

        private static string InsertAfter(string source, string marker, string insertion)
        {
            var index = source.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0) throw new InvalidOperationException("Missing interchange fixture marker: " + marker);
            var offset = index + marker.Length;
            return source.Substring(0, offset) + insertion + source.Substring(offset);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
