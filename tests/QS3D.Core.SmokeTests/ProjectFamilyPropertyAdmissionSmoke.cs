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
            NormalizesNullPropertyValueBeforeMutation();
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

        private static ProjectState CreateProjectWithFamily(out ProjectFamily family)
        {
            var project = new ProjectState("P-FAMILY-PROPERTY-ADMISSION", "Family property admission regression");
            family = new ProjectFamily("F-WALL", "Wall", ElementCategory.Wall);
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
