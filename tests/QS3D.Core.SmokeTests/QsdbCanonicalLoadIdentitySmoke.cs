using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbCanonicalLoadIdentitySmoke
    {
        public static void Run()
        {
            RequiredIdentityTokensFailClosed();
            OptionalIdentityReferencesFailClosed();
            ProvenanceTokensFailClosed();
            CanonicalEmptyOptionalReferencesRemainAccepted();
        }

        private static void RequiredIdentityTokensFailClosed()
        {
            RejectTamperedLoad(
                root => root.Element("zones")!.Element("zone")!.SetAttributeValue("id", " zone-a "),
                "Padded persisted zone id was silently normalized while loading QSDB.");
            RejectTamperedLoad(
                root => root.Element("floors")!.Element("floor")!.SetAttributeValue("id", " floor-a "),
                "Padded persisted floor id was silently normalized while loading QSDB.");
            RejectTamperedLoad(
                root => root.Element("families")!.Element("family")!.SetAttributeValue("id", " family-a "),
                "Padded persisted family id was silently normalized while loading QSDB.");
            RejectTamperedLoad(
                root => root.Element("elements")!.Elements("element").Last().SetAttributeValue("id", " element-a "),
                "Padded persisted element id was silently normalized while loading QSDB.");
        }

        private static void OptionalIdentityReferencesFailClosed()
        {
            RejectTamperedLoad(
                root => root.SetAttributeValue("activeZoneId", " zone-a "),
                "Padded active zone id was silently normalized while loading QSDB.");
            RejectTamperedLoad(
                root => root.SetAttributeValue("activeFloorId", " floor-a "),
                "Padded active floor id was silently normalized while loading QSDB.");
            RejectTamperedLoad(
                root => root.SetAttributeValue("activeZoneId", "   "),
                "Whitespace-only active zone id silently became an empty selection while loading QSDB.");
            RejectTamperedLoad(
                root => root.Element("elements")!.Elements("element").Last().SetAttributeValue("familyId", " family-a "),
                "Padded element family reference was silently normalized while loading QSDB.");
            RejectTamperedLoad(
                root => root.Element("elements")!.Elements("element").Last().SetAttributeValue("floorId", " floor-a "),
                "Padded element floor reference was silently normalized while loading QSDB.");
            RejectTamperedLoad(
                root => root.Element("elements")!.Elements("element").Last().SetAttributeValue("zoneId", " zone-a "),
                "Padded element zone reference was silently normalized while loading QSDB.");
        }

        private static void ProvenanceTokensFailClosed()
        {
            RejectTamperedLoad(
                root => root.Element("elements")!.Elements("element").Last().Element("handles")!.Element("h")!.Value = " 2B ",
                "Padded source handle was silently normalized while loading QSDB.");
            RejectTamperedLoad(
                root => root.Element("elements")!.Elements("element").Last().Element("dependencies")!.Element("d")!.Value = " host-a ",
                "Padded dependency id was silently normalized while loading QSDB.");
        }

        private static void CanonicalEmptyOptionalReferencesRemainAccepted()
        {
            WithCanonicalFile((store, path, root) =>
            {
                root.SetAttributeValue("activeZoneId", string.Empty);
                root.SetAttributeValue("activeFloorId", string.Empty);
                var element = root.Element("elements")!.Elements("element").Last();
                element.SetAttributeValue("familyId", string.Empty);
                element.SetAttributeValue("floorId", string.Empty);
                element.SetAttributeValue("zoneId", string.Empty);
                root.Document!.Save(path, SaveOptions.DisableFormatting);

                var loaded = store.Load(path);
                if (loaded.ActiveZoneId.Length != 0 || loaded.ActiveFloorId.Length != 0)
                    throw new Exception("Canonical empty active selection did not remain empty while loading QSDB.");
                var loadedElement = loaded.Elements.Last();
                if (loadedElement.FamilyId.Length != 0 || loadedElement.FloorId.Length != 0 || loadedElement.ZoneId.Length != 0)
                    throw new Exception("Canonical empty element identity reference did not remain empty while loading QSDB.");
            });
        }

        private static void RejectTamperedLoad(Action<XElement> tamper, string message)
        {
            WithCanonicalFile((store, path, root) =>
            {
                tamper(root);
                root.Document!.Save(path, SaveOptions.DisableFormatting);

                var rejected = false;
                try { store.Load(path); }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected) throw new Exception(message);
            });
        }

        private static void WithCanonicalFile(Action<QsdbProjectStore, string, XElement> action)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-canonical-load-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(NewProject(), path);
                var root = XDocument.Load(path, LoadOptions.None).Root
                    ?? throw new Exception("Serialized QSDB root fixture was not found.");
                action(store, path, root);
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
                try { if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp"); } catch { }
            }
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("project-a", "Canonical load identity");
            project.Zones.Add(new ZoneDefinition("zone-a", "Zone A"));
            project.Floors.Add(new FloorDefinition("floor-a", "Floor A", 0d));
            project.ActiveZoneId = "zone-a";
            project.ActiveFloorId = "floor-a";

            project.Families.Add(new ProjectFamily("family-a", "Family A", ElementCategory.ArchitecturalWall));

            var host = new ProjectElement("host-a", ElementCategory.ArchitecturalWall, "family-a", "floor-a", "zone-a");
            host.SourceHandles.Add("1A");
            project.Elements.Add(host);

            var element = new ProjectElement("element-a", ElementCategory.ArchitecturalWall, "family-a", "floor-a", "zone-a");
            element.SourceHandles.Add("2B");
            element.DependsOn.Add("host-a");
            project.Elements.Add(element);
            return project;
        }
    }
}
