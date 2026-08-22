using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ComprehensiveGeneratedLiveHandleIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NumericLiveAliasMatchesCanonicalPersistedHandle();
            CanonicalLiveHandleMatchesPaddedNumericPersistedIdentity();
            TrulyMissingGeneratedHandleStillFailsVisible();
            NumericSourceLiveAliasMatchesCanonicalIdentity();
            TrulyMissingSourceHandleStillFailsVisible();
        }

        private static void NumericLiveAliasMatchesCanonicalPersistedHandle()
        {
            var project = Project("FORWARD");
            project.Elements.Add(Generated("E-1", "A"));
            var issues = new ComprehensiveModelHealthService().Inspect(project, null, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "00A" });
            EnsureAbsent(issues, "GENERATED_SOLID_MISSING", "Numeric-equivalent live generated handle must satisfy canonical persisted handle.");
        }

        private static void CanonicalLiveHandleMatchesPaddedNumericPersistedIdentity()
        {
            var project = Project("REVERSE");
            project.Elements.Add(Generated("E-1", "00A"));
            var issues = new ComprehensiveModelHealthService().Inspect(project, null, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" });
            EnsureAbsent(issues, "GENERATED_SOLID_MISSING", "Canonical live generated handle must satisfy leading-zero persisted numeric identity.");
        }

        private static void TrulyMissingGeneratedHandleStillFailsVisible()
        {
            var project = Project("MISSING");
            var element = Generated("E-1", "A");
            project.Elements.Add(element);
            var issues = new ComprehensiveModelHealthService().Inspect(project, null, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "B" });
            Require(issues, element.Id, "GENERATED_SOLID_MISSING");
        }

        private static void NumericSourceLiveAliasMatchesCanonicalIdentity()
        {
            var project = Project("SOURCE");
            var element = new ProjectElement("E-1", ElementCategory.ArchitecturalWall);
            element.SourceHandles.Add("0A");
            project.Elements.Add(element);
            var issues = new ComprehensiveModelHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" }, null);
            EnsureAbsent(issues, "ORPHAN_HANDLE", "Numeric-equivalent live source handle must satisfy the persisted semantic source identity.");
        }

        private static void TrulyMissingSourceHandleStillFailsVisible()
        {
            var project = Project("SOURCE-MISSING");
            var element = new ProjectElement("E-1", ElementCategory.ArchitecturalWall);
            element.SourceHandles.Add("B");
            project.Elements.Add(element);
            var issues = new ComprehensiveModelHealthService().Inspect(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" }, null);
            Require(issues, element.Id, "ORPHAN_HANDLE");
        }

        private static ProjectState Project(string suffix) =>
            new ProjectState("P-COMPREHENSIVE-LIVE-" + suffix, "Comprehensive generated live identity smoke");

        private static ProjectElement Generated(string id, string handle)
        {
            var element = new ProjectElement(id, ElementCategory.ArchitecturalWall);
            element.Properties["GeneratedSolidHandle"] = handle;
            element.Properties["GeneratedSolidCategory"] = ElementCategory.ArchitecturalWall.ToString();
            element.Properties["GeneratedSolidOwnershipVersion"] = "1";
            element.Properties["GeneratedSolidOwnerProjectId"] = "P-COMPREHENSIVE-LIVE-FORWARD";
            element.Properties["GeneratedSolidOwnerElementId"] = id;
            return element;
        }

        private static void Require(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                throw new InvalidOperationException("Expected health issue was not reported: " + code + ".");
        }

        private static void EnsureAbsent(IReadOnlyList<ModelHealthIssue> issues, string code, string message)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new InvalidOperationException(message);
        }
    }
}
