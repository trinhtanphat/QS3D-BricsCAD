using System;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleCanonicalNumericKeySmoke
    {
        internal static void Run()
        {
            CanonicalNumericKeyStillEvaluates();
            PaddedElementNumericKeyFailsClosed();
            PaddedFamilyNumericKeyFailsClosed();
        }

        private static void CanonicalNumericKeyStillEvaluates()
        {
            var project = CreateProject(out var family, out var element);
            element.Properties["Width"] = "2.5";

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);

            Equal(1, applied, "canonical rule count");
            Near(2.5, element.Quantities["Result"], "canonical result");
        }

        private static void PaddedElementNumericKeyFailsClosed()
        {
            var project = CreateProject(out _, out var element);
            element.Properties[" Width "] = "2.5";

            ThrowsNonCanonical(() => new QuantityRuleEngine().ApplyMatching(project, element), "element padded numeric key");
        }

        private static void PaddedFamilyNumericKeyFailsClosed()
        {
            var project = CreateProject(out var family, out var element);
            family.Properties[" Width "] = "2.5";

            ThrowsNonCanonical(() => new QuantityRuleEngine().ApplyMatching(project, element), "family padded numeric key");
        }

        private static ProjectState CreateProject(out ProjectFamily family, out ProjectElement element)
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Quantity Rule Canonical Key Smoke");
            project.Zones.Add(new ZoneDefinition("zone-1", "Zone"));
            project.Floors.Add(new FloorDefinition("floor-1", "Floor", 0));
            project.ActiveZoneId = "zone-1";
            project.ActiveFloorId = "floor-1";

            family = new ProjectFamily("family-1", "Family", ElementCategory.Room);
            project.Families.Add(family);
            element = new ProjectElement("element-1", ElementCategory.Room, family.Id, "floor-1", "zone-1");
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("rule-1", ElementCategory.Room, "Result", "Width", "1"));
            return project;
        }

        private static void ThrowsNonCanonical(Action action, string scope)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("non-canonical", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception(scope + " threw the wrong error: " + ex.Message);
            }

            throw new Exception(scope + " did not fail closed.");
        }

        private static void Near(double expected, double actual, string scope)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new Exception(scope + ": expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(int expected, int actual, string scope)
        {
            if (expected != actual)
                throw new Exception(scope + ": expected " + expected + ", got " + actual + ".");
        }
    }
}
