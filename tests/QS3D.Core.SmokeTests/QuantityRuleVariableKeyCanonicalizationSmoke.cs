using System;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleVariableKeyCanonicalizationSmoke
    {
        internal static void Run()
        {
            CanonicalCrossScopePrecedenceIsPreserved();
            SamePropertyMapCollisionFailsBeforeMutation();
        }

        private static void CanonicalCrossScopePrecedenceIsPreserved()
        {
            var project = new ProjectState("canonical-projection", "Canonical rule projection");
            var family = new ProjectFamily("beam", "Beam", ElementCategory.Beam);
            family.Properties["Factor"] = "2";
            family.Properties["   "] = "111";
            project.Families.Add(family);

            var element = new ProjectElement("B1", ElementCategory.Beam, family.Id, "floor", "zone");
            element.Properties["factor"] = "3";
            element.Properties["LengthM"] = "4";
            element.Properties["\t"] = "222";
            element.Quantities["LengthM"] = 5d;
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("beam-canonical-projected", ElementCategory.Beam, "ProjectedQuantity", "Factor*LengthM", "1"));

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);
            if (applied != 1)
                throw new InvalidOperationException("Expected one matching quantity rule, got " + applied + ".");
            if (!element.Quantities.TryGetValue("ProjectedQuantity", out var value) || Math.Abs(value - 15d) > 1e-12)
                throw new InvalidOperationException("Canonical variable precedence was not preserved.");
            if (!element.Properties.TryGetValue("Rule:ProjectedQuantity", out var provenance) || provenance != "beam-canonical-projected@1")
                throw new InvalidOperationException("Quantity rule provenance was not recorded.");
        }

        private static void SamePropertyMapCollisionFailsBeforeMutation()
        {
            var project = new ProjectState("canonical-collision", "Canonical rule collision");
            var element = new ProjectElement("B2", ElementCategory.Beam);
            element.Properties["Factor"] = "2";
            element.Properties[" Factor "] = "3";
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("beam-ambiguous", ElementCategory.Beam, "ProjectedQuantity", "Factor*2", "1"));

            var beforeUpdatedUtc = element.UpdatedUtc;
            var beforeDirty = element.Dirty;
            try
            {
                new QuantityRuleEngine().ApplyMatching(project, element);
                throw new InvalidOperationException("Expected non-canonical same-map variable key to fail closed.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "Expected non-canonical same-map variable key to fail closed.") throw;
            }

            if (element.Quantities.ContainsKey("ProjectedQuantity"))
                throw new InvalidOperationException("Rejected non-canonical variable key wrote a quantity output.");
            if (element.Properties.ContainsKey("Rule:ProjectedQuantity"))
                throw new InvalidOperationException("Rejected non-canonical variable key wrote provenance.");
            if (element.UpdatedUtc != beforeUpdatedUtc || element.Dirty != beforeDirty)
                throw new InvalidOperationException("Rejected non-canonical variable key changed element freshness state.");
        }
    }
}
