using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningScheduleHostIdCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalHostIdIsReported();
            MissingAndEmptyHostIdsRemainUnhosted();
            PaddedHostIdFailsClosed();
            WhitespaceOnlyHostIdFailsClosed();
            OrphanHostIdFailsClosed();
            NonWallHostIdFailsClosed();
        }

        private static void CanonicalHostIdIsReported()
        {
            var project = Project("canonical", "W1", includeHostProperty: true, addHost: true);
            var rows = DoorOpeningScheduleBuilder.Build(project);
            if (rows.Count != 1 || rows[0].HostCount != 1 || rows[0].HostIds.Count != 1 ||
                !string.Equals(rows[0].HostIds[0], "W1", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical HostWallId was not preserved by the door/opening schedule.");
        }

        private static void MissingAndEmptyHostIdsRemainUnhosted()
        {
            var missing = DoorOpeningScheduleBuilder.Build(Project("missing", string.Empty, includeHostProperty: false));
            var empty = DoorOpeningScheduleBuilder.Build(Project("empty", string.Empty, includeHostProperty: true));
            if (missing.Count != 1 || missing[0].HostCount != 0 || missing[0].HostIds.Count != 0)
                throw new InvalidOperationException("Missing HostWallId must remain an unhosted schedule row.");
            if (empty.Count != 1 || empty[0].HostCount != 0 || empty[0].HostIds.Count != 0)
                throw new InvalidOperationException("Empty HostWallId must remain an unhosted schedule row.");
        }

        private static void PaddedHostIdFailsClosed()
        {
            Throws<InvalidOperationException>(() =>
                DoorOpeningScheduleBuilder.Build(Project("padded", " W1 ", includeHostProperty: true, addHost: true)));
        }

        private static void WhitespaceOnlyHostIdFailsClosed()
        {
            Throws<InvalidOperationException>(() =>
                DoorOpeningScheduleBuilder.Build(Project("whitespace", "   ", includeHostProperty: true, addHost: true)));
        }

        private static void OrphanHostIdFailsClosed()
        {
            Throws<InvalidOperationException>(() =>
                DoorOpeningScheduleBuilder.Build(Project("orphan", "W-MISSING", includeHostProperty: true)));
        }

        private static void NonWallHostIdFailsClosed()
        {
            Throws<InvalidOperationException>(() =>
                DoorOpeningScheduleBuilder.Build(Project("non-wall", "W1", includeHostProperty: true, addHost: true, hostCategory: ElementCategory.Beam)));
        }

        private static ProjectState Project(
            string suffix,
            string hostId,
            bool includeHostProperty,
            bool addHost = false,
            ElementCategory hostCategory = ElementCategory.ArchitecturalWall)
        {
            var project = new ProjectState("P-DOOR-HOST-" + suffix, "Door host canonicality smoke");
            if (addHost) project.Elements.Add(new ProjectElement("W1", hostCategory));
            var opening = new ProjectElement("D-" + suffix, ElementCategory.Door);
            if (includeHostProperty) opening.Properties["HostWallId"] = hostId;
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
