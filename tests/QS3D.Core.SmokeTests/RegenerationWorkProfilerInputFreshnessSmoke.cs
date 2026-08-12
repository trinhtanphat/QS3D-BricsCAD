using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfilerInputFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            StableLazyTargetProducesCurrentProfile();
            MutatingLazyTargetFailsBeforeProfiling();
            MutatingEmptyTargetFailsBeforeEmptyProfile();
        }

        private static void StableLazyTargetProducesCurrentProfile()
        {
            var project = CreateProject();
            var beforeVersion = project.ChangeVersion;

            var profile = new RegenerationWorkProfiler().ProfileSubset(project, LazyTarget("E1"));

            Require(profile.SourceChangeVersion == beforeVersion,
                "Stable regeneration profile did not retain the source project revision.");
            Require(profile.Scope == RegenerationWorkScope.Subset,
                "Stable regeneration profile changed subset scope.");
            Require(profile.TargetElementIds.Count == 1 && profile.TargetElementIds[0] == "E1",
                "Stable lazy regeneration target was not preserved.");
            Require(profile.PlannedElementCount == 1 && profile.Items[0].ElementId == "E1",
                "Stable lazy regeneration target did not produce the expected work item.");
            Require(project.ChangeVersion == beforeVersion,
                "Read-only regeneration profiling changed ProjectState.ChangeVersion.");
        }

        private static void MutatingLazyTargetFailsBeforeProfiling()
        {
            var project = CreateProject();
            var beforeVersion = project.ChangeVersion;

            ThrowsFreshness(() => new RegenerationWorkProfiler().ProfileSubset(project, TouchThenYield(project, "E1")));

            Require(project.ChangeVersion == beforeVersion + 1L,
                "Mutating target enumerable side effect was unexpectedly rolled back.");
            Require(project.Elements.Count == 1 && project.Elements[0].Id == "E1",
                "Profile freshness rejection mutated semantic elements.");
        }

        private static void MutatingEmptyTargetFailsBeforeEmptyProfile()
        {
            var project = CreateProject();
            var beforeVersion = project.ChangeVersion;

            ThrowsFreshness(() => new RegenerationWorkProfiler().ProfileSubset(project, TouchThenStop(project)));

            Require(project.ChangeVersion == beforeVersion + 1L,
                "Mutating empty target enumerable side effect was unexpectedly rolled back.");
            Require(project.Elements.Count == 1 && project.Elements[0].Id == "E1",
                "Mutating empty target escaped freshness rejection through empty-profile behavior.");
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("PROFILE-FRESH", "Regeneration profile freshness");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.CustomQuantity));
            return project;
        }

        private static IEnumerable<string> LazyTarget(string elementId)
        {
            yield return elementId;
        }

        private static IEnumerable<string> TouchThenYield(ProjectState project, string elementId)
        {
            project.Touch();
            yield return elementId;
        }

        private static IEnumerable<string> TouchThenStop(ProjectState project)
        {
            project.Touch();
            yield break;
        }

        private static void ThrowsFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                const string expected = "Project changed while regeneration profile target ids were being materialized. Re-run the profile against the current semantic state.";
                if (!string.Equals(ex.Message, expected, StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected regeneration profile input freshness error.", ex);
                return;
            }
            throw new InvalidOperationException("Expected regeneration profile input freshness rejection.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
