using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkAutoProvenanceSmoke
    {
        private static readonly string[] AutoHostKeys =
        {
            "AutoHostMatched",
            "AutoHostGapM",
            "AutoHostElevDeltaM",
            "AutoHostCandidateCount"
        };

        public static void Run()
        {
            ManualRehostClearsAutoHostProvenance();
            UnlinkClearsAutoHostProvenance();
            MetadataOnlyUnlinkClearsAndTouches();
            SameHostPreservesCurrentAutoHostProvenance();
            RejectedRehostPreservesAutoHostProvenance();
        }

        private static void ManualRehostClearsAutoHostProvenance()
        {
            var setup = CreateLinked("rehost");
            var nextHost = AddWall(setup.Project, "W2");
            var beforeVersion = setup.Project.ChangeVersion;

            new HostLinkService().LinkOpening(setup.Project, setup.Opening.Id, nextHost.Id);

            Equal(nextHost.Id, setup.Opening.Properties["HostWallId"], "Manual re-host did not update HostWallId.");
            if (setup.Opening.DependsOn.Any(x => string.Equals(x, setup.Host.Id, StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Manual re-host left the previous host dependency behind.");
            if (!setup.Opening.DependsOn.Any(x => string.Equals(x, nextHost.Id, StringComparison.Ordinal)))
                throw new Exception("Manual re-host did not add the new canonical host dependency.");
            AssertAutoHostCleared(setup.Opening, "Manual re-host");
            if (setup.Project.ChangeVersion <= beforeVersion)
                throw new Exception("Manual re-host did not advance ChangeVersion.");
        }

        private static void UnlinkClearsAutoHostProvenance()
        {
            var setup = CreateLinked("unlink");
            var beforeVersion = setup.Project.ChangeVersion;

            new HostLinkService().UnlinkOpening(setup.Project, setup.Opening.Id);

            if (setup.Opening.Properties.ContainsKey("HostWallId"))
                throw new Exception("Unlink left HostWallId behind.");
            AssertAutoHostCleared(setup.Opening, "Unlink");
            if (setup.Project.ChangeVersion <= beforeVersion)
                throw new Exception("Unlink did not advance ChangeVersion.");
        }

        private static void MetadataOnlyUnlinkClearsAndTouches()
        {
            var project = new ProjectState("host-meta-only", "Host metadata only");
            var opening = AddOpening(project, "O1");
            StampAutoHost(opening);
            var beforeVersion = project.ChangeVersion;

            new HostLinkService().UnlinkOpening(project, opening.Id);

            AssertAutoHostCleared(opening, "Metadata-only unlink");
            if (project.ChangeVersion <= beforeVersion)
                throw new Exception("Metadata-only AutoHost cleanup did not advance ChangeVersion.");
        }

        private static void SameHostPreservesCurrentAutoHostProvenance()
        {
            var setup = CreateLinked("same-host");
            var beforeVersion = setup.Project.ChangeVersion;
            var before = AutoHostKeys.ToDictionary(x => x, x => setup.Opening.Properties[x], StringComparer.Ordinal);

            new HostLinkService().LinkOpening(setup.Project, setup.Opening.Id, setup.Host.Id);

            foreach (var key in AutoHostKeys)
                Equal(before[key], setup.Opening.Properties[key], "Same-host canonical link cleared current AutoHost provenance: " + key);
            if (setup.Project.ChangeVersion != beforeVersion)
                throw new Exception("Same-host canonical link unexpectedly touched project state.");
        }

        private static void RejectedRehostPreservesAutoHostProvenance()
        {
            var setup = CreateLinked("guarded-rehost");
            var nextHost = AddWall(setup.Project, "W2");
            setup.Host.Properties["PhysicalOpeningCutSolidHandle"] = "ABCD";
            var beforeVersion = setup.Project.ChangeVersion;
            var before = AutoHostKeys.ToDictionary(x => x, x => setup.Opening.Properties[x], StringComparer.Ordinal);

            Throws<InvalidOperationException>(() => new HostLinkService().LinkOpening(setup.Project, setup.Opening.Id, nextHost.Id));

            Equal(setup.Host.Id, setup.Opening.Properties["HostWallId"], "Rejected re-host changed HostWallId.");
            foreach (var key in AutoHostKeys)
                Equal(before[key], setup.Opening.Properties[key], "Rejected re-host cleared AutoHost provenance before physical-cut guard: " + key);
            if (setup.Project.ChangeVersion != beforeVersion)
                throw new Exception("Rejected re-host touched project state.");
        }

        private static Setup CreateLinked(string id)
        {
            var project = new ProjectState("host-link-" + id, "Host link " + id);
            var host = AddWall(project, "W1");
            var opening = AddOpening(project, "O1");
            opening.Properties["HostWallId"] = host.Id;
            opening.DependsOn.Add(host.Id);
            StampAutoHost(opening);
            return new Setup(project, host, opening);
        }

        private static ProjectElement AddWall(ProjectState project, string id)
        {
            var wall = new ProjectElement(id, ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(wall);
            return wall;
        }

        private static ProjectElement AddOpening(ProjectState project, string id)
        {
            var opening = new ProjectElement(id, ElementCategory.WallOpening, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(opening);
            return opening;
        }

        private static void StampAutoHost(ProjectElement opening)
        {
            opening.Properties["AutoHostMatched"] = "true";
            opening.Properties["AutoHostGapM"] = "0.01";
            opening.Properties["AutoHostElevDeltaM"] = "0.02";
            opening.Properties["AutoHostCandidateCount"] = "2";
        }

        private static void AssertAutoHostCleared(ProjectElement opening, string operation)
        {
            foreach (var key in AutoHostKeys)
                if (opening.Properties.ContainsKey(key))
                    throw new Exception(operation + " left stale AutoHost provenance: " + key);
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(message + " Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private sealed class Setup
        {
            public Setup(ProjectState project, ProjectElement host, ProjectElement opening)
            {
                Project = project;
                Host = host;
                Opening = opening;
            }

            public ProjectState Project { get; }
            public ProjectElement Host { get; }
            public ProjectElement Opening { get; }
        }
    }
}
