using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantityReportReadOnlyResultSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GroupAndDetailRemainReadOnly();
        }

        private static void GroupAndDetailRemainReadOnly()
        {
            var project = new ProjectState("P-PQ-READONLY", "Project quantity readonly smoke");
            project.Families.Add(new ProjectFamily("F", "Beam", ElementCategory.Beam));

            var first = new ProjectElement("E1", ElementCategory.Beam, "F", string.Empty, string.Empty);
            first.SetQuantity("LengthM", 2d);
            project.Elements.Add(first);

            var second = new ProjectElement("E2", ElementCategory.Beam, "F", string.Empty, string.Empty);
            second.SetQuantity("LengthM", 3d);
            project.Elements.Add(second);

            var grouped = ProjectQuantityReportBuilder.Group(project);
            if (grouped.Count != 1 || grouped[0].Count != 2 || Math.Abs(grouped[0].LengthM - 5d) > 1e-12d)
                throw new InvalidOperationException("Project quantity grouping semantics changed while hardening the result boundary.");
            AssertReadOnly(grouped, "grouped");

            var detailed = ProjectQuantityReportBuilder.Detail(project);
            if (detailed.Count != 2 || detailed[0].Count != 1 || detailed[1].Count != 1 ||
                Math.Abs(detailed[0].LengthM + detailed[1].LengthM - 5d) > 1e-12d)
                throw new InvalidOperationException("Project quantity detail semantics changed while hardening the result boundary.");
            AssertReadOnly(detailed, "detail");
        }

        private static void AssertReadOnly(IReadOnlyList<QuantityReportRow> rows, string label)
        {
            if (!(rows is ICollection<QuantityReportRow> collection) || !collection.IsReadOnly)
                throw new InvalidOperationException("Project quantity " + label + " result must expose a structural read-only collection boundary.");

            try
            {
                collection.Add(new QuantityReportRow());
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new InvalidOperationException("Project quantity " + label + " result accepted structural mutation through ICollection<T>.");
        }
    }
}
