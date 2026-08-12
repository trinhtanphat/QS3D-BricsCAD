using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportReadOnlyResultSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OrdinaryGroupsRemainOrderedAndReadOnly();
        }

        private static void OrdinaryGroupsRemainOrderedAndReadOnly()
        {
            var family = new FamilyDefinition("B300", ElementCategory.Beam, "Concrete");
            var rows = QuantityReportBuilder.Group(new[]
            {
                new ElementInstance("E1", family, "L1") { GrossConcreteM3 = 1d },
                new ElementInstance("E2", family, "L1") { GrossConcreteM3 = 2d },
                new ElementInstance("E3", family, "L2") { GrossConcreteM3 = 3d }
            });

            if (rows.Count != 2 ||
                !string.Equals(rows[0].Floor, "L1", StringComparison.Ordinal) || rows[0].Count != 2 || Math.Abs(rows[0].GrossConcreteM3 - 3d) > 1e-12d ||
                !string.Equals(rows[1].Floor, "L2", StringComparison.Ordinal) || rows[1].Count != 1 || Math.Abs(rows[1].GrossConcreteM3 - 3d) > 1e-12d)
                throw new InvalidOperationException("Quantity report grouping/order/count semantics changed while hardening the result boundary.");

            if (!(rows is ICollection<QuantityReportRow> collection) || !collection.IsReadOnly)
                throw new InvalidOperationException("Quantity report result must expose a structural read-only collection boundary.");

            try
            {
                collection.Add(new QuantityReportRow());
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new InvalidOperationException("Quantity report result accepted structural mutation through ICollection<T>.");
        }
    }
}
