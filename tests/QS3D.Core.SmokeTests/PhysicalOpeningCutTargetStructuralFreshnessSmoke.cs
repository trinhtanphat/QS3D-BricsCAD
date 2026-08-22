using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PhysicalOpeningCutTargetStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ReplacementDuringTargetEnumerationFailsFreshness();
            StableTargetResolutionKeepsOwnedInstance();
        }

        private static void ReplacementDuringTargetEnumerationFailsFreshness()
        {
            var project = Fixture(out var host, out var opening);
            var beforeVersion = project.ChangeVersion;
            var index = project.Elements.IndexOf(opening);

            IEnumerable<string> Targets()
            {
                var replacement = Opening("D1", host.Id);
                project.Elements[index] = replacement;
                yield return "D1";
            }

            ThrowsStructuralFreshness(() => PhysicalOpeningCutTargetStateCodec.Resolve(project, host, Targets()));
            Equal(beforeVersion, project.ChangeVersion, "direct opening replacement must leave ChangeVersion unchanged");
            if (ReferenceEquals(project.Elements[index], opening))
                throw new InvalidOperationException("PhysicalOpeningCutTargetStructuralFreshnessSmoke replacement fixture did not change opening ownership.");
        }

        private static void StableTargetResolutionKeepsOwnedInstance()
        {
            var project = Fixture(out var host, out var opening);
            var beforeVersion = project.ChangeVersion;
            var resolved = PhysicalOpeningCutTargetStateCodec.Resolve(project, host, new[] { "D1" });

            Equal(beforeVersion, project.ChangeVersion, "stable target resolution must remain read-only");
            Equal(1, resolved.Count, "stable target count");
            if (!ReferenceEquals(resolved[0], opening))
                throw new InvalidOperationException("Stable physical opening resolution must return the exact project-owned opening instance.");
        }

        private static ProjectState Fixture(out ProjectElement host, out ProjectElement opening)
        {
            var project = new ProjectState("P-PHYSICAL-OPENING-STRUCTURAL", "Physical Opening structural freshness");
            host = new ProjectElement("W1", ElementCategory.StructuralWall);
            opening = Opening("D1", host.Id);
            project.Elements.Add(host);
            project.Elements.Add(opening);
            return project;
        }

        private static ProjectElement Opening(string id, string hostId)
        {
            var opening = new ProjectElement(id, ElementCategory.Door);
            opening.Properties["HostWallId"] = hostId;
            return opening;
        }

        private static void ThrowsStructuralFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("element structure changed", StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Unexpected physical opening structural-freshness error.", ex);
            }

            throw new InvalidOperationException("Expected physical opening structural-freshness rejection.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
