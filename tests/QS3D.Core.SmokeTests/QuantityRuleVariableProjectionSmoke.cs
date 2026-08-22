using System;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleVariableProjectionSmoke
    {
        internal static void Run()
        {
            var project = new ProjectState("projection", "Rule projection");
            var family = new ProjectFamily("beam", "Beam", ElementCategory.Beam);
            family.Properties["Factor"] = "2";
            family.Properties["   "] = "123";
            project.Families.Add(family);

            var element = new ProjectElement("B1", ElementCategory.Beam, family.Id, "floor", "zone");
            element.Properties["LengthM"] = "3";
            element.Properties["\t"] = "456";
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("beam-projected", ElementCategory.Beam, "ProjectedQuantity", "LengthM*Factor", "1"));

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);
            if (applied != 1)
                throw new InvalidOperationException("Expected one matching quantity rule, got " + applied + ".");
            if (!element.Quantities.TryGetValue("ProjectedQuantity", out var value) || Math.Abs(value - 6d) > 1e-12)
                throw new InvalidOperationException("Valid numeric variables were not projected correctly.");
            if (!element.Properties.TryGetValue("Rule:ProjectedQuantity", out var provenance) || provenance != "beam-projected@1")
                throw new InvalidOperationException("Quantity rule provenance was not recorded.");
        }
    }
}
