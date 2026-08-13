using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementSetQuantityDirtySmoke
    {
        internal static void Run()
        {
            ChangedQuantityMarksOnlyQuantityDirty();
            IdenticalQuantityRemainsNoOp();
            SignedZeroIsCanonicalizedBySetter();
            QuantityOnlyMutationParticipatesInRegeneration();
        }

        private static void ChangedQuantityMarksOnlyQuantityDirty()
        {
            var element = NewCleanElement("quantity-dirty");
            element.Properties["GeneratedSolidHandle"] = "AB12";

            element.SetQuantity(" AreaM2 ", 12.5d);

            Require(element.Quantities.TryGetValue("AreaM2", out var value) && value == 12.5d,
                "SetQuantity did not preserve canonical quantity key/value semantics.");
            Require(element.Dirty == ElementDirtyFlags.Quantity,
                "Changed quantity did not set exactly the Quantity dirty flag.");
            Require(!element.IsGeneratedGeometryStale(),
                "Quantity-only mutation unexpectedly marked generated geometry stale.");
        }

        private static void IdenticalQuantityRemainsNoOp()
        {
            var element = NewCleanElement("quantity-noop");
            element.SetQuantity("AreaM2", 9.25d);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeUpdatedUtc = element.UpdatedUtc;

            element.SetQuantity(" AreaM2 ", 9.25d);

            Require(element.Dirty == ElementDirtyFlags.None,
                "Identical quantity value unexpectedly dirtied the element.");
            Require(element.UpdatedUtc == beforeUpdatedUtc,
                "Identical quantity value unexpectedly changed UpdatedUtc.");
        }

        private static void SignedZeroIsCanonicalizedBySetter()
        {
            var element = NewCleanElement("quantity-signed-zero");
            var negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);

            element.SetQuantity("AreaM2", negativeZero);

            Require(element.Quantities.TryGetValue("AreaM2", out var stored),
                "SetQuantity did not store the signed-zero quantity.");
            Require(stored == 0d,
                "SetQuantity signed zero must remain numerically zero.");
            Require(BitConverter.DoubleToInt64Bits(stored) == 0L,
                "SetQuantity must canonicalize negative zero to positive-zero bits.");
            Require(element.Dirty == ElementDirtyFlags.Quantity,
                "Initial signed-zero quantity write must keep normal Quantity dirty semantics.");

            element.MarkClean(ElementDirtyFlags.All);
            var beforeUpdatedUtc = element.UpdatedUtc;
            element.SetQuantity("AreaM2", 0d);

            Require(element.Dirty == ElementDirtyFlags.None,
                "Positive zero after canonical signed-zero storage must remain an identical-value no-op.");
            Require(element.UpdatedUtc == beforeUpdatedUtc,
                "Positive zero after canonical signed-zero storage unexpectedly changed UpdatedUtc.");
        }

        private static void QuantityOnlyMutationParticipatesInRegeneration()
        {
            var project = new ProjectState("quantity-regeneration", "Quantity regeneration");
            var element = NewCleanElement("quantity-regeneration-element");
            project.Elements.Add(element);
            element.SetQuantity("AreaM2", 4d);
            var probe = new ProbeRegenerator();
            var engine = new RegenerationEngine(new DependencyGraph(), new IElementRegenerator[] { probe });

            var regenerated = engine.RegenerateDirty(project);

            Require(regenerated == 1, "Quantity-only mutation was skipped by semantic regeneration.");
            Require(probe.Calls == 1, "Quantity-only mutation did not invoke the selected regenerator exactly once.");
        }

        private static ProjectElement NewCleanElement(string id)
        {
            var element = new ProjectElement(id, ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            element.MarkClean(ElementDirtyFlags.All);
            return element;
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private sealed class ProbeRegenerator : IElementRegenerator
        {
            internal int Calls { get; private set; }

            public bool CanRegenerate(ElementCategory category) => category == ElementCategory.Room;

            public void Regenerate(ProjectState project, ProjectElement element)
            {
                Calls++;
            }
        }
    }
}
