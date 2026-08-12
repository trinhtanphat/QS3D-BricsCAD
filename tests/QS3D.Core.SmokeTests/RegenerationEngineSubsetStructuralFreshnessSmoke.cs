using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationEngineSubsetStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ReplacementDuringTargetEnumerationFailsFreshness();
            StableSubsetStillRegenerates();
        }

        private static void ReplacementDuringTargetEnumerationFailsFreshness()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var original = project.FindElement("B1")!;
            var index = project.Elements.IndexOf(original);

            IEnumerable<string> Targets()
            {
                var replacement = Beam("B1", "6", "0.3", "0.5");
                project.Elements[index] = replacement;
                yield return "B1";
            }

            ThrowsStructuralFreshness(() => Engine().RegenerateDirtySubset(project, Targets()));
            Equal(beforeVersion, project.ChangeVersion, "direct replacement must leave ChangeVersion unchanged");
            var current = project.FindElement("B1")!;
            if (ReferenceEquals(current, original))
                throw new InvalidOperationException("RegenerationEngineSubsetStructuralFreshnessSmoke replacement fixture did not change target ownership.");
            True(!current.Quantities.ContainsKey("NetVolumeM3"), "replacement target must not be regenerated after freshness rejection");
            True(!original.Quantities.ContainsKey("NetVolumeM3"), "detached original target must not be regenerated after freshness rejection");
        }

        private static void StableSubsetStillRegenerates()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var count = Engine().RegenerateDirtySubset(project, new[] { "B1" });

            True(count >= 1, "stable subset must regenerate at least one element");
            True(project.ChangeVersion > beforeVersion, "successful targeted regeneration must advance project revision");
            Near(0.9d, project.FindElement("B1")!.Quantities["NetVolumeM3"], "stable B1 net volume");
            True(!project.FindElement("B2")!.Quantities.ContainsKey("NetVolumeM3"), "stable subset must not regenerate B2");
        }

        private static RegenerationEngine Engine() =>
            new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-REGEN-ENGINE-STRUCTURAL", "Regeneration Engine structural freshness");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            var family = new ProjectFamily("FAM", "Beam", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            project.Families.Add(family);
            project.Elements.Add(Beam("B1", "6", "0.3", "0.5"));
            project.Elements.Add(Beam("B2", "4", "0.3", "0.4"));
            return project;
        }

        private static ProjectElement Beam(string id, string length, string width, string height)
        {
            var beam = new ProjectElement(id, ElementCategory.Beam, "FAM", "F", "Z");
            beam.Properties["LengthM"] = length;
            beam.Properties["WidthM"] = width;
            beam.Properties["HeightM"] = height;
            return beam;
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
                throw new InvalidOperationException("Unexpected targeted-regeneration structural-freshness error.", ex);
            }

            throw new InvalidOperationException("Expected targeted-regeneration structural-freshness rejection.");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-9)
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("RegenerationEngineSubsetStructuralFreshnessSmoke: " + label + ".");
        }
    }
}
