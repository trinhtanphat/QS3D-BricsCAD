using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageAggregationPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PreservesRepresentableLengthContributions();
            PreservesRepresentableAreaContributions();
            PreservesRepresentableVolumeContributions();
            PreservesRepresentableMassContributions();
            PreservesAcrossInputOrder();
            PreservesOrdinaryAggregationAndProvenance();
            PreservesOrdinaryDecimalAggregation();
            RejectsUnrepresentableFinalLoss();
            RejectsInvalidAndOverflowingInputs();
        }

        private static void PreservesRepresentableLengthContributions()
        {
            var row = Build("LengthM", 1e16, 1d, 1d);
            Exact(10000000000000002d, row.LengthM, "LengthM");
        }

        private static void PreservesRepresentableAreaContributions()
        {
            var row = Build("NetWallAreaM2", 1e16, 1d, 1d);
            Exact(10000000000000002d, row.AreaM2, "AreaM2");
        }

        private static void PreservesRepresentableVolumeContributions()
        {
            var row = Build("NetVolumeM3", 1e16, 1d, 1d);
            Exact(10000000000000002d, row.VolumeM3, "VolumeM3");
        }

        private static void PreservesRepresentableMassContributions()
        {
            var row = Build("WeightKg", 1e16, 1d, 1d);
            Exact(10000000000000002d, row.MassKg, "MassKg");
        }

        private static void PreservesAcrossInputOrder()
        {
            var row = Build("LengthM", 1d, 1e16, 1d);
            Exact(10000000000000002d, row.LengthM, "LengthM input order");
        }

        private static void PreservesOrdinaryAggregationAndProvenance()
        {
            var project = Project();
            Add(project, "a", "LengthM", 2d);
            Add(project, "b", "LengthM", 3d);

            var row = MaterialUsageScheduleBuilder.Build(project).Single();
            Exact(5d, row.LengthM, "ordinary LengthM");
            if (row.ElementCount != 2)
                throw new InvalidOperationException("Material Usage compensated aggregation changed ElementCount.");
            if (row.ElementIds.Count != 2 || row.ElementIds[0] != "a" || row.ElementIds[1] != "b")
                throw new InvalidOperationException("Material Usage compensated aggregation changed element provenance/order.");
        }

        private static void PreservesOrdinaryDecimalAggregation()
        {
            var row = BuildTwo("NetVolumeM3", 2.8d, 1.6d);
            Exact(4.4d, row.VolumeM3, "ordinary decimal VolumeM3");
        }

        private static void RejectsUnrepresentableFinalLoss()
        {
            Expect<OverflowException>(() => BuildTwo("LengthM", 1e16, 1d), "half-ULP final compensation");
        }

        private static void RejectsInvalidAndOverflowingInputs()
        {
            Expect<InvalidOperationException>(() => BuildTwo("LengthM", -1d, 1d), "negative contribution");
            Expect<InvalidOperationException>(() => BuildTwo("LengthM", double.NaN, 1d), "non-finite contribution");
            Expect<OverflowException>(() => BuildTwo("LengthM", double.MaxValue, double.MaxValue), "overflowing aggregate");
        }

        private static MaterialUsageRow Build(string quantityKey, double first, double second, double third)
        {
            var project = Project();
            Add(project, "a", quantityKey, first);
            Add(project, "b", quantityKey, second);
            Add(project, "c", quantityKey, third);
            return MaterialUsageScheduleBuilder.Build(project).Single();
        }

        private static MaterialUsageRow BuildTwo(string quantityKey, double first, double second)
        {
            var project = Project();
            Add(project, "a", quantityKey, first);
            Add(project, "b", quantityKey, second);
            return MaterialUsageScheduleBuilder.Build(project).Single();
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("material-precision", "Material precision");
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            var family = new ProjectFamily("wall", "Wall", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Concrete";
            project.Families.Add(family);
            return project;
        }

        private static void Add(ProjectState project, string id, string quantityKey, double value)
        {
            var element = new ProjectElement(id, ElementCategory.ArchitecturalWall, "wall", "f", "z");
            element.Quantities[quantityKey] = value;
            project.Elements.Add(element);
        }

        private static void Exact(double expected, double actual, string label)
        {
            if (actual != expected)
                throw new InvalidOperationException("Unexpected Material Usage " + label + ": expected " + expected + ", got " + actual + ".");
        }

        private static void Expect<TException>(Action action, string scenario) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + " for Material Usage " + scenario + ".");
        }
    }
}
