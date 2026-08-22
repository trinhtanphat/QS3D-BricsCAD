using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridRenumberReadOnlyResultSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OrdinaryRenumberRemainsOrderedAndReadOnly();
        }

        private static void OrdinaryRenumberRemainsOrderedAndReadOnly()
        {
            var project = new ProjectState("GRID-READONLY", "Grid readonly smoke");
            var first = new ProjectElement("G1", ElementCategory.Grid);
            var second = new ProjectElement("G2", ElementCategory.Grid);
            project.Elements.Add(first);
            project.Elements.Add(second);

            var plan = GridNamingService.Renumber(project, new[] { "G2", "G1" }, new GridNamingOptions
            {
                Prefix = "A-",
                StartIndex = 3
            });

            if (plan.Count != 2 ||
                !string.Equals(plan[0].ElementId, "G2", StringComparison.Ordinal) || !string.Equals(plan[0].Label, "A-3", StringComparison.Ordinal) ||
                !string.Equals(plan[1].ElementId, "G1", StringComparison.Ordinal) || !string.Equals(plan[1].Label, "A-4", StringComparison.Ordinal) ||
                !string.Equals(second.Properties[GridNamingService.GridLabelKey], "A-3", StringComparison.Ordinal) ||
                !string.Equals(first.Properties[GridNamingService.GridLabelKey], "A-4", StringComparison.Ordinal))
                throw new InvalidOperationException("Grid renumber order/label/property semantics changed while hardening the result boundary.");

            if (!(plan is ICollection<GridLabelAssignment> collection) || !collection.IsReadOnly)
                throw new InvalidOperationException("Grid renumber plan must expose a structural read-only collection boundary.");

            try
            {
                collection.Add(new GridLabelAssignment("G3", "A-5", 5));
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new InvalidOperationException("Grid renumber plan accepted structural mutation through ICollection<T>.");
        }
    }
}
