using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyHealthSmoke
    {
        public static void Run()
        {
            AcyclicChainPasses();
            NullElementFailsVisible();
            SelfReferenceIsReported();
            MultiElementCycleReportsOnlyCycleMembers();
            MissingDependencyIsNotMisclassifiedAsCycle();
            DuplicateDependencyTargetIsReportedAsAmbiguous();
        }

        private static void AcyclicChainPasses()
        {
            var project = Project("acyclic");
            var a = Element("A");
            var b = Element("B");
            var c = Element("C");
            b.DependsOn.Add("A");
            c.DependsOn.Add("B");
            project.Elements.Add(a);
            project.Elements.Add(b);
            project.Elements.Add(c);
            var issues = new DependencyHealthService().Inspect(project);
            Require(!issues.Any(), "acyclic dependency chain must pass");
        }

        private static void NullElementFailsVisible()
        {
            var project = Project("null-element");
            project.Elements.Add(Element("A"));
            project.Elements.Add(null!);
            Throws<InvalidOperationException>(() => new DependencyHealthService().Inspect(project));

            var aggregateIssues = new ComprehensiveModelHealthService().Inspect(project);
            Require(aggregateIssues.Any(x =>
                    x.Code == "HEALTH_PROVIDER_FAILED" &&
                    (x.Message ?? string.Empty).IndexOf("DependencyHealthService", StringComparison.Ordinal) >= 0),
                "comprehensive health must surface dependency provider failure for a null semantic element");
        }

        private static void SelfReferenceIsReported()
        {
            var project = Project("self");
            var a = Element("A");
            a.DependsOn.Add("a");
            project.Elements.Add(a);
            var issues = new DependencyHealthService().Inspect(project);
            Require(issues.Count(x => x.Code == "DEPENDENCY_SELF_REFERENCE" && x.ElementId == "A") == 1,
                "self dependency must be reported exactly once");
            Require(!issues.Any(x => x.Code == "DEPENDENCY_CYCLE"), "self dependency should not be duplicated as a generic cycle");
        }

        private static void MultiElementCycleReportsOnlyCycleMembers()
        {
            var project = Project("cycle");
            var a = Element("A");
            var b = Element("B");
            var c = Element("C");
            var downstream = Element("D");
            a.DependsOn.Add("B");
            b.DependsOn.Add("C");
            c.DependsOn.Add("A");
            downstream.DependsOn.Add("A");
            project.Elements.Add(a);
            project.Elements.Add(b);
            project.Elements.Add(c);
            project.Elements.Add(downstream);

            var issues = new DependencyHealthService().Inspect(project)
                .Where(x => x.Code == "DEPENDENCY_CYCLE")
                .ToArray();
            Require(issues.Length == 3, "three-node cycle must report exactly its three members");
            Require(issues.Any(x => x.ElementId == "A") && issues.Any(x => x.ElementId == "B") && issues.Any(x => x.ElementId == "C"),
                "all cycle members must be reported");
            Require(!issues.Any(x => x.ElementId == "D"), "element depending on a cycle is blocked by it but is not itself a cycle member");
        }

        private static void MissingDependencyIsNotMisclassifiedAsCycle()
        {
            var project = Project("missing");
            var a = Element("A");
            a.DependsOn.Add("MISSING");
            project.Elements.Add(a);
            var issues = new DependencyHealthService().Inspect(project);
            Require(issues.Count(x => x.Code == "DEPENDENCY_TARGET_MISSING" && x.Severity == HealthSeverity.Error && x.ElementId == "A") == 1,
                "missing dependency must be reported exactly once as an error on the referencing element");
            Require(!issues.Any(x => x.Code == "DEPENDENCY_CYCLE" || x.Code == "DEPENDENCY_SELF_REFERENCE"),
                "missing dependency must not be misclassified as a cycle or self-reference");
        }

        private static void DuplicateDependencyTargetIsReportedAsAmbiguous()
        {
            var project = Project("ambiguous");
            var owner = Element("OWNER");
            owner.DependsOn.Add("DUP");
            project.Elements.Add(owner);
            project.Elements.Add(Element("DUP"));
            project.Elements.Add(Element("dup"));

            var issues = new DependencyHealthService().Inspect(project);
            Require(issues.Count(x => x.Code == "DEPENDENCY_TARGET_AMBIGUOUS" && x.ElementId == owner.Id) == 1,
                "dependency targeting a duplicate semantic ID must fail closed as one ambiguous-target issue");
            Require(!issues.Any(x => x.Code == "DEPENDENCY_CYCLE"),
                "an ambiguous dependency target must not be traversed or misclassified as a cycle");
        }

        private static ProjectState Project(string id) => new ProjectState(id, id);

        private static ProjectElement Element(string id) =>
            new ProjectElement(id, ElementCategory.CustomQuantity, string.Empty, string.Empty, string.Empty);

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
            throw new InvalidOperationException("DependencyHealthSmoke: expected " + typeof(TException).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("DependencyHealthSmoke: " + message);
        }
    }
}
