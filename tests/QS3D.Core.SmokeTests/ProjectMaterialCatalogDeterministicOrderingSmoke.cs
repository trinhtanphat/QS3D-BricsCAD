using System;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMaterialCatalogDeterministicOrderingSmoke
    {
        internal static void Run()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                CatalogAndReferenceOrderingUseOrdinalIdentitySemantics();
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        private static void CatalogAndReferenceOrderingUseOrdinalIdentitySemantics()
        {
            var project = new ProjectState("P-MATERIAL-ORDER", "Material ordering");
            ProjectMaterialCatalog.UpsertCustom(project, "custom-zebra", "Zebra", "m", string.Empty);
            ProjectMaterialCatalog.UpsertCustom(project, "custom-a-umlaut", "Äther", "m", string.Empty);

            var catalogNames = ProjectMaterialCatalog.GetAll(project).Select(x => x.Name).ToArray();
            AssertOrdinalOrder(catalogNames, "material catalog");

            var family = new ProjectFamily("F-MATERIAL-ORDER", "Material refs", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Zebra";
            family.Properties["CurtainFrameMaterial"] = "Äther";
            project.Families.Add(family);

            var referenceNames = ProjectMaterialCatalog.ReferencedMaterialNames(project).ToArray();
            AssertOrdinalOrder(referenceNames, "referenced material names");
            if (referenceNames.Length != 2 ||
                !string.Equals(referenceNames[0], "Zebra", StringComparison.Ordinal) ||
                !string.Equals(referenceNames[1], "Äther", StringComparison.Ordinal))
                throw new InvalidOperationException("Referenced material ordering must follow OrdinalIgnoreCase identity semantics.");
        }

        private static void AssertOrdinalOrder(string[] values, string label)
        {
            var expected = values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            if (!values.SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidOperationException(label + " must be culture-independent and OrdinalIgnoreCase ordered.");
        }
    }
}
