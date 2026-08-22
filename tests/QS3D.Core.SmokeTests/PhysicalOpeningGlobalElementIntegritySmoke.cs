using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PhysicalOpeningGlobalElementIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsUnrelatedDuplicateSemanticIds();
            ResolvesValidTargetsByCanonicalIdentity();
            RejectsHostRemovedDuringLazyTargetEnumeration();
        }

        private static void RejectsUnrelatedDuplicateSemanticIds()
        {
            var project = CreateValidProject(out var host, out var opening);
            project.Elements.Add(new ProjectElement("DUP", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("dup", ElementCategory.Column));

            Throws<InvalidOperationException>(
                () => PhysicalOpeningCutTargetStateCodec.Resolve(project, host, new[] { opening.Id }),
                "unrelated duplicate semantic ids");
        }

        private static void ResolvesValidTargetsByCanonicalIdentity()
        {
            var project = CreateValidProject(out var host, out var opening);
            var resolved = PhysicalOpeningCutTargetStateCodec.Resolve(project, host, new[] { opening.Id });

            Equal(1, resolved.Count, "valid target count");
            if (!ReferenceEquals(opening, resolved[0]))
                throw new Exception("PhysicalOpeningGlobalElementIntegritySmoke valid target did not resolve to the canonical ProjectElement instance.");
        }

        private static void RejectsHostRemovedDuringLazyTargetEnumeration()
        {
            var project = CreateValidProject(out var host, out var opening);
            host.MarkClean(ElementDirtyFlags.All);
            opening.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeHostUpdated = host.UpdatedUtc;
            var beforeOpeningUpdated = opening.UpdatedUtc;
            var beforeHostDirty = host.Dirty;
            var beforeOpeningDirty = opening.Dirty;
            var beforeLinkedHost = opening.Properties["HostWallId"];

            ThrowsContaining<InvalidOperationException>(
                () => PhysicalOpeningCutTargetStateCodec.Resolve(project, host, YieldThenRemoveHost(project, host, opening.Id)),
                "Project element structure changed while physical opening target ids were being enumerated; recompute the target set against the current project state.",
                "host structural freshness");

            Equal(beforeVersion, project.ChangeVersion, "host structural freshness project revision");
            if (project.Elements.Contains(host))
                throw new Exception("PhysicalOpeningGlobalElementIntegritySmoke expected the deliberate external host removal to remain visible.");
            Equal(beforeLinkedHost, opening.Properties["HostWallId"], "host structural freshness HostWallId");
            Equal(beforeHostDirty, host.Dirty, "host structural freshness host dirty state");
            Equal(beforeOpeningDirty, opening.Dirty, "host structural freshness opening dirty state");
            Equal(beforeHostUpdated, host.UpdatedUtc, "host structural freshness host timestamp");
            Equal(beforeOpeningUpdated, opening.UpdatedUtc, "host structural freshness opening timestamp");
        }

        private static IEnumerable<string> YieldThenRemoveHost(ProjectState project, ProjectElement host, string openingId)
        {
            yield return openingId;
            project.Elements.Remove(host);
        }

        private static ProjectState CreateValidProject(out ProjectElement host, out ProjectElement opening)
        {
            var project = new ProjectState("P-PHYSICAL-GLOBAL-ID", "Physical opening global identity smoke");
            host = new ProjectElement("HOST", ElementCategory.ArchitecturalWall);
            opening = new ProjectElement("OPENING", ElementCategory.WallOpening);
            opening.Properties["HostWallId"] = host.Id;
            project.Elements.Add(host);
            project.Elements.Add(opening);
            return project;
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("PhysicalOpeningGlobalElementIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new Exception("PhysicalOpeningGlobalElementIntegritySmoke " + label + ": expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new Exception("PhysicalOpeningGlobalElementIntegritySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }

        private static void ThrowsContaining<TException>(Action action, string expectedText, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("PhysicalOpeningGlobalElementIntegritySmoke " + label + ": expected message containing '" + expectedText + "', actual='" + ex.Message + "'.");
            }
            catch (Exception ex)
            {
                throw new Exception("PhysicalOpeningGlobalElementIntegritySmoke " + label + ": expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new Exception("PhysicalOpeningGlobalElementIntegritySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
