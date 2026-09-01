using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbPrimaryIdentityCanonicalitySmoke
    {
        public static void Run()
        {
            RejectsPaddedZoneId();
            RejectsPaddedFloorId();
            RejectsPaddedFamilyId();
            RejectsPaddedElementId();
            RejectsPaddedRuleId();
            RejectsPaddedRuleOutput();
            RejectsPaddedQuantityName();
            CanonicalControlRoundTrips();
        }

        private static void RejectsPaddedZoneId() => RejectTamperedIdentity(
            "zone",
            document => Required(document, "zones", "zone").SetAttributeValue("id", " Z1 "));

        private static void RejectsPaddedFloorId() => RejectTamperedIdentity(
            "floor",
            document => Required(document, "floors", "floor").SetAttributeValue("id", " F1 "));

        private static void RejectsPaddedFamilyId() => RejectTamperedIdentity(
            "family",
            document => Required(document, "families", "family").SetAttributeValue("id", " FAM1 "));

        private static void RejectsPaddedElementId() => RejectTamperedIdentity(
            "element",
            document => Required(document, "elements", "element").SetAttributeValue("id", " E1 "));

        private static void RejectsPaddedRuleId() => RejectTamperedIdentity(
            "rule-id",
            document => Required(document, "rules", "rule").SetAttributeValue("id", " R1 "));

        private static void RejectsPaddedRuleOutput() => RejectTamperedIdentity(
            "rule-output",
            document => Required(document, "rules", "rule").SetAttributeValue("output", " AreaM2 "));

        private static void RejectsPaddedQuantityName() => RejectTamperedIdentity(
            "quantity-name",
            document => Required(document, "elements", "element").Element("quantities")!.Element("q")!.SetAttributeValue("name", " AreaM2 "));

        private static void CanonicalControlRoundTrips()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-primary-identity-control-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(BuildProject(), path);
                var loaded = store.Load(path);
                if (loaded.Zones.Count != 1 || loaded.Zones[0].Id != "Z1") throw new Exception("Canonical zone identity changed during QSDB round-trip.");
                if (loaded.Floors.Count != 1 || loaded.Floors[0].Id != "F1") throw new Exception("Canonical floor identity changed during QSDB round-trip.");
                if (loaded.Families.Count != 1 || loaded.Families[0].Id != "FAM1") throw new Exception("Canonical family identity changed during QSDB round-trip.");
                if (loaded.Elements.Count != 1 || loaded.Elements[0].Id != "E1") throw new Exception("Canonical element identity changed during QSDB round-trip.");
                if (loaded.QuantityRules.Count != 1 || loaded.QuantityRules[0].Id != "R1" || loaded.QuantityRules[0].OutputName != "AreaM2")
                    throw new Exception("Canonical quantity-rule identity changed during QSDB round-trip.");
                if (!loaded.Elements[0].Quantities.ContainsKey("AreaM2")) throw new Exception("Canonical element quantity identity changed during QSDB round-trip.");
            }
            finally
            {
                Delete(path);
            }
        }

        private static void RejectTamperedIdentity(string label, Action<XDocument> tamper)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-primary-identity-" + label + "-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(BuildProject(), path);
                var document = XDocument.Load(path, LoadOptions.None);
                tamper(document);
                document.Save(path, SaveOptions.DisableFormatting);

                try
                {
                    store.Load(path);
                }
                catch (InvalidDataException)
                {
                    return;
                }
                throw new Exception("Padded persisted QSDB primary identity was silently trim-normalized: " + label + ".");
            }
            finally
            {
                Delete(path);
            }
        }

        private static XElement Required(XDocument document, string containerName, string itemName)
        {
            return document.Root?.Element(containerName)?.Element(itemName)
                ?? throw new Exception("QSDB primary identity fixture is missing " + containerName + "/" + itemName + ".");
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P1", "Primary identity canonicality");
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("F1", "Floor 1", 0d));
            project.Families.Add(new ProjectFamily("FAM1", "Wall family", ElementCategory.ArchitecturalWall));
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.ArchitecturalWall, "AreaM2", "1", "v1"));
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, "FAM1", "F1", "Z1");
            element.SetQuantity("AreaM2", 1d);
            project.Elements.Add(element);
            return project;
        }

        private static void Delete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
            try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
        }
    }
}
