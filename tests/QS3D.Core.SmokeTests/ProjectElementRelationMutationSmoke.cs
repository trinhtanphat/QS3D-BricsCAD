using System;
using System.Collections.Generic;
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
            EffectiveMutationSurfaceMarksRelationsDirty();
            RelationIdentityLookupAndRemovalUseCanonicalCaseInsensitiveIdentity();
            RelationMutationInvalidatesGeneratedOutput();
            NoOpRemovalPreservesCleanState();
            NoOpMutationsPreserveCleanState();
            RejectedMutationsAreAtomic();
            RelationInputsNormalizeAndValidate();
        }

        private static void SourceHandleAddMarksRelationsDirty()
        {
            AssertEffectiveMutation(null, element => element.SourceHandles.Add("AA11"));
        }

        private static void DependencyAddMarksRelationsDirty()
        {
            AssertEffectiveMutation(null, element => element.DependsOn.Add("E2"));
        }

        private static void EffectiveMutationSurfaceMarksRelationsDirty()
        {
            AssertEffectiveMutation(
                element => Seed(element.SourceHandles, "A"),
                element => element.SourceHandles.Insert(0, "B"));

            AssertEffectiveMutation(
                element => Seed(element.SourceHandles, "A"),
                element => element.SourceHandles[0] = "B");

            AssertEffectiveMutation(
                element => Seed(element.SourceHandles, "A"),
                element =>
                {
                    if (!element.SourceHandles.Remove(" A ")) throw new Exception("Canonicalized relation removal must succeed.");
                });

            AssertEffectiveMutation(
                element => Seed(element.DependsOn, "A", "B"),
                element => element.DependsOn.RemoveAt(0));

            AssertEffectiveMutation(
                element => Seed(element.DependsOn, "A"),
                element => element.DependsOn.Clear());
        }

        private static void RelationIdentityLookupAndRemovalUseCanonicalCaseInsensitiveIdentity()
        {
            var element = CleanElement();
            element.SourceHandles.Add("AA11");
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;

            if (!element.SourceHandles.Contains(" aa11 "))
                throw new Exception("Relation lookup must use canonical case-insensitive identity.");
            Equal(0, element.SourceHandles.IndexOf(" aa11 "));

            WaitUntilClockCanAdvance(before);
            if (!element.SourceHandles.Remove(" aa11 "))
                throw new Exception("Relation removal must use canonical case-insensitive identity.");
            Equal(0, element.SourceHandles.Count);
            Has(element.Dirty, ElementDirtyFlags.Relations);
            if (element.UpdatedUtc <= before)
                throw new Exception("Case-insensitive relation removal must advance UpdatedUtc.");
        }

        private static void RelationMutationInvalidatesGeneratedOutput()
        {
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.SetProperty("GeneratedSolidHandle", "AA11");
            element.ClearGeneratedGeometryStale();
            element.MarkClean(ElementDirtyFlags.All);
            if (element.IsGeneratedSolidStale())
                throw new Exception("Generated solid must be fresh before relation mutation regression.");

            element.DependsOn.Add("E2");

            if (!element.IsGeneratedSolidStale())
                throw new Exception("Effective relation mutation must invalidate generated solid output.");
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

        private static void NoOpMutationsPreserveCleanState()
        {
            var empty = CleanElement();
            var emptyBefore = empty.UpdatedUtc;
            empty.DependsOn.Clear();
            Equal(ElementDirtyFlags.None, empty.Dirty);
            Equal(emptyBefore, empty.UpdatedUtc);

            var identical = CleanElement();
            Seed(identical.SourceHandles, "AA11");
            identical.MarkClean(ElementDirtyFlags.All);
            var identicalBefore = identical.UpdatedUtc;
            identical.SourceHandles[0] = "AA11";
            Equal(ElementDirtyFlags.None, identical.Dirty);
            Equal(identicalBefore, identical.UpdatedUtc);

            var caseOnly = CleanElement();
            Seed(caseOnly.SourceHandles, "AA11");
            caseOnly.MarkClean(ElementDirtyFlags.All);
            var caseOnlyBefore = caseOnly.UpdatedUtc;
            caseOnly.SourceHandles[0] = "aa11";
            Equal("AA11", caseOnly.SourceHandles[0]);
            Equal(ElementDirtyFlags.None, caseOnly.Dirty);
            Equal(caseOnlyBefore, caseOnly.UpdatedUtc);
        }

        private static void RejectedMutationsAreAtomic()
        {
            var duplicate = CleanElement();
            Seed(duplicate.SourceHandles, "AA11", "BB22");
            duplicate.MarkClean(ElementDirtyFlags.All);
            var before = duplicate.UpdatedUtc;
            Throws<ArgumentException>(() => duplicate.SourceHandles.Add("aa11"));
            Equal(2, duplicate.SourceHandles.Count);
            Equal("AA11", duplicate.SourceHandles[0]);
            Equal("BB22", duplicate.SourceHandles[1]);
            Equal(ElementDirtyFlags.None, duplicate.Dirty);
            Equal(before, duplicate.UpdatedUtc);

            Throws<ArgumentException>(() => duplicate.SourceHandles[1] = " aa11 ");
            Equal("AA11", duplicate.SourceHandles[0]);
            Equal("BB22", duplicate.SourceHandles[1]);
            Equal(ElementDirtyFlags.None, duplicate.Dirty);
            Equal(before, duplicate.UpdatedUtc);

            Throws<ArgumentException>(() => duplicate.DependsOn.Remove("bad\nrelation"));
            Equal(0, duplicate.DependsOn.Count);
            Equal(ElementDirtyFlags.None, duplicate.Dirty);
            Equal(before, duplicate.UpdatedUtc);
        }

        private static void RelationInputsNormalizeAndValidate()
        {
            var source = CleanElement();
            source.SourceHandles.Add(" padded ");
            Equal("padded", source.SourceHandles[0]);
            Has(source.Dirty, ElementDirtyFlags.Relations);

            var dependency = CleanElement();
            Throws<ArgumentException>(() => dependency.DependsOn.Add("bad\nrelation"));
            Equal(0, dependency.DependsOn.Count);
            Equal(ElementDirtyFlags.None, dependency.Dirty);
        }

        private static void AssertEffectiveMutation(Action<ProjectElement>? prepare, Action<ProjectElement> mutation)
        {
            var element = CleanElement();
            prepare?.Invoke(element);
            element.MarkClean(ElementDirtyFlags.All);
            var before = element.UpdatedUtc;
            WaitUntilClockCanAdvance(before);
            mutation(element);
            Has(element.Dirty, ElementDirtyFlags.Relations);
            if (element.UpdatedUtc <= before)
                throw new Exception("Effective relation mutation must advance UpdatedUtc.");
        }

        private static void Seed(IList<string> values, params string[] items)
        {
            foreach (var item in items) values.Add(item);
        }

        private static void WaitUntilClockCanAdvance(DateTime before)
        {
            var deadline = DateTime.UtcNow.AddSeconds(2);
            while (DateTime.UtcNow <= before)
            {
                if (DateTime.UtcNow >= deadline)
                    throw new Exception("UTC clock did not advance while preparing relation mutation regression.");
            }
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
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
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
