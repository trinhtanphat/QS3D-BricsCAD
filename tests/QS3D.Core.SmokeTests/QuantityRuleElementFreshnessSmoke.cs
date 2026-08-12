using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleElementFreshnessSmoke
    {
        internal static void Run()
        {
            FirstDirectApplyTracksElementFreshness();
            IdenticalDirectApplyIsTimestampNoOp();
            ProvenanceOnlyVersionChangeTracksFreshnessWithoutDirtyMutation();
            StaleManagedOutputCleanupTracksFreshnessWithoutDirtyMutation();
            EmptyApplyMatchingIsTimestampNoOp();
        }

        private static void FirstDirectApplyTracksElementFreshness()
        {
            var element = Element("E-FIRST");
            var dirty = element.Dirty;
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            new QuantityRuleEngine().Apply(
                element,
                new QuantityRule("R", ElementCategory.Beam, "Computed", "Base*2", "1"),
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["Base"] = 2d });

            Require(element.UpdatedUtc > before, "first direct rule apply did not advance element UpdatedUtc");
            Equal(dirty, element.Dirty, "direct rule apply changed Dirty");
            Equal(4d, element.Quantities["Computed"], "direct rule apply quantity");
            Equal("R@1", element.Properties["Rule:Computed"], "direct rule apply provenance");
        }

        private static void IdenticalDirectApplyIsTimestampNoOp()
        {
            var engine = new QuantityRuleEngine();
            var element = Element("E-NOOP");
            var rule = new QuantityRule("R", ElementCategory.Beam, "Computed", "Base*2", "1");
            var variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["Base"] = 2d };
            engine.Apply(element, rule, variables);
            var dirty = element.Dirty;
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            engine.Apply(element, rule, variables);

            Equal(before, element.UpdatedUtc, "identical direct rule apply changed element UpdatedUtc");
            Equal(dirty, element.Dirty, "identical direct rule apply changed Dirty");
            Equal(4d, element.Quantities["Computed"], "identical direct rule apply changed quantity");
            Equal("R@1", element.Properties["Rule:Computed"], "identical direct rule apply changed provenance");
        }

        private static void ProvenanceOnlyVersionChangeTracksFreshnessWithoutDirtyMutation()
        {
            var engine = new QuantityRuleEngine();
            var element = Element("E-PROV");
            var variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["Base"] = 2d };
            engine.Apply(element, new QuantityRule("R", ElementCategory.Beam, "Computed", "Base*2", "1"), variables);
            var dirty = element.Dirty;
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            engine.Apply(element, new QuantityRule("R", ElementCategory.Beam, "Computed", "Base*2", "2"), variables);

            Require(element.UpdatedUtc > before, "provenance-only version change did not advance element UpdatedUtc");
            Equal(dirty, element.Dirty, "provenance-only version change changed Dirty");
            Equal(4d, element.Quantities["Computed"], "provenance-only version change changed quantity");
            Equal("R@2", element.Properties["Rule:Computed"], "provenance-only version change did not persist new provenance");
        }

        private static void StaleManagedOutputCleanupTracksFreshnessWithoutDirtyMutation()
        {
            var project = new ProjectState("P-CLEAN", "Rule cleanup");
            var element = Element("E-CLEAN");
            project.Elements.Add(element);
            element.Quantities["Managed"] = 7d;
            element.Properties["Rule:Managed"] = "OLD@1";
            element.Quantities["Unmanaged"] = 9d;
            element.Properties["Keep"] = "value";
            var dirty = element.Dirty;
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);

            Equal(1, applied, "stale managed cleanup operation count");
            Require(element.UpdatedUtc > before, "stale managed cleanup did not advance element UpdatedUtc");
            Equal(dirty, element.Dirty, "stale managed cleanup changed Dirty");
            Require(!element.Quantities.ContainsKey("Managed"), "stale managed quantity remained after cleanup");
            Require(!element.Properties.ContainsKey("Rule:Managed"), "stale managed provenance remained after cleanup");
            Equal(9d, element.Quantities["Unmanaged"], "cleanup changed unmanaged quantity");
            Equal("value", element.Properties["Keep"], "cleanup changed unrelated property");
        }

        private static void EmptyApplyMatchingIsTimestampNoOp()
        {
            var project = new ProjectState("P-EMPTY", "Rule empty");
            var element = Element("E-EMPTY");
            project.Elements.Add(element);
            var dirty = element.Dirty;
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);

            Equal(0, applied, "empty ApplyMatching operation count");
            Equal(before, element.UpdatedUtc, "empty ApplyMatching changed element UpdatedUtc");
            Equal(dirty, element.Dirty, "empty ApplyMatching changed Dirty");
        }

        private static ProjectElement Element(string id) =>
            new ProjectElement(id, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("QuantityRuleElementFreshnessSmoke: " + message + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("QuantityRuleElementFreshnessSmoke: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class QuantityRuleElementFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityRuleElementFreshnessSmoke.Run();
    }
}
