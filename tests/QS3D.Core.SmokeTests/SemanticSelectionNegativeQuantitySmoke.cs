using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Selection;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionNegativeQuantitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DirectNegativeQuantityFailsClosedWithoutMutation();
            MixedSelectionCannotHideNegativeQuantity();
            NegativeZeroRemainsCanonicalZero();
            OrdinaryZeroAndPositiveQuantitiesRemainValid();
            NonFiniteQuantitiesStillFailClosed();
            QuantityKeyValidationPrecedesValueProjection();
        }

        private static void DirectNegativeQuantityFailsClosedWithoutMutation()
        {
            var project = BuildProject();
            var element = project.FindElement("B-001")!;
            element.Quantities["LengthM"] = -1d;
            var version = project.ChangeVersion;

            var error = MustFail(() => SemanticSelectionInspector.Inspect(project, new[] { "B-001" }));
            Contains("negative quantity", error.Message);
            Equal(-1d, element.Quantities["LengthM"]);
            Equal(version, project.ChangeVersion);
        }

        private static void MixedSelectionCannotHideNegativeQuantity()
        {
            var project = BuildProject();
            var poisoned = project.FindElement("B-002")!;
            poisoned.Quantities["LengthM"] = -0.25d;
            var version = project.ChangeVersion;

            var error = MustFail(() => SemanticSelectionInspector.Inspect(project, new[] { "B-001", "B-002" }));
            Contains("B-002/LengthM", error.Message);
            Equal(-0.25d, poisoned.Quantities["LengthM"]);
            Equal(version, project.ChangeVersion);
        }

        private static void NegativeZeroRemainsCanonicalZero()
        {
            var project = BuildProject();
            var element = project.FindElement("B-001")!;
            var negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);
            element.Quantities["LengthM"] = negativeZero;
            var version = project.ChangeVersion;

            var quantity = SemanticSelectionInspector.Inspect(project, new[] { "B-001" })
                .Quantities.Single(x => x.Name == "LengthM");
            if (!quantity.Value.HasValue) throw new Exception("Signed zero quantity must remain present.");
            Equal(0L, BitConverter.DoubleToInt64Bits(quantity.Value.Value));
            Equal(long.MinValue, BitConverter.DoubleToInt64Bits(element.Quantities["LengthM"]));
            Equal(version, project.ChangeVersion);
        }

        private static void OrdinaryZeroAndPositiveQuantitiesRemainValid()
        {
            var project = BuildProject();
            project.FindElement("B-001")!.Quantities["LengthM"] = 0d;
            project.FindElement("B-002")!.Quantities["LengthM"] = 12.5d;

            var first = SemanticSelectionInspector.Inspect(project, new[] { "B-001" })
                .Quantities.Single(x => x.Name == "LengthM");
            var second = SemanticSelectionInspector.Inspect(project, new[] { "B-002" })
                .Quantities.Single(x => x.Name == "LengthM");
            Equal(0d, first.Value!.Value);
            Equal(12.5d, second.Value!.Value);
        }

        private static void NonFiniteQuantitiesStillFailClosed()
        {
            foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                var project = BuildProject();
                var element = project.FindElement("B-001")!;
                element.Quantities["LengthM"] = invalid;
                var version = project.ChangeVersion;
                var error = MustFail(() => SemanticSelectionInspector.Inspect(project, new[] { "B-001" }));
                Contains("non-finite quantity", error.Message);
                Equal(version, project.ChangeVersion);
            }
        }

        private static void QuantityKeyValidationPrecedesValueProjection()
        {
            var project = BuildProject();
            var element = project.FindElement("B-001")!;
            element.Quantities[" BadQuantity "] = -5d;
            var version = project.ChangeVersion;

            var error = MustFail(() => SemanticSelectionInspector.Inspect(project, new[] { "B-001" }));
            Contains("non-canonical quantity name", error.Message);
            Equal(-5d, element.Quantities[" BadQuantity "]);
            Equal(version, project.ChangeVersion);
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-NEG-QTY", "Negative quantity selection smoke");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            var family = new ProjectFamily("FAM-B", "Beam", ElementCategory.Beam);
            project.Families.Add(family);

            var first = new ProjectElement("B-001", ElementCategory.Beam, "FAM-B", "F-01", "Z-A");
            first.SetQuantity("LengthM", 5d);
            var second = new ProjectElement("B-002", ElementCategory.Beam, "FAM-B", "F-01", "Z-A");
            second.SetQuantity("LengthM", 6d);
            project.Elements.Add(first);
            project.Elements.Add(second);
            return project;
        }

        private static InvalidOperationException MustFail(Action action)
        {
            try { action(); }
            catch (InvalidOperationException error) { return error; }
            throw new Exception("Expected semantic selection inspection to fail closed.");
        }

        private static void Contains(string expected, string actual)
        {
            if (actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new Exception("Expected message containing '" + expected + "' but got '" + actual + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
