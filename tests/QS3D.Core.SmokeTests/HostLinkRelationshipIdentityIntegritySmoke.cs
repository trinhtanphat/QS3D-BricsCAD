using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkRelationshipIdentityIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LinkRejectsMalformedNonMatchingDependencyBeforeMutation();
            LinkRejectsControlBearingDependencyBeforeMutation();
            LinkRejectsMalformedHostGraphDependencyBeforeMutation();
            UnlinkRejectsMalformedPersistedHostIdBeforeMutation();
            CallerIdentityRejectsHostileTextAndPreservesCanonicalLookup();
        }

        private static void LinkRejectsMalformedNonMatchingDependencyBeforeMutation()
        {
            var state = CreateState();
            state.Opening.DependsOn.Add("legacy-\uD800");

            ThrowsInvalid(
                () => state.Service.LinkOpening(state.Project, state.Opening.Id, state.Wall.Id),
                "malformed UTF-16");

            False(state.Opening.Properties.ContainsKey("HostWallId"), "malformed dependency must fail before HostWallId publication");
            Equal(1, state.Opening.DependsOn.Count);
            Equal("legacy-\uD800", state.Opening.DependsOn[0]);
        }

        private static void LinkRejectsControlBearingDependencyBeforeMutation()
        {
            var state = CreateState();
            state.Opening.DependsOn.Add("legacy-\u0001-id");

            ThrowsInvalid(
                () => state.Service.LinkOpening(state.Project, state.Opening.Id, state.Wall.Id),
                "control characters");

            False(state.Opening.Properties.ContainsKey("HostWallId"), "control-bearing dependency must fail before HostWallId publication");
            Equal(1, state.Opening.DependsOn.Count);
        }

        private static void LinkRejectsMalformedHostGraphDependencyBeforeMutation()
        {
            var state = CreateState();
            state.Wall.DependsOn.Add("graph-\uFFFF-id");

            ThrowsInvalid(
                () => state.Service.LinkOpening(state.Project, state.Opening.Id, state.Wall.Id),
                "XML-invalid");

            False(state.Opening.Properties.ContainsKey("HostWallId"), "host graph identity failure must precede semantic mutation");
            Equal(0, state.Opening.DependsOn.Count);
        }

        private static void UnlinkRejectsMalformedPersistedHostIdBeforeMutation()
        {
            var state = CreateState();
            state.Opening.Properties["HostWallId"] = "host-\uDC00-id";
            state.Opening.DependsOn.Add(state.Wall.Id);

            ThrowsInvalid(
                () => state.Service.UnlinkOpening(state.Project, state.Opening.Id),
                "malformed UTF-16");

            Equal("host-\uDC00-id", state.Opening.Properties["HostWallId"]);
            Equal(state.Wall.Id, state.Opening.DependsOn[0]);
        }

        private static void CallerIdentityRejectsHostileTextAndPreservesCanonicalLookup()
        {
            var state = CreateState("Wall-\U0001F680");
            ThrowsInvalid(
                () => state.Service.LinkOpening(state.Project, "Opening-\u0001", state.Wall.Id),
                "control characters");
            ThrowsInvalid(
                () => state.Service.LinkOpening(state.Project, state.Opening.Id, "Wall-\uD800"),
                "malformed UTF-16");
            ThrowsInvalid(
                () => state.Service.LinkOpening(state.Project, " " + state.Opening.Id, state.Wall.Id),
                "non-canonical");

            state.Service.LinkOpening(state.Project, state.Opening.Id.ToLowerInvariant(), state.Wall.Id.ToLowerInvariant());
            Equal(state.Wall.Id, state.Opening.Properties["HostWallId"]);
            Equal(1, state.Opening.DependsOn.Count);
            Equal(state.Wall.Id, state.Opening.DependsOn[0]);
        }

        private static State CreateState(string wallId = "Wall-A")
        {
            var project = new ProjectState("project-host-link-integrity", "Host Link Integrity");
            var opening = new ProjectElement("Opening-A", ElementCategory.WallOpening);
            var wall = new ProjectElement(wallId, ElementCategory.ArchitecturalWall);
            project.Elements.Add(opening);
            project.Elements.Add(wall);
            return new State(project, opening, wall, new HostLinkService());
        }

        private static void ThrowsInvalid(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Expected diagnostic containing '" + expectedMessage + "' but got: " + ex.Message);
                return;
            }
            throw new InvalidOperationException("Expected hostile host-link identity to fail before mutation.");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new InvalidOperationException(label);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private sealed class State
        {
            public State(ProjectState project, ProjectElement opening, ProjectElement wall, HostLinkService service)
            {
                Project = project;
                Opening = opening;
                Wall = wall;
                Service = service;
            }

            public ProjectState Project { get; }
            public ProjectElement Opening { get; }
            public ProjectElement Wall { get; }
            public HostLinkService Service { get; }
        }
    }
}
