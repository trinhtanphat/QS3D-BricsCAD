using System;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleVariableKeyCanonicalizationSmoke
    {
        internal static void Run()
        {
            var project = new ProjectState("canonical-projection", "Canonical rule projection");
            var family = new ProjectFamily("beam", "Beam", ElementCategory.Beam);
            family.Properties[" Factor "] = "2";
            family.Properties["   "] = "111";
            project.Families.Add(family);

            var element = new ProjectElement("B1", ElementCategory.Beam, family.Id, "floor", "zone");
            element.Properties["factor"] = "3";
            element.Properties[" LengthM "] = "4";
            element.Properties["\t"] = "222";
            element.Quantities[" lengthm "] = 5d;
            element.Quantities["  "] = 333d;
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
    }
}
