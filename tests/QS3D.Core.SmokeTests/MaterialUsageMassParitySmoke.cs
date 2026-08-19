using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageMassParitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            DerivedSteelMassMatchesQuantityReport();
            ExplicitMassStillWins();
            NonKgPrimaryQuantityRemainsVolumeBased();
        }

        private static void DerivedSteelMassMatchesQuantityReport()
        {
            var project = MassProject("derived-steel", "Thép", "7850", 2.5d);

            var material = MaterialUsageScheduleBuilder.Build(project).Single();
            var quantity = ProjectQuantityReportBuilder.Detail(project).Single();
            const double expected = 19625d;

            Equal("kg", material.UnitHint, "built-in steel unit");
            Equal(expected, material.MassKg, "derived Material Usage steel mass");
            Equal(expected, material.PrimaryQuantity, "Material Usage kg primary quantity");
            if (!quantity.MassKg.HasValue) throw new InvalidOperationException("Quantity report mass was unexpectedly blank.");
            Equal(quantity.MassKg.Value, material.MassKg, "Material Usage / quantity-report mass parity");
        }

        private static void ExplicitMassStillWins()
        {
            var project = MassProject("explicit-steel", "Thép", "7850", 2.5d);
            project.Elements[0].Quantities["WeightKg"] = 123.5d;

            var material = MaterialUsageScheduleBuilder.Build(project).Single();
            var quantity = ProjectQuantityReportBuilder.Detail(project).Single();

            Equal(123.5d, material.MassKg, "explicit Material Usage mass precedence");
            Equal(123.5d, material.PrimaryQuantity, "explicit kg primary quantity precedence");
            if (!quantity.MassKg.HasValue) throw new InvalidOperationException("Quantity report explicit mass was unexpectedly blank.");
            Equal(quantity.MassKg.Value, material.MassKg, "explicit mass parity");
        }

        private static void NonKgPrimaryQuantityRemainsVolumeBased()
        {
            var project = MassProject("concrete-volume", "Bê tông", "2400", 1.25d);

            var material = MaterialUsageScheduleBuilder.Build(project).Single();

            Equal("m³", material.UnitHint, "built-in concrete unit");
            Equal(3000d, material.MassKg, "derived non-kg material mass remains available");
            Equal(1.25d, material.VolumeM3, "concrete volume");
            Equal(1.25d, material.PrimaryQuantity, "non-kg primary quantity must remain unit-selected volume");
        }

        private static ProjectState MassProject(string id, string materialName, string density, double volume)
        {
            var project = new ProjectState(id, id);
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));

            var family = new ProjectFamily("family", "Family", ElementCategory.Slab);
            family.Properties["Material"] = materialName;
            family.Properties["DensityKgM3"] = density;
            project.Families.Add(family);

            var element = new ProjectElement("E1", ElementCategory.Slab, family.Id, "f", "z");
            element.Quantities["NetVolumeM3"] = volume;
            project.Elements.Add(element);
            return project;
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
