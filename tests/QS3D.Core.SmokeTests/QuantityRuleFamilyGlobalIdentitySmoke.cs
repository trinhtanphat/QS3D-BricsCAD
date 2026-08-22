using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleFamilyGlobalIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            UnrelatedDuplicateFamiliesFailBeforeRuleMutation();
            ValidFamilyRuleEvaluationStillWorks();
        }

        private static void UnrelatedDuplicateFamiliesFailBeforeRuleMutation()
        {
            var project = new ProjectState("RULE-FAMILY-DUP", "Rule Family duplicate identity");
            project.Families.Add(new ProjectFamily("F1", "Duplicate A", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("f1", "Duplicate B", ElementCategory.Beam));
            project.Families.Add(new ProjectFamily("F2", "Target", ElementCategory.Beam));

            var element = new ProjectElement("E1", ElementCategory.Beam, "F2", string.Empty, string.Empty);
            element.Properties["Keep"] = "original";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Beam, "RuleQ", "2", "1"));

            var beforeUpdatedUtc = element.UpdatedUtc;
            try
            {
                new QuantityRuleEngine().ApplyMatching(project, element);
            }
            catch (InvalidOperationException ex)
            {
                if ((ex.Message ?? string.Empty).IndexOf("duplicate family id", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Quantity rule Family identity preflight returned the wrong failure.", ex);

                Require(element.Quantities.Count == 0, "Rejected rule evaluation wrote a quantity.");
                Require(element.Properties.Count == 1 && element.Properties["Keep"] == "original",
                    "Rejected rule evaluation changed element properties/provenance.");
                Require(element.Dirty == ElementDirtyFlags.None, "Rejected rule evaluation dirtied the element.");
                Require(element.UpdatedUtc == beforeUpdatedUtc, "Rejected rule evaluation changed element persistence time.");
                return;
            }

            throw new InvalidOperationException("QuantityRuleEngine accepted an unrelated duplicate Family-ID collection.");
        }

        private static void ValidFamilyRuleEvaluationStillWorks()
        {
            var project = new ProjectState("RULE-FAMILY-VALID", "Rule Family valid control");
            project.Families.Add(new ProjectFamily("F2", "Target", ElementCategory.Beam));
            var element = new ProjectElement("E1", ElementCategory.Beam, "F2", string.Empty, string.Empty);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Beam, "RuleQ", "2", "1"));

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);

            Require(applied == 1, "Valid quantity rule evaluation did not report one applied rule.");
            Require(element.Quantities.TryGetValue("RuleQ", out var value) && value.Equals(2d),
                "Valid quantity rule evaluation did not write the expected output.");
            Require(element.Properties.TryGetValue("Rule:RuleQ", out var provenance) &&
                    string.Equals(provenance, "R1@1", StringComparison.Ordinal),
                "Valid quantity rule evaluation did not write canonical provenance.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
