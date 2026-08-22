using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditTargetInputBoundSmoke
    {
        internal static void Run()
        {
            TenThousandRepeatedObjectTargetsRemainSupported();
            KnownOversizeObjectTargetsFailReadOnly();
            LazyOversizeObjectTargetsStopAtMaxPlusOneReadOnly();
            KnownOversizeIdTargetsFailReadOnly();
            LazyOversizeIdTargetsStopAtMaxPlusOneReadOnly();
        }

        private static void TenThousandRepeatedObjectTargetsRemainSupported()
        {
            var project = BuildProject(out var element);
            var service = new BulkEditService();
            var targets = Enumerable.Repeat(element, 10000).ToArray();

            var changed = service.SetProperty(project, targets, "Note", "ok");

            Equal(1, changed.Count);
            Equal("E-1", changed[0]);
            Equal("ok", element.Properties["Note"]);
        }

        private static void KnownOversizeObjectTargetsFailReadOnly()
        {
            var project = BuildProject(out var element);
            var service = new BulkEditService();
            var beforeVersion = project.ChangeVersion;
            var targets = Enumerable.Repeat(element, 10001).ToArray();

            Throws<InvalidOperationException>(() => service.SetProperty(project, targets, "Note", "blocked"));

            Equal(beforeVersion, project.ChangeVersion);
            Equal(false, element.Properties.ContainsKey("Note"));
        }

        private static void LazyOversizeObjectTargetsStopAtMaxPlusOneReadOnly()
        {
            var project = BuildProject(out var element);
            var service = new BulkEditService();
            var beforeVersion = project.ChangeVersion;
            var observed = 0;

            Throws<InvalidOperationException>(() =>
                service.SetProperty(project, CountedElements(element, 20000, () => observed++), "Note", "blocked"));

            Equal(10001, observed);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(false, element.Properties.ContainsKey("Note"));
        }

        private static void KnownOversizeIdTargetsFailReadOnly()
        {
            var project = BuildProject(out var element);
            var service = new BulkEditService();
            var beforeVersion = project.ChangeVersion;
            var ids = Enumerable.Range(0, 10001).Select(index => "E-" + index).ToArray();

            Throws<InvalidOperationException>(() => service.SetProperty(project, ids, "Note", "blocked"));

            Equal(beforeVersion, project.ChangeVersion);
            Equal(false, element.Properties.ContainsKey("Note"));
        }

        private static void LazyOversizeIdTargetsStopAtMaxPlusOneReadOnly()
        {
            var project = BuildProject(out var element);
            var service = new BulkEditService();
            var beforeVersion = project.ChangeVersion;
            var observed = 0;

            Throws<InvalidOperationException>(() =>
                service.SetProperty(project, CountedIds(20000, () => observed++), "Note", "blocked"));

            Equal(10001, observed);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(false, element.Properties.ContainsKey("Note"));
        }

        private static ProjectState BuildProject(out ProjectElement element)
        {
            var project = new ProjectState("bulk-bound", "Bulk Bound");
            element = new ProjectElement("E-1", ElementCategory.Room);
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<ProjectElement> CountedElements(ProjectElement element, int count, Action onYield)
        {
            for (var index = 0; index < count; index++)
            {
                onYield();
                yield return element;
            }
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

    internal static class BulkEditTargetInputBoundSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BulkEditTargetInputBoundSmoke.Run();
    }
}
