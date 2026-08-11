using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyHealthBlankTargetSmoke
    {
        public static void Run()
        {
            var project = new ProjectState("P1", "Dependency blank target");
            var target = Element("TARGET");
            var source = Element("SOURCE");
            source.DependsOn.Add(null!);
            source.DependsOn.Add(string.Empty);
            source.DependsOn.Add("   ");
            source.DependsOn.Add(" target ");

            var validSource = Element("VALID-SOURCE");
            validSource.DependsOn.Add("TARGET");

            project.Elements.Add(source);
            project.Elements.Add(target);
            project.Elements.Add(validSource);

            var issues = new DependencyHealthService().Inspect(project);
            var blank = issues.Where(x => string.Equals(x.Code, "DEPENDENCY_TARGET_BLANK", StringComparison.OrdinalIgnoreCase)).ToList();
            if (blank.Count != 1)
                throw new Exception("Blank/null/whitespace dependency tokens on one source element must produce exactly one health issue.");
            if (!string.Equals(blank[0].ElementId, "SOURCE", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Blank dependency health issue must identify the source semantic element.");
            if (blank[0].Severity != HealthSeverity.Error)
                throw new Exception("Blank dependency health issue must block regeneration/release as an Error.");
            if (issues.Any(x => string.Equals(x.Code, "DEPENDENCY_TARGET_MISSING", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("A valid normalized dependency must not be misreported as missing while checking blank tokens.");
            if (issues.Any(x => string.Equals(x.Code, "DEPENDENCY_TARGET_BLANK", StringComparison.OrdinalIgnoreCase) && string.Equals(x.ElementId, "VALID-SOURCE", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("A source containing only valid dependencies must not receive a blank dependency issue.");
        }

        private static ProjectElement Element(string id)
        {
            return new ProjectElement(id, ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
        }
    }
}
