using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementRelationMutationSmoke
    {
        internal static void Run()
        {
            SourceHandleAddMarksRelationsDirty();
            DependencyAddMarksRelationsDirty();
            NoOpRemovalPreservesCleanState();
            RelationInputsMustBeCanonical();
        }

        private static void SourceHandleAddMarksRelationsDirty()
        {
            var element = CleanElement();
            element.SourceHandles.Add("AA11");
            Has(element.Dirty, ElementDirtyFlags.Relations);
        }

        private static void DependencyAddMarksRelationsDirty()
        {
            var element = CleanElement();
            element.DependsOn.Add("E2");
            Has(element.Dirty, ElementDirtyFlags.Relations);
        }

        private static void NoOpRemovalPreservesCleanState()
        {
            var element = CleanElement();
            var before = element.UpdatedUtc;
            if (element.SourceHandles.Remove("MISSING"))
                throw new Exception("Removing a missing source handle must return false.");
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(before, element.UpdatedUtc);
        }

        private static void RelationInputsMustBeCanonical()
        {
            var source = CleanElement();
            Throws<ArgumentException>(() => source.SourceHandles.Add(" padded "));
            Equal(0, source.SourceHandles.Count);
            Equal(ElementDirtyFlags.None, source.Dirty);

            var dependency = CleanElement();
            Throws<ArgumentException>(() => dependency.DependsOn.Add("bad\nrelation"));
            Equal(0, dependency.DependsOn.Count);
            Equal(ElementDirtyFlags.None, dependency.Dirty);
        }

        private static ProjectElement CleanElement()
        {
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.MarkClean(ElementDirtyFlags.All);
            return element;
        }

        private static void Has(ElementDirtyFlags actual, ElementDirtyFlags expected)
        {
            if ((actual & expected) != expected)
                throw new Exception("Expected dirty flags " + actual + " to contain " + expected + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
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

    internal static class ProjectElementRelationMutationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectElementRelationMutationSmoke.Run();
    }
}
