using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkPhysicalCutSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            ExactTargetBlocksRehostWithoutMutation();
            ExactTargetBlocksUnlinkWithoutMutation();
            VerifiedNonTargetCanRehost();
            LegacyCutWithoutTargetStateFailsClosed();
            CorruptTargetStateFailsClosed();
            CodecRoundTripsDeterministically();
            CodecRejectsPaddedTargetsWithoutMutation();
            CodecRejectsDuplicateTargetsWithoutMutation();
        }

        private static void ExactTargetBlocksRehostWithoutMutation()
        {
            var project = ProjectWithOpening(out var oldHost, out var newHost, out var opening);
            SeedPhysicalCut(oldHost, opening.Id);

            Throws<InvalidOperationException>(() => new HostLinkService().LinkOpening(project, opening.Id, newHost.Id));
            Equal(oldHost.Id, opening.Properties["HostWallId"]);
            True(opening.DependsOn.Any(x => string.Equals(x, oldHost.Id, StringComparison.OrdinalIgnoreCase)));
            False(opening.DependsOn.Any(x => string.Equals(x, newHost.Id, StringComparison.OrdinalIgnoreCase)));
        }

        private static void ExactTargetBlocksUnlinkWithoutMutation()
        {
            var project = ProjectWithOpening(out var oldHost, out _, out var opening);
            SeedPhysicalCut(oldHost, opening.Id);

            Throws<InvalidOperationException>(() => new HostLinkService().UnlinkOpening(project, opening.Id));
            Equal(oldHost.Id, opening.Properties["HostWallId"]);
            True(opening.DependsOn.Any(x => string.Equals(x, oldHost.Id, StringComparison.OrdinalIgnoreCase)));
        }

        private static void VerifiedNonTargetCanRehost()
        {
            var project = ProjectWithOpening(out var oldHost, out var newHost, out var opening);
            var actuallyCut = new ProjectElement("O-CUT", ElementCategory.WallOpening, string.Empty, string.Empty, string.Empty);
            actuallyCut.Properties["HostWallId"] = oldHost.Id;
            actuallyCut.DependsOn.Add(oldHost.Id);
            project.Elements.Add(actuallyCut);
            SeedPhysicalCut(oldHost, actuallyCut.Id);

            new HostLinkService().LinkOpening(project, opening.Id, newHost.Id);
            Equal(newHost.Id, opening.Properties["HostWallId"]);
            False(opening.DependsOn.Any(x => string.Equals(x, oldHost.Id, StringComparison.OrdinalIgnoreCase)));
            True(opening.DependsOn.Any(x => string.Equals(x, newHost.Id, StringComparison.OrdinalIgnoreCase)));
        }

        private static void LegacyCutWithoutTargetStateFailsClosed()
        {
            var project = ProjectWithOpening(out var oldHost, out var newHost, out var opening);
            oldHost.Properties["PhysicalOpeningCutSolidHandle"] = "AA";
            oldHost.Properties["PhysicalOpeningCutFingerprint"] = "F";

            Throws<InvalidOperationException>(() => new HostLinkService().LinkOpening(project, opening.Id, newHost.Id));
            Equal(oldHost.Id, opening.Properties["HostWallId"]);
        }

        private static void CorruptTargetStateFailsClosed()
        {
            var project = ProjectWithOpening(out var oldHost, out var newHost, out var opening);
            oldHost.Properties["PhysicalOpeningCutSolidHandle"] = "AA";
            oldHost.Properties["PhysicalOpeningCutFingerprint"] = "F";
            oldHost.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey] = "%%%not-base64%%%";

            Throws<InvalidOperationException>(() => new HostLinkService().LinkOpening(project, opening.Id, newHost.Id));
            Equal(oldHost.Id, opening.Properties["HostWallId"]);
        }

        private static void CodecRoundTripsDeterministically()
        {
            var host = new ProjectElement("W", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            PhysicalOpeningCutTargetStateCodec.Write(host, new[] { "B", "a", "C" });
            True(PhysicalOpeningCutTargetStateCodec.TryRead(host, out var ids));
            Equal(3, ids.Count);
            Equal("a", ids[0]);
            Equal("B", ids[1]);
            Equal("C", ids[2]);
        }

        private static void CodecRejectsPaddedTargetsWithoutMutation()
        {
            var host = new ProjectElement("W", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey] = "sentinel";

            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.Normalize(new[] { "O1", " o2 " }));
            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.Write(host, new[] { "O1", " o2 " }));
            Equal("sentinel", host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey]);
        }

        private static void CodecRejectsDuplicateTargetsWithoutMutation()
        {
            var host = new ProjectElement("W", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey] = "sentinel";

            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.Normalize(new[] { "O1", "o1" }));
            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.Write(host, new[] { "O1", "o1" }));
            Equal("sentinel", host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey]);
        }

        private static ProjectState ProjectWithOpening(out ProjectElement oldHost, out ProjectElement newHost, out ProjectElement opening)
        {
            var project = new ProjectState("HOST-CUT", "Physical opening host guard");
            oldHost = new ProjectElement("W-OLD", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            newHost = new ProjectElement("W-NEW", ElementCategory.StructuralWall, string.Empty, string.Empty, string.Empty);
            opening = new ProjectElement("O1", ElementCategory.Door, string.Empty, string.Empty, string.Empty);
            opening.Properties["HostWallId"] = oldHost.Id;
            opening.DependsOn.Add(oldHost.Id);
            project.Elements.Add(oldHost);
            project.Elements.Add(newHost);
            project.Elements.Add(opening);
            return project;
        }

        private static void SeedPhysicalCut(ProjectElement host, params string[] openingIds)
        {
            host.Properties["PhysicalOpeningCutSolidHandle"] = "AA";
            host.Properties["PhysicalOpeningCutFingerprint"] = "F";
            PhysicalOpeningCutTargetStateCodec.Write(host, openingIds);
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected false.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
