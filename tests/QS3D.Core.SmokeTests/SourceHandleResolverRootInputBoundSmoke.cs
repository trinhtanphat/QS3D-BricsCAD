using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleResolverRootInputBoundSmoke
    {
        internal static void Run()
        {
            TenThousandCanonicalRootsRemainSupported();
            KnownOversizeRootsFailReadOnly();
            LazyOversizeRootsStopAtMaxPlusOneReadOnly();
        }

        private static void TenThousandCanonicalRootsRemainSupported()
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;
            var roots = Enumerable.Repeat("ROOT", 9999).Concat(new[] { "root" });

            var handles = SourceHandleResolver.Resolve(project, roots);

            Equal(1, handles.Count);
            Equal("SOURCE-H", handles[0]);
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void KnownOversizeRootsFailReadOnly()
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;
            var oversized = Enumerable.Range(0, 10001).Select(index => "E-" + index).ToArray();

            Throws<InvalidOperationException>(() => SourceHandleResolver.Resolve(project, oversized));

            Equal(beforeVersion, project.ChangeVersion);
            Equal(1, project.Elements.Count);
            Equal("SOURCE-H", project.Elements[0].SourceHandles.Single());
        }

        private static void LazyOversizeRootsStopAtMaxPlusOneReadOnly()
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;
            var observed = 0;

            Throws<InvalidOperationException>(() =>
                SourceHandleResolver.Resolve(project, CountedIds(20000, () => observed++)));

            Equal(10001, observed);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(1, project.Elements.Count);
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("root-bound", "Root Bound");
            var element = new ProjectElement("ROOT", ElementCategory.Room);
            element.SourceHandles.Add("SOURCE-H");
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<string> CountedIds(int count, Action onYield)
        {
            for (var index = 0; index < count; index++)
            {
                onYield();
                yield return "E-" + index;
            }
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

    internal static class SourceHandleResolverRootInputBoundSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SourceHandleResolverRootInputBoundSmoke.Run();
    }
}
