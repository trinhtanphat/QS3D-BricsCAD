using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PhysicalOpeningHostReferenceCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-PHYSICAL-HOST", "Physical host reference smoke");
            var host = new ProjectElement("HOST", ElementCategory.ArchitecturalWall);
            var opening = new ProjectElement("OPENING", ElementCategory.WallOpening);
            project.Elements.Add(host);
            project.Elements.Add(opening);

            opening.Properties["HostWallId"] = "host";
            var resolved = PhysicalOpeningCutTargetStateCodec.Resolve(project, host, new[] { opening.Id });
            Equal(1, resolved.Count, "canonical lower-case host relation count");
            if (!ReferenceEquals(opening, resolved[0]))
                throw new Exception("PhysicalOpeningHostReferenceCanonicalitySmoke canonical relation resolved a detached opening.");

            opening.Properties["HostWallId"] = " HOST ";
            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.Resolve(project, host, new[] { opening.Id }), "padded HostWallId");

            opening.Properties["HostWallId"] = "   ";
            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.Resolve(project, host, new[] { opening.Id }), "whitespace-only HostWallId");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("PhysicalOpeningHostReferenceCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("PhysicalOpeningHostReferenceCanonicalitySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
