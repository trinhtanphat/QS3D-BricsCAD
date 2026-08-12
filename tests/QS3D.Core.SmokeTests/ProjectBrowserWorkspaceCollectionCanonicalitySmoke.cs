using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceCollectionCanonicalitySmoke
    {
        public static void Run()
        {
            var store = new ProjectBrowserWorkspaceStateStore();
            var canonical = store.Serialize(new ProjectBrowserWorkspaceState(
                categories: new[] { ElementCategory.Column, ElementCategory.Beam },
                floorIds: new[] { "F-2", "F-1" },
                zoneIds: new[] { "Z-2", "Z-1" },
                expandedPaths: new[] { "P/B", "P/A" },
                selectedElementIds: new[] { "E-2", "E-1" },
                primaryElementId: "E-1"));

            store.Deserialize(canonical);

            RejectReordered(store, canonical, "Categories");
            RejectReordered(store, canonical, "FloorIds");
            RejectReordered(store, canonical, "ZoneIds");
            RejectReordered(store, canonical, "ExpandedPaths");
            RejectReordered(store, canonical, "SelectedElementIds");

            var duplicateCategory = XDocument.Parse(canonical);
            var categories = duplicateCategory.Root!.Element("Categories")!;
            categories.Add(new XElement("Category", categories.Elements("Category").First().Value));
            Throws<InvalidDataException>(() => store.Deserialize(duplicateCategory.ToString(SaveOptions.DisableFormatting)));
        }

        private static void RejectReordered(ProjectBrowserWorkspaceStateStore store, string canonical, string collectionName)
        {
            var document = XDocument.Parse(canonical);
            var items = document.Root!.Element(collectionName)!.Elements().ToList();
            if (items.Count < 2) throw new Exception("Expected at least two persisted items in " + collectionName + ".");
            var first = items[0].Value;
            items[0].Value = items[1].Value;
            items[1].Value = first;
            Throws<InvalidDataException>(() => store.Deserialize(document.ToString(SaveOptions.DisableFormatting)));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
