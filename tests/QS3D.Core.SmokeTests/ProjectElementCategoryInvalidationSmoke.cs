using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementCategoryInvalidationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var element = new ProjectElement("E-CATEGORY", ElementCategory.Beam, "FAM", string.Empty, string.Empty);
            element.Properties["GeneratedSolidHandle"] = "AB";
            element.MarkClean(ElementDirtyFlags.All);
            Equal(ElementDirtyFlags.None, element.Dirty, "clean baseline");
            False(element.IsGeneratedGeometryStale(), "clean baseline stale state");

            element.Category = ElementCategory.Column;
            Equal(ElementCategory.Column, element.Category, "real category reassignment");
            Equal(ElementDirtyFlags.All, element.Dirty, "real category dirty flags");
            True(element.IsGeneratedSolidStale(), "real category generated-solid stale state");
            Equal("FAM", element.FamilyId, "category reassignment must not rewrite FamilyId");

            element.ClearGeneratedGeometryStale();
            element.MarkClean(ElementDirtyFlags.All);
            element.Category = ElementCategory.Column;
            Equal(ElementDirtyFlags.None, element.Dirty, "same-category no-op dirty flags");
            False(element.IsGeneratedGeometryStale(), "same-category no-op stale state");

            Throws<ArgumentOutOfRangeException>(() => element.Category = (ElementCategory)int.MaxValue);
            Equal(ElementCategory.Column, element.Category, "invalid category preserves previous value");
            Equal(ElementDirtyFlags.None, element.Dirty, "invalid category preserves dirty flags");
            False(element.IsGeneratedGeometryStale(), "invalid category preserves stale state");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new Exception("ProjectElementCategoryInvalidationSmoke expected true: " + label + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("ProjectElementCategoryInvalidationSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectElementCategoryInvalidationSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ProjectElementCategoryInvalidationSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
