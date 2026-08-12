using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleOwnershipSelectionBoundSmoke
    {
        internal static void Run()
        {
            TenThousandRawEntriesRemainSupportedAndNormalized();
            KnownOversizeSelectionFailsReadOnly();
            LazyOversizeSelectionStopsAtMaxPlusOneReadOnly();
        }

        private static void TenThousandRawEntriesRemainSupportedAndNormalized()
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;
            var handles = Enumerable.Repeat("   ", 9997)
                .Concat(new[] { " owned ", "OWNED", "missing" });

            var owners = SemanticHandleOwnershipResolver.Resolve(project, handles);

            Equal(1, owners.Count);
            Equal("E-1", owners[0].Id);
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void KnownOversizeSelectionFailsReadOnly()
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;
            var oversized = Enumerable.Range(0, 10001).Select(index => "H-" + index).ToArray();

            Throws<InvalidOperationException>(() => SemanticHandleOwnershipResolver.Resolve(project, oversized));

            Equal(beforeVersion, project.ChangeVersion);
            Equal(1, project.Elements.Count);
            Equal("OWNED", project.Elements[0].SourceHandles.Single());
        }

        private static void LazyOversizeSelectionStopsAtMaxPlusOneReadOnly()
        {
            var project = BuildProject();
            var beforeVersion = project.ChangeVersion;
            var observed = 0;

            Throws<InvalidOperationException>(() =>
                SemanticHandleOwnershipResolver.Resolve(project, CountedHandles(20000, () => observed++)));

            Equal(10001, observed);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(1, project.Elements.Count);
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("selection-bound", "Selection Bound");
            var element = new ProjectElement("E-1", ElementCategory.Room);
            element.SourceHandles.Add("OWNED");
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<string> CountedHandles(int count, Action onYield)
        {
            for (var index = 0; index < count; index++)
            {
                onYield();
                yield return "H-" + index;
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

    internal static class SemanticHandleOwnershipSelectionBoundSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticHandleOwnershipSelectionBoundSmoke.Run();
    }
}
