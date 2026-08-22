using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PhysicalOpeningSemanticTargetFreshnessSmoke
    {
        internal static void Run()
        {
            StableLazyTargetsStillResolve();
            TouchThenYieldFailsClosed();
            TouchThenEmptyFailsClosed();
        }

        private static void StableLazyTargetsStillResolve()
        {
            var project = BuildProject(out var host, out var opening);
            var resolved = PhysicalOpeningCutTargetStateCodec.Resolve(project, host, StableTargets());
            if (resolved.Count != 1 || !ReferenceEquals(resolved[0], opening))
                throw new InvalidOperationException("Stable lazy physical opening target resolution changed unexpectedly.");
        }

        private static void TouchThenYieldFailsClosed()
        {
            var project = BuildProject(out var host, out _);
            Throws<InvalidOperationException>(() =>
                PhysicalOpeningCutTargetStateCodec.Resolve(project, host, TouchThenYield(project)));
        }

        private static void TouchThenEmptyFailsClosed()
        {
            var project = BuildProject(out var host, out _);
            Throws<InvalidOperationException>(() =>
                PhysicalOpeningCutTargetStateCodec.Resolve(project, host, TouchThenEmpty(project)));
        }

        private static IEnumerable<string> StableTargets()
        {
            yield return "O1";
        }

        private static IEnumerable<string> TouchThenYield(ProjectState project)
        {
            project.Touch();
            yield return "O1";
        }

        private static IEnumerable<string> TouchThenEmpty(ProjectState project)
        {
            project.Touch();
            yield break;
        }

        private static ProjectState BuildProject(out ProjectElement host, out ProjectElement opening)
        {
            var project = new ProjectState("P-OPENING-SEMANTIC-FRESHNESS", "Opening semantic freshness");
            host = new ProjectElement("H1", ElementCategory.ArchitecturalWall);
            opening = new ProjectElement("O1", ElementCategory.WallOpening);
            opening.Properties["HostWallId"] = host.Id;
            project.Elements.Add(host);
            project.Elements.Add(opening);
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
