using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceContainerOrderSmoke
    {
        public static void Run()
        {
            var store = new ProjectBrowserWorkspaceStateStore();
            var canonical = store.Serialize(new ProjectBrowserWorkspaceState());
            store.Deserialize(canonical);

            var reordered = XDocument.Parse(canonical);
            var root = reordered.Root!;
            var categories = root.Element("Categories")!;
            var floorIds = root.Element("FloorIds")!;
            floorIds.Remove();
            categories.AddBeforeSelf(floorIds);

            Throws<InvalidDataException>(() => store.Deserialize(reordered.ToString(SaveOptions.DisableFormatting)));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
