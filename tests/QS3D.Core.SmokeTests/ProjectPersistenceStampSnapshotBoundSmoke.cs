using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampSnapshotBoundSmoke
    {
        private const int MaximumTopLevelEntries = 100_000;
        private const int MaximumNestedEntries = 10_000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OversizedQuantityRuleCollectionFailsWithoutProjectMutation();
            OversizedNestedFamilyPropertiesFailWithoutProjectMutation();
            ExactBoundNestedFamilyPropertiesRemainSupportedAndDeterministic();
        }

        private static void OversizedQuantityRuleCollectionFailsWithoutProjectMutation()
        {
            var project = Project("stamp-oversized-rules");
            for (var index = 0; index <= MaximumTopLevelEntries; index++)
                project.QuantityRules.Add(null!);

            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            var error = Capture<InvalidOperationException>(() => new ProjectPersistenceStamp(project));

            Contains("project quantity rules", error.Message,
                "Oversized quantity-rule snapshot did not identify the bounded collection.");
            Contains("100000", error.Message,
                "Oversized quantity-rule snapshot did not report the supported top-level bound.");
            Equal(version, project.ChangeVersion,
                "Rejected persistence-stamp quantity-rule snapshot mutated project ChangeVersion.");
            Equal(updatedUtc, project.UpdatedUtc,
                "Rejected persistence-stamp quantity-rule snapshot mutated project UpdatedUtc.");
            Equal(MaximumTopLevelEntries + 1, project.QuantityRules.Count,
                "Rejected persistence-stamp quantity-rule snapshot mutated the source collection.");
        }

        private static void OversizedNestedFamilyPropertiesFailWithoutProjectMutation()
        {
            var project = Project("stamp-oversized-family-properties");
            var family = new ProjectFamily("F1", "Family 1", ElementCategory.Beam);
            project.Families.Add(family);
            FillProperties(family, MaximumNestedEntries + 1);

            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            var error = Capture<InvalidOperationException>(() => new ProjectPersistenceStamp(project));

            Contains("family properties", error.Message,
                "Oversized nested family-property snapshot did not identify the bounded collection.");
            Contains("10000", error.Message,
                "Oversized nested family-property snapshot did not report the supported bound.");
            Equal(version, project.ChangeVersion,
                "Rejected persistence-stamp family-property snapshot mutated project ChangeVersion.");
            Equal(updatedUtc, project.UpdatedUtc,
                "Rejected persistence-stamp family-property snapshot mutated project UpdatedUtc.");
            Equal(MaximumNestedEntries + 1, family.Properties.Count,
                "Rejected persistence-stamp family-property snapshot mutated the source collection.");
        }

        private static void ExactBoundNestedFamilyPropertiesRemainSupportedAndDeterministic()
        {
            var project = Project("stamp-exact-family-properties");
            var family = new ProjectFamily("F1", "Family 1", ElementCategory.Beam);
            project.Families.Add(family);
            FillProperties(family, MaximumNestedEntries);

            var version = project.ChangeVersion;
            var stamp = new ProjectPersistenceStamp(project);

            Equal(false, stamp.RequiresSave(project),
                "Exact-bound nested family properties were not accepted as a stable saved snapshot.");
            Equal(version, project.ChangeVersion,
                "Reading an exact-bound persistence stamp mutated project ChangeVersion.");

            family.Properties["P05000"] = "changed";
            Equal(version + 1L, project.ChangeVersion,
                "Direct owned family-property mutation did not advance project ChangeVersion exactly once.");
            var mutationVersion = project.ChangeVersion;

            Equal(true, stamp.RequiresSave(project),
                "Exact-bound nested family-property mutation was not detected by persistence stamp.");
            Equal(mutationVersion, project.ChangeVersion,
                "Persistence-stamp dirty detection mutated project ChangeVersion after the owned property mutation.");
        }

        private static ProjectState Project(string id) => new ProjectState(id, "Persistence stamp bounds");

        private static void FillProperties(ProjectFamily family, int count)
        {
            for (var index = 0; index < count; index++)
                family.Properties["P" + index.ToString("D5")] = "V" + index;
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}