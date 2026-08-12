using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleResolverCanonicalDependencySmoke
    {
        internal static void Run()
        {
            CanonicalDependencyTraversalPreservesOrder();
            BlankDependencyFailsReadOnly();
            PaddedDependencyFailsReadOnly();
            CaseInsensitiveDuplicateDependencyFailsReadOnly();
        }

        private static void CanonicalDependencyTraversalPreservesOrder()
        {
            var project = BuildProject("B");
            var beforeVersion = project.ChangeVersion;

            var handles = SourceHandleResolver.Resolve(project, new[] { "A" });

            Equal(2, handles.Count);
            Equal("HA", handles[0]);
            Equal("HB", handles[1]);
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void BlankDependencyFailsReadOnly() => AssertMalformedFails("   ");

        private static void PaddedDependencyFailsReadOnly() => AssertMalformedFails(" B ");

        private static void CaseInsensitiveDuplicateDependencyFailsReadOnly()
        {
            var project = BuildProject("B", "b");
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => SourceHandleResolver.Resolve(project, new[] { "A" }));

            Equal(beforeVersion, project.ChangeVersion);
            Equal("HA", project.Elements.Single(x => x.Id == "A").SourceHandles.Single());
            Equal("HB", project.Elements.Single(x => x.Id == "B").SourceHandles.Single());
        }

        private static void AssertMalformedFails(string dependency)
        {
            var project = BuildProject(dependency);
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => SourceHandleResolver.Resolve(project, new[] { "A" }));

            Equal(beforeVersion, project.ChangeVersion);
            Equal("HA", project.Elements.Single(x => x.Id == "A").SourceHandles.Single());
            Equal("HB", project.Elements.Single(x => x.Id == "B").SourceHandles.Single());
        }

        private static ProjectState BuildProject(params string[] dependencies)
        {
            var project = new ProjectState("locate-dependency-canonical", "Locate Dependency Canonical");
            var a = new ProjectElement("A", ElementCategory.Room);
            a.SourceHandles.Add("HA");
            foreach (var dependency in dependencies) a.DependsOn.Add(dependency);
            var b = new ProjectElement("B", ElementCategory.Room);
            b.SourceHandles.Add("HB");
            project.Elements.Add(a);
            project.Elements.Add(b);
            return project;
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

    internal static class SourceHandleResolverCanonicalDependencySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SourceHandleResolverCanonicalDependencySmoke.Run();
    }
}
