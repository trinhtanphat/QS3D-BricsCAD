using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectActiveContextCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalAssignmentsAndExplicitClearRemainSupported();
            PaddedZoneIdsRejectAtomically();
            PaddedFloorIdsRejectAtomically();
            EmbeddedControlCharactersRejectAtomically();
        }

        private static void CanonicalAssignmentsAndExplicitClearRemainSupported()
        {
            var project = new ProjectState("active-context-canonical", "Active context canonicality");

            project.ActiveZoneId = "ZONE-1";
            Equal("ZONE-1", project.ActiveZoneId);
            var zoneVersion = project.ChangeVersion;
            var zoneUpdatedUtc = project.UpdatedUtc;

            project.ActiveZoneId = "ZONE-1";
            Equal(zoneVersion, project.ChangeVersion);
            Equal(zoneUpdatedUtc, project.UpdatedUtc);

            project.ActiveFloorId = "FLOOR-1";
            Equal("FLOOR-1", project.ActiveFloorId);
            var floorVersion = project.ChangeVersion;
            var floorUpdatedUtc = project.UpdatedUtc;

            project.ActiveFloorId = "FLOOR-1";
            Equal(floorVersion, project.ChangeVersion);
            Equal(floorUpdatedUtc, project.UpdatedUtc);

            project.ActiveZoneId = string.Empty;
            Equal(string.Empty, project.ActiveZoneId);
            project.ActiveFloorId = string.Empty;
            Equal(string.Empty, project.ActiveFloorId);
        }

        private static void PaddedZoneIdsRejectAtomically()
        {
            var project = ProjectWithActiveContexts();
            RejectZoneAtomically(project, " ZONE-1");
            RejectZoneAtomically(project, "ZONE-1 ");
            RejectZoneAtomically(project, " ZONE-1 ");
            RejectZoneAtomically(project, "\tZONE-1");
            RejectZoneAtomically(project, "ZONE-1\r");
            RejectZoneAtomically(project, "ZONE-1\n");
            RejectZoneAtomically(project, "   ");
            RejectZoneAtomically(project, "\t");
        }

        private static void PaddedFloorIdsRejectAtomically()
        {
            var project = ProjectWithActiveContexts();
            RejectFloorAtomically(project, " FLOOR-1");
            RejectFloorAtomically(project, "FLOOR-1 ");
            RejectFloorAtomically(project, " FLOOR-1 ");
            RejectFloorAtomically(project, "\tFLOOR-1");
            RejectFloorAtomically(project, "FLOOR-1\r");
            RejectFloorAtomically(project, "FLOOR-1\n");
            RejectFloorAtomically(project, "   ");
            RejectFloorAtomically(project, "\t");
        }

        private static void EmbeddedControlCharactersRejectAtomically()
        {
            var project = ProjectWithActiveContexts();
            RejectZoneAtomically(project, "ZONE\u0000-1");
            RejectFloorAtomically(project, "FLOOR\u0000-1");
        }

        private static ProjectState ProjectWithActiveContexts()
        {
            var project = new ProjectState("active-context-reject", "Active context rejection");
            project.ActiveZoneId = "ZONE-1";
            project.ActiveFloorId = "FLOOR-1";
            return project;
        }

        private static void RejectZoneAtomically(ProjectState project, string candidate)
        {
            var beforeValue = project.ActiveZoneId;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            try
            {
                project.ActiveZoneId = candidate;
            }
            catch (ArgumentException)
            {
                Equal(beforeValue, project.ActiveZoneId);
                Equal(beforeVersion, project.ChangeVersion);
                Equal(beforeUpdatedUtc, project.UpdatedUtc);
                return;
            }

            throw new Exception("Expected non-canonical ActiveZoneId to fail: " + Escape(candidate));
        }

        private static void RejectFloorAtomically(ProjectState project, string candidate)
        {
            var beforeValue = project.ActiveFloorId;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            try
            {
                project.ActiveFloorId = candidate;
            }
            catch (ArgumentException)
            {
                Equal(beforeValue, project.ActiveFloorId);
                Equal(beforeVersion, project.ChangeVersion);
                Equal(beforeUpdatedUtc, project.UpdatedUtc);
                return;
            }

            throw new Exception("Expected non-canonical ActiveFloorId to fail: " + Escape(candidate));
        }

        private static string Escape(string value)
        {
            return value
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("\0", "\\0");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
