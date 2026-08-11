using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementPropertyInvalidationSmoke
    {
        internal static void Run()
        {
            NonGeometryPropertyPreservesFreshGeneratedOutput();
            GeometryPropertyStalesGeneratedOutput();
            LevelReferencePropertiesStaleGeneratedOutput();
            BroadPropertyDirtyRetainsCompatibility();
            NoOpPropertyWriteDoesNotMutateState();
            NonGeometryPropertyDoesNotClearExistingStaleState();
        }

        private static void NonGeometryPropertyPreservesFreshGeneratedOutput()
        {
            var element = FreshGeneratedBeam();

            element.SetProperty("Mark", "B-01");

            Has(element.Dirty, ElementDirtyFlags.Properties);
            Has(element.Dirty, ElementDirtyFlags.Quantity);
            Lacks(element.Dirty, ElementDirtyFlags.Geometry);
            False(element.IsGeneratedGeometryStale());
        }

        private static void GeometryPropertyStalesGeneratedOutput()
        {
            var element = FreshGeneratedBeam();

            element.SetProperty("WidthM", "0.35");

            Has(element.Dirty, ElementDirtyFlags.Properties);
            Has(element.Dirty, ElementDirtyFlags.Quantity);
            Has(element.Dirty, ElementDirtyFlags.Geometry);
            True(element.IsGeneratedGeometryStale());
        }

        private static void LevelReferencePropertiesStaleGeneratedOutput()
        {
            foreach (var property in new[]
            {
                ProjectFloorService.BottomLevelIdKey,
                ProjectFloorService.BottomLevelOffsetKey,
                ProjectFloorService.TopLevelIdKey,
                ProjectFloorService.TopLevelOffsetKey
            })
            {
                var element = FreshGeneratedBeam();

                element.SetProperty(property, property.EndsWith("Id", StringComparison.Ordinal) ? "L1" : "0.1");

                Has(element.Dirty, ElementDirtyFlags.Geometry);
                True(element.IsGeneratedGeometryStale());
            }
        }

        private static void BroadPropertyDirtyRetainsCompatibility()
        {
            var element = FreshGeneratedBeam();

            element.MarkDirty(ElementDirtyFlags.Properties);

            Has(element.Dirty, ElementDirtyFlags.Properties);
            True(element.IsGeneratedGeometryStale());
        }

        private static void NoOpPropertyWriteDoesNotMutateState()
        {
            var element = FreshGeneratedBeam();
            element.SetProperty("Mark", "B-01");
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;

            element.SetProperty("Mark", "B-01");

            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);
            False(element.IsGeneratedGeometryStale());
        }

        private static void NonGeometryPropertyDoesNotClearExistingStaleState()
        {
            var element = FreshGeneratedBeam();
            element.MarkDirty(ElementDirtyFlags.Properties);
            True(element.IsGeneratedGeometryStale());
            element.MarkClean(ElementDirtyFlags.All);

            element.SetProperty("Mark", "B-02");

            Lacks(element.Dirty, ElementDirtyFlags.Geometry);
            True(element.IsGeneratedGeometryStale());
        }

        private static ProjectElement FreshGeneratedBeam()
        {
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties["GeneratedSolidHandle"] = "AA11";
            element.ClearGeneratedGeometryStale();
            element.MarkClean(ElementDirtyFlags.All);
            False(element.IsGeneratedGeometryStale());
            return element;
        }

        private static void Has(ElementDirtyFlags actual, ElementDirtyFlags expected)
        {
            if ((actual & expected) != expected)
                throw new Exception("Expected dirty flags " + actual + " to contain " + expected + ".");
        }

        private static void Lacks(ElementDirtyFlags actual, ElementDirtyFlags unexpected)
        {
            if ((actual & unexpected) != 0)
                throw new Exception("Expected dirty flags " + actual + " to exclude " + unexpected + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected condition to be false.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class ProjectElementPropertyInvalidationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectElementPropertyInvalidationSmoke.Run();
    }
}
