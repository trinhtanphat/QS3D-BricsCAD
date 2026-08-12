using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyHealthCanonicalRelationSmoke
    {
        internal static void Run()
        {
            PaddedExistingDependencyIsBlockingAndNotNormalized();
            DuplicateCanonicalDependencyIsBlockingAndFirstEdgeStillParticipatesInCycleAnalysis();
            MissingTargetContractSurvivesSeparatePaddedTokens();
            CanonicalUniqueDependencyRemainsHealthy();
        }

        private static void PaddedExistingDependencyIsBlockingAndNotNormalized()
        {
            var project = Project("padded");
            var a = Element("A", " B ");
            var b = Element("B");
            project.Elements.Add(a);
            project.Elements.Add(b);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = a.UpdatedUtc;
            var beforeDependency = a.DependsOn[0];

            var issues = new DependencyHealthService().Inspect(project);

            Require(issues.Count(x => x.Code == "DEPENDENCY_TARGET_NON_CANONICAL" && x.Severity == HealthSeverity.Error && x.ElementId == "A") == 1,
                "padded dependency must produce exactly one non-canonical Error");
            Require(!issues.Any(x => x.Code == "DEPENDENCY_TARGET_MISSING" || x.Code == "DEPENDENCY_CYCLE"),
                "padded existing dependency must not be normalized into missing/cycle traversal");
            Equal(beforeDependency, a.DependsOn[0], "health inspection rewrote padded dependency text");
            Equal(beforeVersion, project.ChangeVersion, "health inspection changed project ChangeVersion");
            Equal(beforeUpdated, a.UpdatedUtc, "health inspection changed element UpdatedUtc");
        }

        private static void DuplicateCanonicalDependencyIsBlockingAndFirstEdgeStillParticipatesInCycleAnalysis()
        {
            var project = Project("duplicate");
            var a = Element("A", "B", "b", "B");
            var b = Element("B", "A");
            project.Elements.Add(a);
            project.Elements.Add(b);

            var issues = new DependencyHealthService().Inspect(project);

            Require(issues.Count(x => x.Code == "DEPENDENCY_TARGET_DUPLICATE" && x.Severity == HealthSeverity.Error && x.ElementId == "A") == 1,
                "case-insensitive repeated dependency identity must produce exactly one duplicate Error");
            Require(issues.Count(x => x.Code == "DEPENDENCY_CYCLE") == 2,
                "the first canonical dependency edge must remain available for cycle analysis");
            Require(issues.Any(x => x.Code == "DEPENDENCY_CYCLE" && x.ElementId == "A") &&
                    issues.Any(x => x.Code == "DEPENDENCY_CYCLE" && x.ElementId == "B"),
                "both cycle members must still be diagnosed when one edge is duplicated");
        }

        private static void MissingTargetContractSurvivesSeparatePaddedTokens()
        {
            var project = Project("missing-with-padding");
            var source = Element("SOURCE", " missing-target ", "MISSING-TARGET", " existing ");
            project.Elements.Add(source);
            project.Elements.Add(Element("EXISTING"));

            var issues = new DependencyHealthService().Inspect(project);

            Require(issues.Count(x => x.Code == "DEPENDENCY_TARGET_MISSING" && x.Severity == HealthSeverity.Error && x.ElementId == "SOURCE") == 1,
                "canonical missing target must still produce exactly one missing Error");
            Require(issues.Count(x => x.Code == "DEPENDENCY_TARGET_NON_CANONICAL" && x.Severity == HealthSeverity.Error && x.ElementId == "SOURCE") == 2,
                "the two padded tokens must be diagnosed independently as non-canonical");
            Require(!issues.Any(x => x.Code == "DEPENDENCY_CYCLE" || x.Code == "DEPENDENCY_SELF_REFERENCE"),
                "invalid/missing dependency tokens must not be misclassified as cycles");
        }

        private static void CanonicalUniqueDependencyRemainsHealthy()
        {
            var project = Project("canonical");
            project.Elements.Add(Element("A", "B"));
            project.Elements.Add(Element("B"));

            var issues = new DependencyHealthService().Inspect(project);

            Require(!issues.Any(), "canonical unique dependency graph must remain healthy");
        }

        private static ProjectState Project(string id) => new ProjectState(id, id);

        private static ProjectElement Element(string id, params string[] dependencies)
        {
            var element = new ProjectElement(id, ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);
            foreach (var dependency in dependencies) element.DependsOn.Add(dependency);
            return element;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("DependencyHealthCanonicalRelationSmoke: " + message + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("DependencyHealthCanonicalRelationSmoke: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class DependencyHealthCanonicalRelationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DependencyHealthCanonicalRelationSmoke.Run();
    }
}
