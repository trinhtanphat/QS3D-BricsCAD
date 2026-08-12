using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceEnumCanonicalitySmoke
    {
        public static void Run()
        {
            var store = new ProjectBrowserWorkspaceStateStore();
            var canonical = store.Serialize(new ProjectBrowserWorkspaceState(
                categories: new[] { ElementCategory.Grid }));

            var baseline = store.Deserialize(canonical);
            Equal(ProjectBrowserGrouping.FloorThenCategory, baseline.Grouping);
            Equal(1, baseline.Categories.Count);
            Equal(ElementCategory.Grid, baseline.Categories[0]);

            var numericGrouping = XDocument.Parse(canonical);
            numericGrouping.Root!.SetAttributeValue(
                "grouping",
                ((int)ProjectBrowserGrouping.FloorThenCategory).ToString(CultureInfo.InvariantCulture));
            Throws<InvalidDataException>(() => store.Deserialize(numericGrouping.ToString(SaveOptions.DisableFormatting)));

            var numericCategory = XDocument.Parse(canonical);
            numericCategory.Root!.Element("Categories")!.Element("Category")!.Value =
                ((int)ElementCategory.Grid).ToString(CultureInfo.InvariantCulture);
            Throws<InvalidDataException>(() => store.Deserialize(numericCategory.ToString(SaveOptions.DisableFormatting)));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
