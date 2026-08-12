using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfilerStructuralFreshnessSmoke
    {
        internal static void Run()
        {
            StableLazySubsetStillProfiles();
            SameIdReplacementFailsClosed();
            RemovalThenEmptyFailsClosed();
        }

        private static void StableLazySubsetStillProfiles()
        {
            var project = BuildProject(out _, out _);
            var profile = new RegenerationWorkProfiler().ProfileSubset(project, StableTargets());
            if (profile.Scope != RegenerationWorkScope.Subset ||
                profile.TargetElementIds.Count != 2 ||
                profile.PlannedElementCount != 2 ||
                profile.ProjectElementCount != 2)
                throw new InvalidOperationException("Stable lazy regeneration work subset profile changed unexpectedly.");
        }

        private static void SameIdReplacementFailsClosed()
        {
            var project = BuildProject(out var first, out _);
            var version = project.ChangeVersion;
            Throws<InvalidOperationException>(() =>
                new RegenerationWorkProfiler().ProfileSubset(project, ReplaceSameIdThenYield(project, first)));
            if (project.ChangeVersion != version)
                throw new InvalidOperationException("Direct structural replacement unexpectedly advanced ProjectState.ChangeVersion.");
        }

        private static void RemovalThenEmptyFailsClosed()
        {
            var project = BuildProject(out var first, out _);
            var version = project.ChangeVersion;
            Throws<InvalidOperationException>(() =>
                new RegenerationWorkProfiler().ProfileSubset(project, RemoveThenEmpty(project, first)));
            if (project.ChangeVersion != version)
                throw new InvalidOperationException("Direct structural removal unexpectedly advanced ProjectState.ChangeVersion.");
        }

        private static IEnumerable<string> StableTargets()
        {
            yield return "E1";
            yield return "E2";
        }

        private static IEnumerable<string> ReplaceSameIdThenYield(ProjectState project, ProjectElement original)
        {
            yield return original.Id;
            var index = project.Elements.IndexOf(original);
            if (index < 0) throw new InvalidOperationException("Expected original element in project.");
            project.Elements[index] = new ProjectElement(original.Id, original.Category);
            yield return "E2";
        }

        private static IEnumerable<string> RemoveThenEmpty(ProjectState project, ProjectElement original)
        {
            if (!project.Elements.Remove(original))
                throw new InvalidOperationException("Expected original element removal to succeed.");
            yield break;
        }

        private static ProjectState BuildProject(out ProjectElement first, out ProjectElement second)
        {
            var project = new ProjectState("P-REGEN-PROFILE-STRUCTURAL", "Regeneration profile structural freshness");
            first = new ProjectElement("E1", ElementCategory.Beam);
            second = new ProjectElement("E2", ElementCategory.Slab);
            project.Elements.Add(first);
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
