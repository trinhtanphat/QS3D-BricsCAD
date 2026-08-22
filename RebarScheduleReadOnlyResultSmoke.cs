using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarScheduleReadOnlyResultSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OrdinaryRowsRemainOrderedAndReadOnly();
        }

        private static void OrdinaryRowsRemainOrderedAndReadOnly()
        {
            var rows = RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput
                {
                    ElementId = "COUNT",
                    BarMark = "B1",
                    Notation = "2x3D16",
                    CuttingLengthM = 2d
                },
                new RebarScheduleInput
                {
                    ElementId = "SPACING",
                    BarMark = "B2",
                    Notation = "D12@200",
                    CuttingLengthM = 1d,
                    DistributionLengthM = 1d
                }
            });

            if (rows.Count != 2 || rows[0].Quantity != 6 || rows[1].Quantity != 6 ||
                !string.Equals(rows[0].BarMark, "B1", StringComparison.Ordinal) ||
                !string.Equals(rows[1].BarMark, "B2", StringComparison.Ordinal))
                throw new InvalidOperationException("Rebar schedule row ordering or quantity semantics changed while hardening the result boundary.");

            if (!(rows is ICollection<RebarScheduleRow> collection) || !collection.IsReadOnly)
                throw new InvalidOperationException("Rebar schedule result must expose a structural read-only collection boundary.");

            try
            {
                collection.Add(new RebarScheduleRow());
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new InvalidOperationException("Rebar schedule result accepted structural mutation through ICollection<T>.");
        }
    }
}
