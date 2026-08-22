using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantityReportGroupKeySmoke
    {
        public static void Run()
        {
            DelimiterInjectionDoesNotMergeDistinctRows();
            LegitimateSameKeyRowsStillAggregate();
        }

        private static void DelimiterInjectionDoesNotMergeDistinctRows()
        {
            var project = new ProjectState("P-COLLISION", "Collision");
            project.Families.Add(new ProjectFamily("F\u001fM", "Family A", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("F", "Family B", ElementCategory.Beam));

            var first = new ProjectElement("E1", ElementCategory.Beam, "F\u001fM", string.Empty, string.Empty);
            first.SetProperty("Material", "X");
            first.SetQuantity("LengthM", 2d);
            project.Elements.Add(first);

            var second = new ProjectElement("E2", ElementCategory.Beam, "F", string.Empty, string.Empty);
            second.SetProperty("Material", "M\u001fX");
            second.SetQuantity("LengthM", 3d);
            project.Elements.Add(second);

            var rows = ProjectQuantityReportBuilder.Group(project);
            if (rows.Count != 2)
                throw new InvalidOperationException("Distinct project quantity group tuples were merged through delimiter injection.");

            var firstRow = rows.Single(x => string.Equals(x.FamilyId, "F\u001fM", StringComparison.Ordinal));
            var secondRow = rows.Single(x => string.Equals(x.FamilyId, "F", StringComparison.Ordinal));
            AssertRow(firstRow, "X", 1, 2d, "first collision row");
            AssertRow(secondRow, "M\u001fX", 1, 3d, "second collision row");
        }

        private static void LegitimateSameKeyRowsStillAggregate()
        {
            var project = new ProjectState("P-AGG", "Aggregation");
            project.Families.Add(new ProjectFamily("F", "Family", ElementCategory.Beam));

            var first = new ProjectElement("E1", ElementCategory.Beam, "F", string.Empty, string.Empty);
            first.SetProperty("Material", "Concrete");
            first.SetQuantity("LengthM", 2d);
            project.Elements.Add(first);

            var second = new ProjectElement("E2", ElementCategory.Beam, "F", string.Empty, string.Empty);
            second.SetProperty("Material", "Concrete");
            second.SetQuantity("LengthM", 3d);
            project.Elements.Add(second);

            var rows = ProjectQuantityReportBuilder.Group(project);
            if (rows.Count != 1)
                throw new InvalidOperationException("Equivalent project quantity group tuples no longer aggregate.");
            AssertRow(rows[0], "Concrete", 2, 5d, "normal aggregate row");
        }

        private static void AssertRow(QuantityReportRow row, string material, int count, double lengthM, string label)
        {
            if (!string.Equals(row.Material, material, StringComparison.Ordinal) || row.Count != count || Math.Abs(row.LengthM - lengthM) > 1e-12d)
                throw new InvalidOperationException("Project quantity " + label + " changed unexpectedly.");
        }
    }

    internal static class ProjectQuantityReportGroupKeySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectQuantityReportGroupKeySmoke.Run();
        }
    }
}
