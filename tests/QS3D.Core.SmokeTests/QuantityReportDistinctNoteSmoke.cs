using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportDistinctNoteSmoke
    {
        internal static void Run()
        {
            var project = new ProjectState("quantity-report-distinct-note", "Quantity report distinct note");
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            var family = new ProjectFamily("slab", "Slab", ElementCategory.Slab);
            project.Families.Add(family);

            project.Elements.Add(Element("S1", family.Id, "A", 1d));
            project.Elements.Add(Element("S2", family.Id, "B", 2d));
            project.Elements.Add(Element("S3", family.Id, "A", 3d));

            var row = ProjectQuantityReportBuilder.Group(project).Single();
            if (row.Count != 3)
                throw new Exception("Quantity report distinct-note grouping must preserve the aggregate element count.");
            if (Math.Abs(row.NetConcreteM3 - 6d) > 1e-12)
                throw new Exception("Quantity report distinct-note grouping must preserve quantity aggregation.");
            if (!string.Equals(row.Note, "A | B", StringComparison.Ordinal))
                throw new Exception("Quantity report distinct-note grouping must render each semantic note once in first-seen order. Actual: " + row.Note + ".");
        }

        private static ProjectElement Element(string id, string familyId, string note, double netConcreteM3)
        {
            var element = new ProjectElement(id, ElementCategory.Slab, familyId, "f", "z");
            element.Properties["Note"] = note;
            element.Quantities["NetConcreteM3"] = netConcreteM3;
            return element;
        }
    }
}
