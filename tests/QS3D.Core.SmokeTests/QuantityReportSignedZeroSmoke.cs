using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportSignedZeroSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalizesExplicitMassSignedZero();
            PreservesOrdinaryPositiveMass();
        }

        private static void CanonicalizesExplicitMassSignedZero()
        {
            var project = NewMassProject("signed-zero", BitConverter.Int64BitsToDouble(long.MinValue));
            var row = ProjectQuantityReportBuilder.Detail(project).Single();

            if (!row.MassKg.HasValue || row.MassKg.Value != 0d || BitConverter.DoubleToInt64Bits(row.MassKg.Value) != 0L)
                throw new InvalidOperationException("QuantityReportSignedZeroSmoke expected public detail MassKg to canonicalize signed zero to positive zero.");
        }

        private static void PreservesOrdinaryPositiveMass()
        {
            var project = NewMassProject("ordinary-positive", 12.5d);
            var row = ProjectQuantityReportBuilder.Detail(project).Single();

            if (!row.MassKg.HasValue || Math.Abs(row.MassKg.Value - 12.5d) > 1e-12d)
                throw new InvalidOperationException("QuantityReportSignedZeroSmoke expected ordinary positive MassKg to remain unchanged.");
        }

        private static ProjectState NewMassProject(string id, double massKg)
        {
            var project = new ProjectState(id, id);
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            var family = new ProjectFamily("family", "Family", ElementCategory.Slab);
            project.Families.Add(family);
            var element = new ProjectElement("E1", ElementCategory.Slab, family.Id, "f", "z");
            element.Quantities["WeightKg"] = massKg;
            project.Elements.Add(element);
            return project;
        }
    }
}
