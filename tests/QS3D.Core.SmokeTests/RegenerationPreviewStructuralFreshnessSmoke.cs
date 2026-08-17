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
            PropertyMutationDuringSubsetEnumerationFailsFreshness();
            QuantityMutationDuringSubsetEnumerationFailsFreshness();
            DependencyMutationDuringSubsetEnumerationFailsFreshness();
            SourceHandleMutationDuringSubsetEnumerationFailsFreshness();
            DirtyStateMutationDuringSubsetEnumerationFailsFreshness();
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

            ThrowsOwnershipFreshness(() => new RegenerationPreviewService().PreviewSubset(project, Targets()));
            Equal(beforeVersion, project.ChangeVersion, "direct replacement must leave ChangeVersion unchanged");
            if (ReferenceEquals(project.Elements[index], original))
                throw new InvalidOperationException("RegenerationPreviewStructuralFreshnessSmoke replacement fixture did not change element ownership.");
            True(!project.FindElement("B1")!.Quantities.ContainsKey("NetVolumeM3"), "failed preview must not mutate live target quantities");
        }

        private static void PropertyMutationDuringSubsetEnumerationFailsFreshness()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var target = project.FindElement("B1")!;

            IEnumerable<string> Targets()
            {
                target.Properties["LengthM"] = "600";
                yield return "B1";
            }

            ThrowsStateFreshness(() => new RegenerationPreviewService().PreviewSubset(project, Targets()), "B1");
            Equal(beforeVersion, project.ChangeVersion, "direct property mutation must leave ChangeVersion unchanged");
            Equal("600", target.Properties["LengthM"], "property mutation fixture must remain observable");
            True(!target.Quantities.ContainsKey("NetVolumeM3"), "failed property-race preview must not mutate live target quantities");
        }

        private static void QuantityMutationDuringSubsetEnumerationFailsFreshness()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var target = project.FindElement("B1")!;

            IEnumerable<string> Targets()
            {
                target.Quantities["InjectedQuantity"] = 999d;
                yield return "B1";
            }

            ThrowsStateFreshness(() => new RegenerationPreviewService().PreviewSubset(project, Targets()), "B1");
            Equal(beforeVersion, project.ChangeVersion, "direct quantity mutation must leave ChangeVersion unchanged");
            Equal(999d, target.Quantities["InjectedQuantity"], "quantity mutation fixture must remain observable");
            True(!target.Quantities.ContainsKey("NetVolumeM3"), "failed quantity-race preview must not mutate regenerated live target quantities");
        }

        private static void DependencyMutationDuringSubsetEnumerationFailsFreshness()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var target = project.FindElement("B1")!;

            IEnumerable<string> Targets()
            {
                target.DependsOn.Add("B2");
                yield return "B1";
            }

            ThrowsStateFreshness(() => new RegenerationPreviewService().PreviewSubset(project, Targets()), "B1");
            Equal(beforeVersion, project.ChangeVersion, "direct dependency mutation must leave ChangeVersion unchanged");
            Equal(1, target.DependsOn.Count, "dependency mutation fixture count");
            Equal("B2", target.DependsOn[0], "dependency mutation fixture value");
            True(!target.Quantities.ContainsKey("NetVolumeM3"), "failed dependency-race preview must not mutate live target quantities");
        }

        private static void SourceHandleMutationDuringSubsetEnumerationFailsFreshness()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var target = project.FindElement("B1")!;

            IEnumerable<string> Targets()
            {
                target.SourceHandles.Add("A1");
                yield return "B1";
            }

            ThrowsStateFreshness(() => new RegenerationPreviewService().PreviewSubset(project, Targets()), "B1");
            Equal(beforeVersion, project.ChangeVersion, "direct source-handle mutation must leave ChangeVersion unchanged");
            Equal(1, target.SourceHandles.Count, "source-handle mutation fixture count");
            Equal("A1", target.SourceHandles[0], "source-handle mutation fixture value");
            True(!target.Quantities.ContainsKey("NetVolumeM3"), "failed source-handle-race preview must not mutate live target quantities");
        }

        private static void DirtyStateMutationDuringSubsetEnumerationFailsFreshness()
        {
            var project = Fixture();
            var beforeVersion = project.ChangeVersion;
            var target = project.FindElement("B1")!;
            Equal(ElementDirtyFlags.All, target.Dirty, "dirty-state fixture starts dirty");

            IEnumerable<string> Targets()
            {
                target.MarkClean(ElementDirtyFlags.All);
                yield return "B1";
            }

            ThrowsStateFreshness(() => new RegenerationPreviewService().PreviewSubset(project, Targets()), "B1");
            Equal(beforeVersion, project.ChangeVersion, "direct dirty-state mutation must leave ChangeVersion unchanged");
            Equal(ElementDirtyFlags.None, target.Dirty, "dirty-state mutation fixture must remain observable");
            True(!target.Quantities.ContainsKey("NetVolumeM3"), "failed dirty-state-race preview must not mutate live target quantities");
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

        private static void ThrowsOwnershipFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("element ownership changed", StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Unexpected regeneration preview ownership-freshness error.", ex);
            }

            throw new InvalidOperationException("Expected regeneration preview ownership-freshness rejection.");
        }

        private static void ThrowsStateFreshness(Action action, string elementId)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("element state changed", StringComparison.Ordinal) >= 0 &&
                    ex.Message.IndexOf(elementId, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("Unexpected regeneration preview element-state freshness error.", ex);
            }

            throw new InvalidOperationException("Expected regeneration preview element-state freshness rejection.");
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