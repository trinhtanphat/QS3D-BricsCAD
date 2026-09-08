using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthDependencyCanonicalRegressionSmoke
    {
        private const string NonCanonicalCode = "DEPENDENCY_REFERENCE_NON_CANONICAL";

        [ModuleInitializer]
        internal static void Initialize()
        {
            FlagsPaddedDependencyWithoutBreakingLookup();
            PreservesCanonicalDependencyBehavior();
            PreservesNormalizedDuplicateDetection();
        }

        private static void FlagsPaddedDependencyWithoutBreakingLookup()
        {
            var project = CreateProject("P-DEP-PADDED", " DEP-TARGET ");
            var issues = new ModelHealthService().Inspect(project);

            var canonicality = issues.Where(x => x.Code == NonCanonicalCode && x.ElementId == "DEP-OWNER").ToList();
            Equal(1, canonicality.Count, "padded canonicality issue count");
            Equal(HealthSeverity.Error, canonicality[0].Severity, "padded canonicality severity");
            Equal(false, issues.Any(x => x.Code == "MISSING_DEPENDENCY" && x.ElementId == "DEP-OWNER"), "padded lookup remains valid");
        }

        private static void PreservesCanonicalDependencyBehavior()
        {
            var project = CreateProject("P-DEP-CANONICAL", "DEP-TARGET");
            var issues = new ModelHealthService().Inspect(project);

            Equal(false, issues.Any(x => x.Code == NonCanonicalCode && x.ElementId == "DEP-OWNER"), "canonical reference remains clean");
            Equal(false, issues.Any(x => x.Code == "MISSING_DEPENDENCY" && x.ElementId == "DEP-OWNER"), "canonical lookup remains valid");
            Equal(false, issues.Any(x => x.Code == "DUPLICATE_DEPENDENCY" && x.ElementId == "DEP-OWNER"), "canonical single reference is not duplicate");
        }

        private static void PreservesNormalizedDuplicateDetection()
        {
            var project = CreateProject("P-DEP-DUPLICATE", "DEP-TARGET", " DEP-TARGET ");
            var issues = new ModelHealthService().Inspect(project);

            Equal(1, issues.Count(x => x.Code == NonCanonicalCode && x.ElementId == "DEP-OWNER"), "duplicate padded canonicality issue count");
            Equal(1, issues.Count(x => x.Code == "DUPLICATE_DEPENDENCY" && x.ElementId == "DEP-OWNER"), "normalized duplicate issue count");
            Equal(false, issues.Any(x => x.Code == "MISSING_DEPENDENCY" && x.ElementId == "DEP-OWNER"), "normalized duplicate lookup remains valid");
        }

        private static ProjectState CreateProject(string projectId, params string[] dependencies)
        {
            var project = new ProjectState(projectId, "Dependency canonical regression");
            var target = new ProjectElement("DEP-TARGET", ElementCategory.Railing);
            var owner = new ProjectElement("DEP-OWNER", ElementCategory.Railing);
            target.SetProperty("LengthM", "1");
            owner.SetProperty("LengthM", "1");
            foreach (var dependency in dependencies)
            {
                if (string.Equals(dependency, dependency.Trim(), StringComparison.Ordinal))
                    owner.DependsOn.Add(dependency);
                else
                    AddPersistedDependency(owner, dependency);
            }
            project.Elements.Add(target);
            project.Elements.Add(owner);
            return project;
        }

        private static void AddPersistedDependency(ProjectElement element, string dependency)
        {
            var relationField = typeof(ProjectElement).GetField("_dependsOn", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement persisted dependency backing relation field is unavailable.");
            var relation = relationField.GetValue(element)
                ?? throw new InvalidOperationException("ProjectElement persisted dependency backing relation is unavailable.");
            var valuesField = relation.GetType().GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ProjectElement persisted dependency backing values field is unavailable.");
            var values = valuesField.GetValue(relation) as List<string>
                ?? throw new InvalidOperationException("ProjectElement persisted dependency backing values are unavailable.");
            values.Add(dependency);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ModelHealthDependencyCanonicalRegressionSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}