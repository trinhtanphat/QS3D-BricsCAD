using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphCanonicalRelationSmoke
    {
        internal static void Run()
        {
            CanonicalRelationsPreserveDeterministicGraphAndOrder();
            BlankDependenciesFailClosed();
            PaddedDependenciesFailClosed();
            CaseInsensitiveDuplicateDependenciesFailClosed();
            FailedRebuildPreservesPreviousGraph();
        }

        private static void CanonicalRelationsPreserveDeterministicGraphAndOrder()
        {
            var project = BuildProject("B");
            var beforeVersion = project.ChangeVersion;
            var graph = new DependencyGraph();

            graph.Rebuild(project.Elements);
            var direct = graph.GetDirectDependents("B");
            var ordered = graph.TopologicalDirtyOrder(project.Elements);

            Equal(1, direct.Count);
            Equal("A", direct[0]);
            Equal(2, ordered.Count);
            Equal("B", ordered[0].Id);
            Equal("A", ordered[1].Id);
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void BlankDependenciesFailClosed()
        {
            AssertMalformedFails("   ");
        }

        private static void PaddedDependenciesFailClosed()
        {
            AssertMalformedFails(" B ");
        }

        private static void CaseInsensitiveDuplicateDependenciesFailClosed()
        {
            var project = BuildPersistedProject("B", "b");
            var beforeVersion = project.ChangeVersion;
            var graph = new DependencyGraph();

            Throws<InvalidOperationException>(() => graph.Rebuild(project.Elements));
            Throws<InvalidOperationException>(() => graph.TopologicalDirtyOrder(project.Elements));
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void FailedRebuildPreservesPreviousGraph()
        {
            var graph = new DependencyGraph();
            var valid = BuildProject("B");
            graph.Rebuild(valid.Elements);

            var malformed = BuildPersistedProject(" B ");
            Throws<InvalidOperationException>(() => graph.Rebuild(malformed.Elements));

            var direct = graph.GetDirectDependents("B");
            Equal(1, direct.Count);
            Equal("A", direct.Single());
        }

        private static void AssertMalformedFails(string dependency)
        {
            var project = BuildPersistedProject(dependency);
            var beforeVersion = project.ChangeVersion;
            var graph = new DependencyGraph();

            Throws<InvalidOperationException>(() => graph.Rebuild(project.Elements));
            Throws<InvalidOperationException>(() => graph.TopologicalDirtyOrder(project.Elements));
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static ProjectState BuildProject(params string[] dependencies)
        {
            var project = new ProjectState("dependency-canonical", "Dependency Canonical");
            var a = new ProjectElement("A", ElementCategory.Room);
            foreach (var dependency in dependencies) a.DependsOn.Add(dependency);
            var b = new ProjectElement("B", ElementCategory.Room);
            project.Elements.Add(a);
            project.Elements.Add(b);
            return project;
        }

        private static ProjectState BuildPersistedProject(params string[] dependencies)
        {
            var project = new ProjectState("dependency-canonical", "Dependency Canonical");
            var a = new ProjectElement("A", ElementCategory.Room);
            foreach (var dependency in dependencies) AddPersistedDependency(a, dependency);
            var b = new ProjectElement("B", ElementCategory.Room);
            project.Elements.Add(a);
            project.Elements.Add(b);
            return project;
        }

        private static void AddPersistedDependency(ProjectElement element, string dependency)
        {
            var relationField = typeof(ProjectElement).GetField("_dependsOn", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("ProjectElement persisted dependency backing relation field is unavailable.");
            var relation = relationField.GetValue(element)
                ?? throw new Exception("ProjectElement persisted dependency backing relation is unavailable.");
            var valuesField = relation.GetType().GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("ProjectElement persisted dependency backing values field is unavailable.");
            var values = valuesField.GetValue(relation) as List<string>
                ?? throw new Exception("ProjectElement persisted dependency backing values are unavailable.");
            values.Add(dependency);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class DependencyGraphCanonicalRelationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DependencyGraphCanonicalRelationSmoke.Run();
    }
}
