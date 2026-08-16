using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyHealthBlankTargetSmoke
    {
        public static void Run()
        {
            VerifyBlankAndControlCharacterClassification();
            VerifyControlCharacterFamiliesFailClosed();
            VerifyMixedCanonicalAndMalformedDependencies();
        }

        private static void VerifyBlankAndControlCharacterClassification()
        {
            var project = new ProjectState("P1", "Dependency blank target");
            var target = Element("TARGET");
            var source = Element("SOURCE");
            source.DependsOn.Add(null!);
            source.DependsOn.Add(string.Empty);
            source.DependsOn.Add("   ");
            source.DependsOn.Add(" target ");
            source.DependsOn.Add("TAR\nGET");
            source.DependsOn.Add("TARGET\t");
            source.DependsOn.Add("TARGET\0BROKEN");

            var validSource = Element("VALID-SOURCE");
            validSource.DependsOn.Add("TARGET");

            project.Elements.Add(source);
            project.Elements.Add(target);
            project.Elements.Add(validSource);

            var issues = new DependencyHealthService().Inspect(project);
            var blank = ForSource(issues, "SOURCE", "DEPENDENCY_TARGET_BLANK");
            if (blank.Count != 1)
                throw new Exception("Blank/null/whitespace dependency tokens on one source element must produce exactly one health issue.");
            RequireError(blank[0], "Blank dependency health issue");

            var controlCharacter = ForSource(issues, "SOURCE", "DEPENDENCY_TARGET_CONTROL_CHARACTER");
            if (controlCharacter.Count != 1)
                throw new Exception("Multiple malformed control-character dependency tokens on one source element must produce exactly one health issue.");
            RequireError(controlCharacter[0], "Control-character dependency health issue");
            RequireSafeDiagnostic(controlCharacter[0]);

            if (ForSource(issues, "SOURCE", "DEPENDENCY_TARGET_MISSING").Count != 0)
                throw new Exception("Malformed control-character dependencies must be rejected before missing-target classification.");
            if (ForSource(issues, "VALID-SOURCE", "DEPENDENCY_TARGET_BLANK").Count != 0 ||
                ForSource(issues, "VALID-SOURCE", "DEPENDENCY_TARGET_CONTROL_CHARACTER").Count != 0)
                throw new Exception("A source containing only canonical dependencies must not receive malformed-target issues.");
        }

        private static void VerifyControlCharacterFamiliesFailClosed()
        {
            var project = new ProjectState("P2", "Dependency control families");
            project.Elements.Add(Element("TARGET"));

            var controls = new[]
            {
                '\u0000', // C0 NUL
                '\u0001', // C0 SOH
                '\u0009', // TAB
                '\u000A', // LF
                '\u001F', // C0 unit separator
                '\u007F', // DEL
                '\u0085', // C1 NEL
                '\u009F'  // C1 application program command
            };

            for (var index = 0; index < controls.Length; index++)
            {
                var sourceId = "CONTROL-" + index;
                var source = Element(sourceId);
                source.DependsOn.Add("TARGET" + controls[index] + "BROKEN");
                source.DependsOn.Add("TARGET" + controls[index] + "SECOND");
                project.Elements.Add(source);
            }

            var issues = new DependencyHealthService().Inspect(project);
            for (var index = 0; index < controls.Length; index++)
            {
                var sourceId = "CONTROL-" + index;
                var malformed = ForSource(issues, sourceId, "DEPENDENCY_TARGET_CONTROL_CHARACTER");
                if (malformed.Count != 1)
                    throw new Exception("Each source containing one or more control-character dependency IDs must receive exactly one deterministic control-character issue.");
                RequireError(malformed[0], "Control-character family health issue");
                RequireSafeDiagnostic(malformed[0]);

                if (ForSource(issues, sourceId, "DEPENDENCY_TARGET_MISSING").Count != 0 ||
                    ForSource(issues, sourceId, "DEPENDENCY_TARGET_NON_CANONICAL").Count != 0 ||
                    ForSource(issues, sourceId, "DEPENDENCY_TARGET_DUPLICATE").Count != 0)
                    throw new Exception("Control-character dependency IDs must stop before canonical, duplicate, or graph-target classification.");
            }
        }

        private static void VerifyMixedCanonicalAndMalformedDependencies()
        {
            var project = new ProjectState("P3", "Dependency mixed malformed and canonical");
            var targetA = Element("TARGET-A");
            var targetB = Element("TARGET-B");
            var mixed = Element("MIXED");
            mixed.DependsOn.Add("TARGET-A");
            mixed.DependsOn.Add("TARGET-B\u0000INJECTED");
            mixed.DependsOn.Add("TARGET-A");

            var canonical = Element("CANONICAL");
            canonical.DependsOn.Add("TARGET-B");

            project.Elements.Add(mixed);
            project.Elements.Add(canonical);
            project.Elements.Add(targetA);
            project.Elements.Add(targetB);

            var issues = new DependencyHealthService().Inspect(project);
            var malformed = ForSource(issues, "MIXED", "DEPENDENCY_TARGET_CONTROL_CHARACTER");
            if (malformed.Count != 1)
                throw new Exception("Mixed canonical/malformed dependencies must retain one source-owned control-character issue.");
            RequireSafeDiagnostic(malformed[0]);

            var duplicate = ForSource(issues, "MIXED", "DEPENDENCY_TARGET_DUPLICATE");
            if (duplicate.Count != 1)
                throw new Exception("Rejecting a malformed dependency must not suppress independent duplicate-canonical dependency diagnostics.");
            if (ForSource(issues, "MIXED", "DEPENDENCY_TARGET_MISSING").Count != 0)
                throw new Exception("Malformed dependency data must never leak into missing-target graph classification.");

            if (issues.Any(x => string.Equals(x.ElementId, "CANONICAL", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Canonical dependency sources must remain healthy while malformed siblings fail closed.");
        }

        private static List<ModelHealthIssue> ForSource(
            IEnumerable<ModelHealthIssue> issues,
            string sourceId,
            string code)
        {
            return issues.Where(x =>
                    string.Equals(x.ElementId, sourceId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static void RequireError(ModelHealthIssue issue, string context)
        {
            if (issue.Severity != HealthSeverity.Error)
                throw new Exception(context + " must block regeneration/release as an Error.");
        }

        private static void RequireSafeDiagnostic(ModelHealthIssue issue)
        {
            if (issue.Message.Any(char.IsControl))
                throw new Exception("Control-character dependency diagnostics must not echo malformed control characters into the health message.");
            if (issue.Message.Contains("TARGET", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Control-character dependency diagnostics must use static safe text rather than reflecting malformed dependency identifiers.");
        }

        private static ProjectElement Element(string id)
        {
            return new ProjectElement(id, ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
        }
    }
}
