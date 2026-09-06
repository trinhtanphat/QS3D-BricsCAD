using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyPropertyAdmissionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsNonPersistablePropertyKeysWithoutMutation();
            RejectsXmlInvalidPropertyValuesWithoutMutation();
            RejectsNonPersistableAddWithoutMutation();
            RejectsOversizedPropertyStateWithoutMutation();
            PreservesMaximumPersistableLengths();
            NormalizesNullPropertyValueBeforeMutation();
            PreservesCaseInsensitiveAndDuplicateSemantics();
            PreservesRemoveAndClearMutationSemantics();
        }

        private static void RejectsNonPersistablePropertyKeysWithoutMutation()
        {
            var invalidKeys = new[] { " ", " padded ", "bad\u0001key" };
            foreach (var invalidKey in invalidKeys)
            {
                var project = CreateProjectWithFamily(out var family);
                var beforeVersion = project.ChangeVersion;
                var beforeUpdatedUtc = project.UpdatedUtc;

                Throws<ArgumentException>(() => family.Properties[invalidKey] = "value");

                Equal(0, family.Properties.Count, "property count after rejected key");
                Equal(beforeVersion, project.ChangeVersion, "change version after rejected key");
                Equal(beforeUpdatedUtc, project.UpdatedUtc, "updatedUtc after rejected key");
            }
        }

        private static void RejectsXmlInvalidPropertyValuesWithoutMutation()
        {
            var project = CreateProjectWithFamily(out var family);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            Throws<ArgumentException>(() => family.Properties["FireRating"] = "bad\u0001value");

            Equal(0, family.Properties.Count, "property count after rejected value");
            Equal(beforeVersion, project.ChangeVersion, "change version after rejected value");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "updatedUtc after rejected value");
        }

        private static void RejectsNonPersistableAddWithoutMutation()
        {
            var invalidKeyProject = CreateProjectWithFamily(out var invalidKeyFamily);
            var invalidKeyVersion = invalidKeyProject.ChangeVersion;
            var invalidKeyUpdatedUtc = invalidKeyProject.UpdatedUtc;

            Throws<ArgumentException>(() => invalidKeyFamily.Properties.Add(" FireRating ", "60"));

            Equal(0, invalidKeyFamily.Properties.Count, "property count after rejected Add key");
            Equal(invalidKeyVersion, invalidKeyProject.ChangeVersion, "change version after rejected Add key");
            Equal(invalidKeyUpdatedUtc, invalidKeyProject.UpdatedUtc, "updatedUtc after rejected Add key");

            var invalidValueProject = CreateProjectWithFamily(out var invalidValueFamily);
            var invalidValueVersion = invalidValueProject.ChangeVersion;
            var invalidValueUpdatedUtc = invalidValueProject.UpdatedUtc;

            Throws<ArgumentException>(() => invalidValueFamily.Properties.Add("FireRating", "bad\u0001value"));

            Equal(0, invalidValueFamily.Properties.Count, "property count after rejected Add value");
            Equal(invalidValueVersion, invalidValueProject.ChangeVersion, "change version after rejected Add value");
            Equal(invalidValueUpdatedUtc, invalidValueProject.UpdatedUtc, "updatedUtc after rejected Add value");
        }

        private static void RejectsOversizedPropertyStateWithoutMutation()
        {
            var oversizedKeyProject = CreateProjectWithFamily(out var oversizedKeyFamily);
            var oversizedKeyVersion = oversizedKeyProject.ChangeVersion;
            var oversizedKeyUpdatedUtc = oversizedKeyProject.UpdatedUtc;

            Throws<ArgumentException>(() => oversizedKeyFamily.Properties[new string('K', 121)] = "value");

            Equal(0, oversizedKeyFamily.Properties.Count, "property count after oversized key");
            Equal(oversizedKeyVersion, oversizedKeyProject.ChangeVersion, "change version after oversized key");
            Equal(oversizedKeyUpdatedUtc, oversizedKeyProject.UpdatedUtc, "updatedUtc after oversized key");

            var oversizedValueProject = CreateProjectWithFamily(out var oversizedValueFamily);
            var oversizedValueVersion = oversizedValueProject.ChangeVersion;
            var oversizedValueUpdatedUtc = oversizedValueProject.UpdatedUtc;

            Throws<ArgumentException>(() => oversizedValueFamily.Properties["Description"] = new string('V', 1001));

            Equal(0, oversizedValueFamily.Properties.Count, "property count after oversized value");
            Equal(oversizedValueVersion, oversizedValueProject.ChangeVersion, "change version after oversized value");
            Equal(oversizedValueUpdatedUtc, oversizedValueProject.UpdatedUtc, "updatedUtc after oversized value");
        }

        private static void PreservesMaximumPersistableLengths()
        {
            var project = CreateProjectWithFamily(out var family);
            var key = new string('K', 120);
            var value = new string('V', 1000);
            var beforeVersion = project.ChangeVersion;

            family.Properties[key] = value;

            Equal(1, family.Properties.Count, "property count at maximum persistence bounds");
            Equal(value, family.Properties[key], "property value at maximum persistence bounds");
            Equal(beforeVersion + 1L, project.ChangeVersion, "change version at maximum persistence bounds");
        }

        private static void NormalizesNullPropertyValueBeforeMutation()
        {
            var project = CreateProjectWithFamily(out var family);
            var beforeVersion = project.ChangeVersion;

#pragma warning disable CS8625
            family.Properties["Description"] = null;
#pragma warning restore CS8625

            Equal(string.Empty, family.Properties["Description"], "normalized null property value");
            Equal(beforeVersion + 1L, project.ChangeVersion, "change version after normalized null write");

#pragma warning disable CS8625
            family.Properties["Description"] = null;
#pragma warning restore CS8625
            Equal(beforeVersion + 1L, project.ChangeVersion, "same normalized null value must be a no-op");
        }

        private static void PreservesCaseInsensitiveAndDuplicateSemantics()
        {
            var project = CreateProjectWithFamily(out var family);
            var beforeVersion = project.ChangeVersion;

            family.Properties.Add("FireRating", "60");
            Equal(beforeVersion + 1L, project.ChangeVersion, "change version after Add");
            Equal("60", family.Properties["firerating"], "case-insensitive lookup");

            family.Properties["FIRERATING"] = "90";
            Equal(beforeVersion + 2L, project.ChangeVersion, "change version after case-insensitive replacement");
            Equal("90", family.Properties["FireRating"], "replacement value");
            Equal(1, family.Properties.Count, "case-insensitive replacement count");

            var beforeDuplicateVersion = project.ChangeVersion;
            Throws<ArgumentException>(() => family.Properties.Add("firerating", "120"));
            Equal(beforeDuplicateVersion, project.ChangeVersion, "duplicate Add must not mutate persistence state");
            Equal("90", family.Properties["FireRating"], "duplicate Add must preserve value");
        }

        private static void PreservesRemoveAndClearMutationSemantics()
        {
            var project = CreateProjectWithFamily(out var family);
            family.Properties.Add("A", "1");
            family.Properties.Add("B", "2");
            var beforeRemoveVersion = project.ChangeVersion;

            Equal(false, family.Properties.Remove("missing"), "missing remove result");
            Equal(beforeRemoveVersion, project.ChangeVersion, "missing remove must be a no-op");
            Equal(true, family.Properties.Remove("a"), "case-insensitive remove result");
            Equal(beforeRemoveVersion + 1L, project.ChangeVersion, "successful remove mutation");

            family.Properties.Clear();
            Equal(beforeRemoveVersion + 2L, project.ChangeVersion, "clear mutation");
            Equal(0, family.Properties.Count, "clear count");

            family.Properties.Clear();
            Equal(beforeRemoveVersion + 2L, project.ChangeVersion, "empty clear must be a no-op");
        }

        private static ProjectState CreateProjectWithFamily(out ProjectFamily family)
        {
            var project = new ProjectState("P-FAMILY-PROPERTY-ADMISSION", "Family property admission regression");
            family = new ProjectFamily("F-WALL", "Wall", ElementCategory.ArchitecturalWall);
            project.Families.Add(family);
            return project;
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "ProjectFamilyPropertyAdmissionSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
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

            throw new InvalidOperationException(
                "ProjectFamilyPropertyAdmissionSmoke expected " + typeof(TException).Name + ".");
        }
    }
}