using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleDirtyPropagationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var project = new ProjectState("RULE-DIRTY", "Quantity Rule Dirty");
            var element = new ProjectElement("E1", ElementCategory.Beam);
            project.Elements.Add(element);
            var engine = new QuantityRuleEngine();

            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Beam, "RuleQty", "1", "1"));
            engine.ApplyMatching(project, element);
            element.MarkClean(ElementDirtyFlags.All);

            engine.ApplyMatching(project, element);
            if (element.Dirty != ElementDirtyFlags.None)
                throw new InvalidOperationException("Unchanged rule quantity/provenance must remain clean.");

            project.QuantityRules.Clear();
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Beam, "RuleQty", "1", "2"));
            engine.ApplyMatching(project, element);
            if ((element.Dirty & ElementDirtyFlags.Properties) == 0)
                throw new InvalidOperationException("Rule provenance change must mark Properties dirty.");
            if ((element.Dirty & ElementDirtyFlags.Quantity) == 0)
                throw new InvalidOperationException("Rule provenance change must mark Quantity dirty.");
            if (!element.Quantities.TryGetValue("RuleQty", out var value) || value != 1d)
                throw new InvalidOperationException("Smoke requires the numeric rule output to remain unchanged.");
            if (!element.Properties.TryGetValue("Rule:RuleQty", out var provenance) ||
                !string.Equals(provenance, "R1@2", StringComparison.Ordinal))
                throw new InvalidOperationException("Rule provenance must update to the new version.");

            element.MarkClean(ElementDirtyFlags.All);
            project.QuantityRules.Clear();
            engine.ApplyMatching(project, element);
            if ((element.Dirty & ElementDirtyFlags.Properties) == 0 ||
                (element.Dirty & ElementDirtyFlags.Quantity) == 0)
                throw new InvalidOperationException("Stale rule cleanup must mark persisted quantity/provenance dirty.");
            if (element.Quantities.ContainsKey("RuleQty") || element.Properties.ContainsKey("Rule:RuleQty"))
                throw new InvalidOperationException("Stale rule quantity/provenance must be removed.");
        }
    }
}
