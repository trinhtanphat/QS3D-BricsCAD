using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportMassPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            RejectsSwallowedDensityContribution();
            RejectsSwallowedVolumeContribution();
            PreservesIdentityAndZeroCases();
        }

        private static void RejectsSwallowedDensityContribution()
        {
            var project = MassProject("swallowed-density", 1.0000000000000002d, double.Epsilon);
            ExpectThrows<InvalidOperationException>(() => ProjectQuantityReportBuilder.Detail(project));
        }

        private static void RejectsSwallowedVolumeContribution()
        {
            var project = MassProject("swallowed-volume", double.Epsilon, 1.0000000000000002d);
            ExpectThrows<InvalidOperationException>(() => ProjectQuantityReportBuilder.Detail(project));
        }

        private static void PreservesIdentityAndZeroCases()
        {
            AssertMass("density-identity", 1d, double.Epsilon, double.Epsilon);
            AssertMass("volume-identity", double.Epsilon, 1d, double.Epsilon);
            AssertMass("zero-volume", 1.0000000000000002d, 0d, 0d);
        }

        private static void AssertMass(string id, double density, double volume, double expected)
        {
            var mass = ProjectQuantityReportBuilder.Detail(MassProject(id, density, volume)).Single().MassKg;
            if (!mass.HasValue || mass.Value != expected)
                throw new Exception("Quantity-report mass identity/zero control failed for " + id + ".");
        }

        private static ProjectState MassProject(string id, double density, double volume)
        {
            var project = new ProjectState(id, id);
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            var family = new ProjectFamily("family", "Family", ElementCategory.Slab);
            family.Properties["Material"] = "Concrete";
            family.Properties["DensityKgM3"] = density.ToString("R", CultureInfo.InvariantCulture);
            project.Families.Add(family);
            var element = new ProjectElement("E1", ElementCategory.Slab, family.Id, "f", "z");
            element.Quantities["NetVolumeM3"] = volume;
            project.Elements.Add(element);
            return project;
        }

        private static void ExpectThrows<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
