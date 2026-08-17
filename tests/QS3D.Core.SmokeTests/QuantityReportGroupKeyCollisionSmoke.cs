using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportGroupKeyCollisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const string separator = "|";

            var beamFamily = new FamilyDefinition("Column", ElementCategory.Beam, "N" + separator + "M");
            var columnFamily = new FamilyDefinition("N", ElementCategory.Column, "M");

            var first = new ElementInstance("E1", beamFamily, "F") { LengthM = 1d };
            first.SourceHandles.Add("H1");
            var sameGroup = new ElementInstance("E3", beamFamily, "F") { LengthM = 3d };
            sameGroup.SourceHandles.Add("H3");
            var delimiterLikeTuple = new ElementInstance("E2", columnFamily, "F" + separator + "Beam") { LengthM = 2d };
            delimiterLikeTuple.SourceHandles.Add("H2");

            var rows = QuantityReportBuilder.Group(new[] { first, sameGroup, delimiterLikeTuple });
            if (rows.Count != 2)
                throw new Exception("Distinct accepted quantity-report grouping tuples must not alias through delimiter-like text.");

            var beamRow = rows.Single(x => x.Category == ElementCategory.Beam.ToString());
            if (beamRow.Count != 2 || Math.Abs(beamRow.LengthM - 4d) > 1e-12d ||
                beamRow.ElementIds.Count != 2 || !beamRow.ElementIds.Contains("E1") || !beamRow.ElementIds.Contains("E3") ||
                beamRow.SourceHandles.Count != 2 || !beamRow.SourceHandles.Contains("H1") || !beamRow.SourceHandles.Contains("H3"))
                throw new Exception("Identical quantity-report tuples must still group with independent aggregate/provenance state.");

            var columnRow = rows.Single(x => x.Category == ElementCategory.Column.ToString());
            if (columnRow.Count != 1 || Math.Abs(columnRow.LengthM - 2d) > 1e-12d ||
                columnRow.ElementIds.Count != 1 || columnRow.ElementIds[0] != "E2" ||
                columnRow.SourceHandles.Count != 1 || columnRow.SourceHandles[0] != "H2")
                throw new Exception("Collision-free quantity-report grouping must preserve the second tuple independently.");
        }
    }
}
