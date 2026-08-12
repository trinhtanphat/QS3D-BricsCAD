using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantityReportSelectionCanonicalitySmoke
    {
        public static void Run()
        {
            UniqueSelectionStillWorks();
            DuplicateSelectionFailsClosed();
            CaseOnlyDuplicateFailsClosed();
            LazyDuplicateStopsBeforeFurtherEnumeration();
            BlankAndUnknownSelectionStillFailClosed();
        }

        private static void UniqueSelectionStillWorks()
        {
            var project = BuildProject();
            var grouped = ProjectQuantityReportBuilder.Group(project, new[] { "E1" });
            if (grouped.Count != 1 || grouped[0].ElementIds.Count != 1 || !string.Equals(grouped[0].ElementIds[0], "E1", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical project quantity selection no longer returns the requested element.");

            var detailed = ProjectQuantityReportBuilder.Detail(project, new[] { "E2" });
            if (detailed.Count != 1 || !string.Equals(detailed[0].ElementIds.Single(), "E2", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical project quantity detail selection no longer returns the requested element.");
        }

        private static void DuplicateSelectionFailsClosed()
        {
            var project = BuildProject();
            AssertThrows<ArgumentException>(
                () => ProjectQuantityReportBuilder.Group(project, new[] { "E1", "E1" }),
                "duplicate quantity report selection must fail closed");
        }

        private static void CaseOnlyDuplicateFailsClosed()
        {
            var project = BuildProject();
            AssertThrows<ArgumentException>(
                () => ProjectQuantityReportBuilder.Detail(project, new[] { "E1", " e1 " }),
                "case-only duplicate quantity report selection must fail closed");
        }

        private static void LazyDuplicateStopsBeforeFurtherEnumeration()
        {
            var project = BuildProject();
            try
            {
                ProjectQuantityReportBuilder.Group(project, DuplicateThenBomb());
                throw new InvalidOperationException("Lazy duplicate quantity selection was accepted unexpectedly.");
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf("unique", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Lazy duplicate quantity selection failed with the wrong canonicality error.", ex);
            }
            catch (InvalidOperationException ex) when (ex.Message == "ENUMERATED_PAST_DUPLICATE")
            {
                throw new InvalidOperationException("Project quantity selection continued enumerating after a duplicate identity instead of failing closed.", ex);
            }
        }

        private static void BlankAndUnknownSelectionStillFailClosed()
        {
            var project = BuildProject();
            AssertThrows<ArgumentException>(
                () => ProjectQuantityReportBuilder.Group(project, new[] { "E1", " " }),
                "blank quantity report selection must remain rejected");
            AssertThrows<KeyNotFoundException>(
                () => ProjectQuantityReportBuilder.Detail(project, new[] { "UNKNOWN" }),
                "unknown quantity report selection must remain rejected");
        }

        private static IEnumerable<string> DuplicateThenBomb()
        {
            yield return "E1";
            yield return "e1";
            throw new InvalidOperationException("ENUMERATED_PAST_DUPLICATE");
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-SELECTION", "Selection canonicality");
            project.Families.Add(new ProjectFamily("F", "Beam", ElementCategory.Beam));

            var first = new ProjectElement("E1", ElementCategory.Beam, "F", string.Empty, string.Empty);
            first.SetQuantity("LengthM", 2d);
            project.Elements.Add(first);

            var second = new ProjectElement("E2", ElementCategory.Beam, "F", string.Empty, string.Empty);
            second.SetQuantity("LengthM", 3d);
            project.Elements.Add(second);
            return project;
        }

        private static void AssertThrows<T>(Action action, string label) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException(label + ".");
        }
    }

    internal static class ProjectQuantityReportSelectionCanonicalitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectQuantityReportSelectionCanonicalitySmoke.Run();
        }
    }
}
