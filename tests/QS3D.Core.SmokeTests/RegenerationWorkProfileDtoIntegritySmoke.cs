using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfileDtoIntegritySmoke
    {
        public static void Run()
        {
            WorkItemRejectsUndefinedCategory();
            WorkItemRejectsUnknownDirtyBit();
            WorkItemRejectsNegativeDependencyMetrics();
            CategoryWorkRejectsUndefinedCategory();
            ProfileRejectsUndefinedScope();
            ValidCombinedDirtyFlagsRemainSupported();
        }

        private static void WorkItemRejectsUndefinedCategory()
        {
            Throws<ArgumentOutOfRangeException>(() => NewItem((ElementCategory)999, ElementDirtyFlags.Geometry, 0, 0, 0));
        }

        private static void WorkItemRejectsUnknownDirtyBit()
        {
            Throws<ArgumentOutOfRangeException>(() => NewItem(ElementCategory.Beam, (ElementDirtyFlags)16, 0, 0, 0));
        }

        private static void WorkItemRejectsNegativeDependencyMetrics()
        {
            Throws<ArgumentOutOfRangeException>(() => NewItem(ElementCategory.Beam, ElementDirtyFlags.Geometry, -1, 0, 0));
            Throws<ArgumentOutOfRangeException>(() => NewItem(ElementCategory.Beam, ElementDirtyFlags.Geometry, 0, -1, 0));
            Throws<ArgumentOutOfRangeException>(() => NewItem(ElementCategory.Beam, ElementDirtyFlags.Geometry, 0, 0, -1));
        }

        private static void CategoryWorkRejectsUndefinedCategory()
        {
            Throws<ArgumentOutOfRangeException>(() => new RegenerationCategoryWork((ElementCategory)999, 1, 0));
        }

        private static void ProfileRejectsUndefinedScope()
        {
            Throws<ArgumentOutOfRangeException>(() => new RegenerationWorkProfile(
                "P",
                0L,
                (RegenerationWorkScope)999,
                Array.Empty<string>(),
                0,
                0,
                Array.Empty<RegenerationWorkItem>(),
                Array.Empty<RegenerationCategoryWork>(),
                0,
                0));
        }

        private static void ValidCombinedDirtyFlagsRemainSupported()
        {
            var item = NewItem(
                ElementCategory.Beam,
                ElementDirtyFlags.Geometry | ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity,
                0,
                0,
                0);
            if (!item.HasSemanticDirtyWork)
                throw new InvalidOperationException("Valid combined semantic dirty flags no longer report semantic work.");
        }

        private static RegenerationWorkItem NewItem(
            ElementCategory category,
            ElementDirtyFlags dirtyFlags,
            int dependencyDepth,
            int dependencyCount,
            int dependentCount)
        {
            return new RegenerationWorkItem(0, "E1", category, dirtyFlags, dependencyDepth, dependencyCount, dependentCount);
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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class RegenerationWorkProfileDtoIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RegenerationWorkProfileDtoIntegritySmoke.Run();
        }
    }
}
