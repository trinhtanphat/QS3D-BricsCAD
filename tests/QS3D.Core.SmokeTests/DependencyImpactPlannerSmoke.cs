using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyImpactPlannerSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ImpactPlanIsDeterministicAndReadOnly();
            MultipleRootsUseStableShortestCause();
            InvalidRootsFailClosed();
            OverBoundRootEnumerationStopsAtProjectCardinality();
        }

        private static void ImpactPlanIsDeterministicAndReadOnly()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = project.UpdatedUtc;
            var plan = new DependencyImpactPlanner().Plan(project, new[] { "ROOT" });

            Equal(project.ProjectId, plan.ProjectId);
            Equal(beforeVersion, plan.SourceChangeVersion);
            Equal(1, plan.RootElementIds.Count);
            Equal("ROOT", plan.RootElementIds[0]);
            Equal(5, plan.TotalCount);
            Equal(2, plan.DirectCount);
            Equal(3, plan.MaxDepth);
            Equal(new[] { "A", "B", "C", "D", "E" }, plan.Entries.Select(x => x.ElementId).ToArray());
            Equal(new[] { 1, 1, 2, 2, 3 }, plan.Entries.Select(x => x.Depth).ToArray());
            Equal("A", plan.Entries.Single(x => x.ElementId == "C").CauseElementId);
            Equal("ROOT", plan.Entries.Single(x => x.ElementId == "E").RootElementId);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeUpdated, project.UpdatedUtc);
        }

        private static void MultipleRootsUseStableShortestCause()
        {
            var project = Fixture();
            var second = Element("SECOND");
            var shared = project.FindElement("B")!;
            shared.DependsOn.Add("SECOND");
            project.Elements.Add(second);

            var plan = new DependencyImpactPlanner().Plan(project, new[] { "SECOND", "ROOT" });
            Equal(new[] { "ROOT", "SECOND" }, plan.RootElementIds.ToArray());
            var b = plan.Entries.Single(x => x.ElementId == "B");
            Equal(1, b.Depth);
            Equal("ROOT", b.RootElementId);
            Equal("ROOT", b.CauseElementId);
        }

        private static void InvalidRootsFailClosed()
        {
            var project = Fixture();
            var planner = new DependencyImpactPlanner();
            Throws<ArgumentException>(() => planner.Plan(project, Array.Empty<string>()));
            Throws<ArgumentException>(() => planner.Plan(project, new[] { " " }));
            Throws<ArgumentException>(() => planner.Plan(project, new[] { " ROOT " }));
            Throws<ArgumentException>(() => planner.Plan(project, new[] { "ROOT", "root" }));
            Throws<InvalidOperationException>(() => planner.Plan(project, new[] { "MISSING" }));
        }

        private static void OverBoundRootEnumerationStopsAtProjectCardinality()
        {
            var project = Fixture();
            var yielded = 0;

            IEnumerable<string> Roots()
            {
                for (var i = 0; i <= project.Elements.Count; i++)
                {
                    yielded++;
                    yield return "ROOT-" + i;
                }
                throw new Exception("Dependency impact planner enumerated beyond the first impossible root.");
            }

            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(project, Roots()));
            Equal(project.Elements.Count + 1, yielded);
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-IMPACT", "Dependency impact");
            project.Elements.Add(Element("ROOT"));
            project.Elements.Add(Element("B", "ROOT"));
            project.Elements.Add(Element("A", "ROOT"));
            project.Elements.Add(Element("D", "B"));
            project.Elements.Add(Element("C", "A", "B"));
            project.Elements.Add(Element("E", "C", "D"));
            return project;
        }

        private static ProjectElement Element(string id, params string[] dependencies)
        {
            var element = new ProjectElement(id, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            foreach (var dependency in dependencies) element.DependsOn.Add(dependency);
            return element;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (expected is Array expectedArray && actual is Array actualArray)
            {
                if (expectedArray.Length != actualArray.Length) throw new Exception("Array lengths differ.");
                for (var i = 0; i < expectedArray.Length; i++)
                    if (!Equals(expectedArray.GetValue(i), actualArray.GetValue(i))) throw new Exception("Array values differ at index " + i + ".");
                return;
            }
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
