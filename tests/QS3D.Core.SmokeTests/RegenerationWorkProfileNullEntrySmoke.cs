using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfileNullEntrySmoke
    {
        internal static void Run()
        {
            RejectsNullTargetId();
            RejectsNullWorkItem();
            RejectsNullCategoryWork();
            ValidProfileRemainsUsable();
        }

        private static void RejectsNullTargetId()
        {
            Throws<ArgumentException>(() => Create(
                new string[] { null! },
                Array.Empty<RegenerationWorkItem>(),
                Array.Empty<RegenerationCategoryWork>()));
        }

        private static void RejectsNullWorkItem()
        {
            Throws<ArgumentException>(() => Create(
                Array.Empty<string>(),
                new RegenerationWorkItem[] { null! },
                Array.Empty<RegenerationCategoryWork>()));
        }

        private static void RejectsNullCategoryWork()
        {
            Throws<ArgumentException>(() => Create(
                Array.Empty<string>(),
                Array.Empty<RegenerationWorkItem>(),
                new RegenerationCategoryWork[] { null! }));
        }

        private static void ValidProfileRemainsUsable()
        {
            var item = new RegenerationWorkItem(
                0,
                "E-1",
                ElementCategory.ArchitecturalWall,
                ElementDirtyFlags.Geometry,
                0,
                0,
                0);
            var category = new RegenerationCategoryWork(ElementCategory.ArchitecturalWall, 1, 0);
            var profile = Create(new[] { "E-1" }, new[] { item }, new[] { category });

            if (profile.PlannedElementCount != 1 || profile.SemanticDirtyElementCount != 0 || profile.GeometryOnlyDirtyElementCount != 1)
                throw new InvalidOperationException("Valid regeneration work profile metrics changed unexpectedly.");
        }

        private static RegenerationWorkProfile Create(
            string[] targetElementIds,
            RegenerationWorkItem[] items,
            RegenerationCategoryWork[] categories)
        {
            return new RegenerationWorkProfile(
                "P-REGEN-PROFILE",
                0L,
                RegenerationWorkScope.Project,
                targetElementIds,
                1,
                0,
                items,
                categories,
                0,
                0);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
