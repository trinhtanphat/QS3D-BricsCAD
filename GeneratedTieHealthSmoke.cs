using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedTieHealthSmoke
    {
        public static void Run()
        {
            MissingTieSolidIsReported();
            OwnershipConflictIsReported();
            CountAndSourceConflictsAreReported();
        }

        private static void MissingTieSolidIsReported()
        {
            var project = new ProjectState("P", "P");
            var column = Element("C1", "AA;BB", "2");
            project.Elements.Add(column);
            var issues = new GeneratedTieRebarHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            True(issues.Any(x => x.Code == "TIE_REBAR_GENERATED_SOLID_MISSING"));
        }

        private static void OwnershipConflictIsReported()
        {
            var project = new ProjectState("P", "P");
            project.Elements.Add(Element("C1", "AA", "1"));
            project.Elements.Add(Element("C2", "AA", "1"));
            var issues = new GeneratedTieRebarHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" });
            True(issues.Any(x => x.Code == "TIE_REBAR_GENERATED_OWNERSHIP_CONFLICT"));
        }

        private static void CountAndSourceConflictsAreReported()
        {
            var project = new ProjectState("P", "P");
            var column = Element("C1", "AA;BB", "3");
            column.SourceHandles.Add("AA");
            project.Elements.Add(column);
            var issues = new GeneratedTieRebarHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA", "BB" });
            True(issues.Any(x => x.Code == "TIE_REBAR_GENERATED_COUNT_MISMATCH"));
            True(issues.Any(x => x.Code == "TIE_REBAR_GENERATED_HANDLE_IN_SOURCE"));
        }

        private static ProjectElement Element(string id, string handles, string count)
        {
            var element = new ProjectElement(id, ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            element.Properties["GeneratedTieRebarHandles"] = handles;
            element.Properties["GeneratedTieRebarCount"] = count;
            element.Properties["GeneratedTieRebarDiameterMm"] = "8";
            element.Properties["GeneratedTieRebarActualSpacingM"] = "0.15";
            return element;
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }
    }
}
