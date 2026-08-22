using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementIdentityXmlPersistabilitySmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidIdentityAndRelationsBeforeMutation();
            SupplementaryUnicodeRoundTripsThroughQsdb();
        }

        private static void RejectsXmlInvalidIdentityAndRelationsBeforeMutation()
        {
            foreach (var invalid in InvalidXmlTokens())
            {
                ExpectArgument(() => new ProjectElement("E-" + invalid, ElementCategory.Room), "element id");
                ExpectArgument(() => new ProjectElement("E1", ElementCategory.Room, "F-" + invalid, string.Empty, string.Empty), "family relation constructor");
                ExpectArgument(() => new ProjectElement("E1", ElementCategory.Room, string.Empty, "L-" + invalid, string.Empty), "floor relation constructor");
                ExpectArgument(() => new ProjectElement("E1", ElementCategory.Room, string.Empty, string.Empty, "Z-" + invalid), "zone relation constructor");

                var element = new ProjectElement("E1", ElementCategory.Room, "F1", "L1", "Z1")
                {
                    DrawingFingerprint = "fingerprint-before"
                };
                element.MarkClean(ElementDirtyFlags.All);

                AssertRejectedSetter(element, () => element.FamilyId, value => element.FamilyId = value, "F-" + invalid, "FamilyId");
                AssertRejectedSetter(element, () => element.FloorId, value => element.FloorId = value, "L-" + invalid, "FloorId");
                AssertRejectedSetter(element, () => element.ZoneId, value => element.ZoneId = value, "Z-" + invalid, "ZoneId");
                AssertRejectedSetter(element, () => element.DrawingFingerprint, value => element.DrawingFingerprint = value, "fingerprint-" + invalid, "DrawingFingerprint");
            }
        }

        private static void SupplementaryUnicodeRoundTripsThroughQsdb()
        {
            const string supplementary = "\U0001F642";
            var familyId = "FAMILY-" + supplementary;
            var floorId = "FLOOR-" + supplementary;
            var zoneId = "ZONE-" + supplementary;
            var elementId = "ELEMENT-" + supplementary;
            var fingerprint = "FP-" + supplementary;
            var path = Path.Combine(Path.GetTempPath(), "qs3d-projectelement-identity-xml-" + Guid.NewGuid().ToString("N") + ".qsdb");

            try
            {
                var project = new ProjectState("P-IDENTITY-XML", "ProjectElement identity XML");
                project.Families.Add(new ProjectFamily(familyId, "Family " + supplementary, ElementCategory.Room));
                project.Floors.Add(new FloorDefinition(floorId, "Floor " + supplementary, 0d));
                project.Zones.Add(new ZoneDefinition(zoneId, "Zone " + supplementary));

                var element = new ProjectElement(
                    "  " + elementId + "  ",
                    ElementCategory.Room,
                    "  " + familyId + "  ",
                    "  " + floorId + "  ",
                    "  " + zoneId + "  ")
                {
                    DrawingFingerprint = "  " + fingerprint + "  "
                };
                project.Elements.Add(element);

                Equal(elementId, element.Id, "Element id trimming/supplementary Unicode changed.");
                Equal(familyId, element.FamilyId, "FamilyId trimming/supplementary Unicode changed.");
                Equal(floorId, element.FloorId, "FloorId trimming/supplementary Unicode changed.");
                Equal(zoneId, element.ZoneId, "ZoneId trimming/supplementary Unicode changed.");
                Equal(fingerprint, element.DrawingFingerprint, "DrawingFingerprint trimming/supplementary Unicode changed.");

                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var restored = loaded.FindElement(elementId) ?? throw new InvalidOperationException("Supplementary-Unicode element was not restored.");

                Equal(elementId, restored.Id, "Element Id supplementary Unicode did not round-trip exactly.");
                Equal(familyId, restored.FamilyId, "FamilyId supplementary Unicode did not round-trip exactly.");
                Equal(floorId, restored.FloorId, "FloorId supplementary Unicode did not round-trip exactly.");
                Equal(zoneId, restored.ZoneId, "ZoneId supplementary Unicode did not round-trip exactly.");
                Equal(fingerprint, restored.DrawingFingerprint, "DrawingFingerprint supplementary Unicode did not round-trip exactly.");
            }
            finally
            {
                DeleteIfExists(path);
                DeleteIfExists(path + ".bak");
                DeleteIfExists(path + ".tmp");
            }
        }

        private static void AssertRejectedSetter(
            ProjectElement element,
            Func<string> read,
            Action<string> write,
            string invalid,
            string label)
        {
            var beforeValue = read();
            var beforeDirty = element.Dirty;
            var beforeUpdatedUtc = element.UpdatedUtc;

            ExpectArgument(() => write(invalid), label);

            Equal(beforeValue, read(), "Rejected " + label + " changed the live value.");
            Equal(beforeDirty, element.Dirty, "Rejected " + label + " changed dirty flags.");
            Equal(beforeUpdatedUtc, element.UpdatedUtc, "Rejected " + label + " changed UpdatedUtc.");
        }

        private static string[] InvalidXmlTokens() => new[]
        {
            new string(new[] { '\uD800' }),
            new string(new[] { '\uDC00' })
        };

        private static void ExpectArgument(Action action, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Expected ArgumentException for XML-invalid " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void DeleteIfExists(string path)
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
}
