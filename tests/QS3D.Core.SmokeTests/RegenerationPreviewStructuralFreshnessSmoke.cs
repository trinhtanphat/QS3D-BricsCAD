using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationPreviewStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ReplacementDuringSubsetEnumerationFailsFreshness();
            StableSubsetStillPreviews();
        }

        private static void ReplacementDuringSubsetEnumerationFailsFreshness()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var original = project.FindElement("B2")!;
            var index = project.Elements.IndexOf(original);

            IEnumerable<string> Targets()
            {
                var replacement = Beam("B2", "4", "0.3", "0.4");
                project.Elements[index] = replacement;
                yield return "B1";
            }

            ThrowsStructuralFreshness(() => new RegenerationPreviewService().PreviewSubset(project, Targets()));
            Equal(beforeVersion, project.ChangeVersion, "direct replacement must leave ChangeVersion unchanged");
            if (ReferenceEquals(project.Elements[index], original))
                throw new InvalidOperationException("RegenerationPreviewStructuralFreshnessSmoke replacement fixture did not change element ownership.");
            True(!project.FindElement("B1")!.Quantities.ContainsKey("NetVolumeM3"), "failed preview must not mutate live target quantities");
        }

        private static void StableSubsetStillPreviews()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var preview = new RegenerationPreviewService().PreviewSubset(project, new[] { "B1" });

            Equal(beforeVersion, preview.SourceChangeVersion, "stable preview source revision");
            Equal(beforeVersion, project.ChangeVersion, "stable preview remains read-only");
            Equal(1, preview.TargetElementIds.Count, "stable target count");
            Equal("B1", preview.TargetElementIds[0], "stable target id");
            True(preview.Deltas.Any(x => string.Equals(x.ElementId, "B1", StringComparison.OrdinalIgnoreCase)), "stable preview must include B1 delta");
            True(!preview.Deltas.Any(x => string.Equals(x.ElementId, "B2", StringComparison.OrdinalIgnoreCase)), "stable subset must exclude B2 delta");
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-REGEN-PREVIEW-STRUCTURAL", "Regeneration Preview structural freshness");
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
                if (ex.Message.IndexOf("element ownership changed", StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Unexpected regeneration preview structural-freshness error.", ex);
            }

            throw new InvalidOperationException("Expected regeneration preview structural-freshness rejection.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "RegenerationPreviewStructuralFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("RegenerationPreviewStructuralFreshnessSmoke: " + label + ".");
        }
    }
}
