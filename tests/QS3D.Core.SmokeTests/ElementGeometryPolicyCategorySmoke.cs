using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementGeometryPolicyCategorySmoke
    {
        internal static void Run()
        {
            UndefinedCategoryFailsClosed();
            DefinedCategoryBehaviorRemainsStable();
        }

        private static void UndefinedCategoryFailsClosed()
        {
            var invalid = (ElementCategory)int.MaxValue;
            Throws<ArgumentOutOfRangeException>(() => ElementGeometryPolicy.RequiresGeneratedGeometry(invalid));
            Throws<ArgumentOutOfRangeException>(() => ElementGeometryPolicy.AffectsGeneratedGeometry(invalid, "LengthM"));
            Throws<ArgumentOutOfRangeException>(() => ElementGeometryPolicy.AffectsGeneratedOutput(invalid, "Material"));
            Throws<ArgumentOutOfRangeException>(() => ElementGeometryPolicy.SemanticCleanFlags(invalid));
        }

        private static void DefinedCategoryBehaviorRemainsStable()
        {
            True(ElementGeometryPolicy.RequiresGeneratedGeometry(ElementCategory.Beam));
            False(ElementGeometryPolicy.RequiresGeneratedGeometry(ElementCategory.Grid));
            True(ElementGeometryPolicy.AffectsGeneratedGeometry(ElementCategory.Beam, "  LengthM  "));
            True(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.Beam, " material "));
            False(ElementGeometryPolicy.AffectsGeneratedOutput(ElementCategory.Grid, "LengthM"));

            var beamClean = ElementGeometryPolicy.SemanticCleanFlags(ElementCategory.Beam);
            var gridClean = ElementGeometryPolicy.SemanticCleanFlags(ElementCategory.Grid);
            False((beamClean & ElementDirtyFlags.Geometry) != 0);
            True((gridClean & ElementDirtyFlags.Geometry) != 0);
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected false.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ElementGeometryPolicyCategorySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ElementGeometryPolicyCategorySmoke.Run();
    }
}
