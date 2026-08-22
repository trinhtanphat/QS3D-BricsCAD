using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantityReportSemanticSelectionFreshnessSmoke
    {
        internal static void Run()
        {
            StableLazySelectionStillWorks();
            TouchThenYieldFailsClosed();
            TouchThenEmptyFailsClosed();
        }

        private static void StableLazySelectionStillWorks()
        {
            var project = BuildProject();
            var grouped = ProjectQuantityReportBuilder.Group(project, StableSelection());
            if (grouped.Count != 1 || grouped[0].Count != 2 || grouped[0].ElementIds.Count != 2 || grouped[0].LengthM != 5d)
                throw new InvalidOperationException("Stable lazy quantity-report Group selection changed unexpectedly.");

            var detailed = ProjectQuantityReportBuilder.Detail(project, StableSelection());
            if (detailed.Count != 2 ||
                !detailed.SelectMany(x => x.ElementIds).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(new[] { "E1", "E2" }))
                throw new InvalidOperationException("Stable lazy quantity-report Detail selection changed unexpectedly.");
        }

        private static void TouchThenYieldFailsClosed()
        {
            var project = BuildProject();
            Throws<InvalidOperationException>(() =>
                ProjectQuantityReportBuilder.Group(project, TouchThenYield(project)));
        }

        private static void TouchThenEmptyFailsClosed()
        {
            var project = BuildProject();
            Throws<InvalidOperationException>(() =>
                ProjectQuantityReportBuilder.Detail(project, TouchThenEmpty(project)));
        }

        private static IEnumerable<string> StableSelection()
        {
            yield return "E1";
            yield return "E2";
        }

        private static IEnumerable<string> TouchThenYield(ProjectState project)
        {
            project.Touch();
            yield return "E1";
        }

        private static IEnumerable<string> TouchThenEmpty(ProjectState project)
        {
            project.Touch();
            yield break;
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-REPORT-SEMANTIC-FRESHNESS", "Report semantic freshness");
            project.Families.Add(new ProjectFamily("F", "Beam", ElementCategory.Beam));

            var first = new ProjectElement("E1", ElementCategory.Beam, "F", string.Empty, string.Empty);
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
