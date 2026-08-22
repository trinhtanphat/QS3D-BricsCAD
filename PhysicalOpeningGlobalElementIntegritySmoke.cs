using System;
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
    }
}
