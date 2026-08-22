using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantityReportSelectionStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RemovalAfterYieldFailsClosed();
            ReplacementAfterYieldFailsClosed();
            StableLazySelectionStillWorks();
        }

        private static void RemovalAfterYieldFailsClosed()
        {
            var project = BuildProject(out var first);
            Throws<InvalidOperationException>(() =>
                ProjectQuantityReportBuilder.Group(project, RemoveAfterFirstYield(project, first)));
        }

        private static void ReplacementAfterYieldFailsClosed()
        {
            var project = BuildProject(out var first);
            Throws<InvalidOperationException>(() =>
                ProjectQuantityReportBuilder.Detail(project, ReplaceAfterFirstYield(project, first)));
        }

        private static void StableLazySelectionStillWorks()
        {
            var project = BuildProject(out _);
            var grouped = ProjectQuantityReportBuilder.Group(project, StableSelection());
            if (grouped.Count != 1 || grouped[0].Count != 2 || grouped[0].ElementIds.Count != 2)
                throw new InvalidOperationException("Stable lazy quantity-report Group selection changed unexpectedly.");

            var detailed = ProjectQuantityReportBuilder.Detail(project, StableSelection());
            if (detailed.Count != 2 ||
                !detailed.SelectMany(x => x.ElementIds).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(new[] { "E1", "E2" }))
                throw new InvalidOperationException("Stable lazy quantity-report Detail selection changed unexpectedly.");
        }

        private static IEnumerable<string> RemoveAfterFirstYield(ProjectState project, ProjectElement first)
        {
            yield return first.Id;
            if (!project.Elements.Remove(first))
                throw new InvalidOperationException("Freshness smoke could not remove the already-yielded element.");
            yield return "E2";
        }

        private static IEnumerable<string> ReplaceAfterFirstYield(ProjectState project, ProjectElement first)
        {
            yield return first.Id;
            var index = project.Elements.IndexOf(first);
            if (index < 0)
                throw new InvalidOperationException("Freshness smoke could not locate the already-yielded element.");
            project.Elements.RemoveAt(index);
            var replacement = new ProjectElement("E1", ElementCategory.Beam, "F", string.Empty, string.Empty);
            replacement.SetQuantity("LengthM", 99d);
            project.Elements.Insert(index, replacement);
            yield return "E2";
        }

        private static IEnumerable<string> StableSelection()
        {
            yield return "E1";
            yield return "E2";
        }

        private static ProjectState BuildProject(out ProjectElement first)
        {
            var project = new ProjectState("P-REPORT-FRESHNESS", "Report freshness");
            project.Families.Add(new ProjectFamily("F", "Beam", ElementCategory.Beam));

            first = new ProjectElement("E1", ElementCategory.Beam, "F", string.Empty, string.Empty);
            first.SetQuantity("LengthM", 2d);
            project.Elements.Add(first);

            var second = new ProjectElement("E2", ElementCategory.Beam, "F", string.Empty, string.Empty);
            second.SetQuantity("LengthM", 3d);
            project.Elements.Add(second);
            return project;
        }

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
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
