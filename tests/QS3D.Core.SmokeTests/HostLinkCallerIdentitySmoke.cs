using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkCallerIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedOpeningCallerIdsWithoutMutation();
            RejectsPaddedWallCallerIdsWithoutMutation();
            RejectsPaddedUnlinkCallerIdsWithoutMutation();
            BlankAndNullCallerIdsRemainRejectedWithoutMutation();
            CanonicalCaseInsensitiveCallerIdsRemainSupported();
            PersistedRelationshipValidationRemainsFailClosed();
        }

        private static void RejectsPaddedOpeningCallerIdsWithoutMutation()
        {
            foreach (var openingId in new[] { " O1", "O1 ", " O1 ", "\tO1\t" })
            {
                var project = NewProject(out var wallA, out _, out var opening);
                AssertRejectedWithoutMutation(
                    project,
                    wallA,
                    opening,
                    () => new HostLinkService().LinkOpening(project, openingId, wallA.Id),
                    "padded opening caller " + Escape(openingId));
            }
        }

        private static void RejectsPaddedWallCallerIdsWithoutMutation()
        {
            foreach (var wallId in new[] { " W1", "W1 ", " W1 ", "\tW1\t" })
            {
                var project = NewProject(out var wallA, out _, out var opening);
                AssertRejectedWithoutMutation(
                    project,
                    wallA,
                    opening,
                    () => new HostLinkService().LinkOpening(project, opening.Id, wallId),
                    "padded wall caller " + Escape(wallId));
            }
        }

        private static void RejectsPaddedUnlinkCallerIdsWithoutMutation()
        {
            foreach (var openingId in new[] { " O1", "O1 ", " O1 ", "\tO1\t" })
            {
                var project = NewProject(out var wallA, out _, out var opening);
                opening.Properties["HostWallId"] = wallA.Id;
                opening.DependsOn.Add(wallA.Id);
                opening.MarkClean(ElementDirtyFlags.All);
                wallA.MarkClean(ElementDirtyFlags.All);

                AssertRejectedWithoutMutation(
                    project,
                    wallA,
                    opening,
                    () => new HostLinkService().UnlinkOpening(project, openingId),
                    "padded unlink caller " + Escape(openingId));
            }
        }

        private static void BlankAndNullCallerIdsRemainRejectedWithoutMutation()
        {
            var project = NewProject(out var wallA, out _, out var opening);
            AssertRejectedWithoutMutation(
                project,
                wallA,
                opening,
                () => new HostLinkService().LinkOpening(project, "   ", wallA.Id),
                "blank opening caller");

            project = NewProject(out wallA, out _, out opening);
            AssertRejectedWithoutMutation(
                project,
                wallA,
                opening,
                () => new HostLinkService().LinkOpening(project, opening.Id, null!),
                "null wall caller");

            project = NewProject(out wallA, out _, out opening);
            AssertRejectedWithoutMutation(
                project,
                wallA,
                opening,
                () => new HostLinkService().UnlinkOpening(project, null!),
                "null unlink caller");
        }

        private static void CanonicalCaseInsensitiveCallerIdsRemainSupported()
        {
            var project = NewProject(out var wallA, out var wallB, out var opening);
            var service = new HostLinkService();
            var initialVersion = project.ChangeVersion;

            service.LinkOpening(project, "o1", "w1");
            Equal(initialVersion + 1L, project.ChangeVersion, "case-insensitive initial link version");
            Equal("W1", opening.Properties["HostWallId"], "case-insensitive initial host");
            SequenceEqual(new[] { "W1" }, opening.DependsOn, "case-insensitive initial dependency");

            var noOpVersion = project.ChangeVersion;
            var noOpAudits = project.AuditEvents.Count;
            service.LinkOpening(project, "O1", "w1");
            Equal(noOpVersion, project.ChangeVersion, "canonical no-op version");
            Equal(noOpAudits, project.AuditEvents.Count, "canonical no-op audit count");

            service.LinkOpening(project, "o1", "w2");
            Equal(noOpVersion + 1L, project.ChangeVersion, "case-insensitive rehost version");
            Equal("W2", opening.Properties["HostWallId"], "case-insensitive rehost host");
            SequenceEqual(new[] { "W2" }, opening.DependsOn, "case-insensitive rehost dependency");
            True((wallA.Dirty & ElementDirtyFlags.Quantity) != 0, "old host marked quantity-dirty on rehost");
            True((wallB.Dirty & ElementDirtyFlags.Quantity) != 0, "new host marked quantity-dirty on rehost");

            var unlinkVersion = project.ChangeVersion;
            service.UnlinkOpening(project, "o1");
            Equal(unlinkVersion + 1L, project.ChangeVersion, "case-insensitive unlink version");
            True(!opening.Properties.ContainsKey("HostWallId"), "case-insensitive unlink removes host property");
            Equal(0, opening.DependsOn.Count, "case-insensitive unlink removes dependency");
        }

        private static void PersistedRelationshipValidationRemainsFailClosed()
        {
            var project = NewProject(out var wallA, out _, out var opening);
            opening.Properties["HostWallId"] = " W1 ";
            opening.DependsOn.Add("W1");
            opening.MarkClean(ElementDirtyFlags.All);
            wallA.MarkClean(ElementDirtyFlags.All);

            AssertRejectedWithoutMutation(
                project,
                wallA,
                opening,
                () => new HostLinkService().LinkOpening(project, opening.Id, wallA.Id),
                "padded persisted HostWallId");
        }

        private static void AssertRejectedWithoutMutation(
            ProjectState project,
            ProjectElement wall,
            ProjectElement opening,
            Action action,
            string label)
        {
            var beforeVersion = project.ChangeVersion;
            var beforeAudits = project.AuditEvents.Count;
            var beforeOpeningDirty = opening.Dirty;
            var beforeWallDirty = wall.Dirty;
            var beforeProperties = opening.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Key + "=" + x.Value).ToArray();
            var beforeDependencies = opening.DependsOn.ToArray();

            Throws<InvalidOperationException>(action, label);

            Equal(beforeVersion, project.ChangeVersion, label + " project version");
            Equal(beforeAudits, project.AuditEvents.Count, label + " audit count");
            Equal(beforeOpeningDirty, opening.Dirty, label + " opening dirty flags");
            Equal(beforeWallDirty, wall.Dirty, label + " wall dirty flags");
            SequenceEqual(beforeProperties, opening.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Key + "=" + x.Value), label + " properties");
            SequenceEqual(beforeDependencies, opening.DependsOn, label + " dependencies");
        }

        private static ProjectState NewProject(out ProjectElement wallA, out ProjectElement wallB, out ProjectElement opening)
        {
            var project = new ProjectState("HOST-LINK-CALLER", "Host link caller identity");
            wallA = new ProjectElement("W1", ElementCategory.ArchitecturalWall);
            wallB = new ProjectElement("W2", ElementCategory.ArchitecturalWall);
            opening = new ProjectElement("O1", ElementCategory.WallOpening);
            wallA.MarkClean(ElementDirtyFlags.All);
            wallB.MarkClean(ElementDirtyFlags.All);
            opening.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wallA);
            project.Elements.Add(wallB);
            project.Elements.Add(opening);
            return project;
        }

        private static string Escape(string value) => value.Replace("\t", "\\t");

        private static void SequenceEqual<T>(System.Collections.Generic.IEnumerable<T> expected, System.Collections.Generic.IEnumerable<T> actual, string label)
        {
            if (!expected.SequenceEqual(actual))
                throw new Exception("HostLinkCallerIdentitySmoke " + label + ": sequence mismatch.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("HostLinkCallerIdentitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool condition, string label)
        {
            if (!condition) throw new Exception("HostLinkCallerIdentitySmoke " + label + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("HostLinkCallerIdentitySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
