using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyImpactStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            StructuralReplacementDuringRootEnumerationFailsFreshness();
            StableStructureStillPlansImpact();
        }

        private static void StructuralReplacementDuringRootEnumerationFailsFreshness()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var originalChild = project.Elements[1];

            IEnumerable<string> Roots()
            {
                var replacement = Element(originalChild.Id, "ROOT");
                project.Elements[1] = replacement;
                yield return "ROOT";
            }

            ThrowsStructuralFreshness(() => new DependencyImpactPlanner().Plan(project, Roots()));
            Equal(beforeVersion, project.ChangeVersion, "direct structural replacement must not rely on ChangeVersion");
            if (ReferenceEquals(project.Elements[1], originalChild))
                throw new InvalidOperationException("DependencyImpactStructuralFreshnessSmoke replacement fixture did not mutate element identity.");
        }

        private static void StableStructureStillPlansImpact()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var plan = new DependencyImpactPlanner().Plan(project, new[] { "ROOT" });

            Equal(beforeVersion, plan.SourceChangeVersion, "stable plan source revision");
            Equal(beforeVersion, project.ChangeVersion, "stable planning must remain read-only");
            Equal(1, plan.TotalCount, "stable impact count");
            Equal("CHILD", plan.Entries[0].ElementId, "stable dependent id");
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-DEPENDENCY-STRUCTURAL-FRESHNESS", "Dependency structural freshness");
            project.Elements.Add(Element("ROOT"));
            project.Elements.Add(Element("CHILD", "ROOT"));
            return project;
        }

        private static ProjectElement Element(string id, params string[] dependencies)
        {
            var element = new ProjectElement(id, ElementCategory.CustomQuantity);
            foreach (var dependency in dependencies) element.DependsOn.Add(dependency);
            return element;
        }

        private static void ThrowsStructuralFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("element structure changed", StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Unexpected dependency impact structural-freshness error.", ex);
            }

            throw new InvalidOperationException("Expected dependency impact structural-freshness rejection.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "DependencyImpactStructuralFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
