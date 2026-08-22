using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleNullPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var malformed = new ProjectState("QTY-RULE-NULL", "Quantity rule null");
            var malformedElement = new ProjectElement("E1", ElementCategory.Room);
            malformed.Elements.Add(malformedElement);
            malformedElement.SetQuantity("Stale", 9d);
            malformedElement.Properties["Rule:Stale"] = "OLD@1";
            malformed.QuantityRules.Add(null!);

            var updatedUtc = malformedElement.UpdatedUtc;
            var staleValue = malformedElement.Quantities["Stale"];
            var staleProvenance = malformedElement.Properties["Rule:Stale"];

            try
            {
                new QuantityRuleEngine().ApplyMatching(malformed, malformedElement);
                throw new InvalidOperationException("Quantity rule matching must reject a null persisted rule entry.");
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Project quantity rule collection contains a null rule.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Quantity rule matching must fail closed with the canonical null-rule integrity error.", ex);
            }

            if (!malformedElement.Quantities.TryGetValue("Stale", out var remaining) || remaining != staleValue)
                throw new InvalidOperationException("Rejected quantity-rule evaluation must not remove or change stale managed quantities.");
            if (!malformedElement.Properties.TryGetValue("Rule:Stale", out var provenance) || !string.Equals(provenance, staleProvenance, StringComparison.Ordinal))
                throw new InvalidOperationException("Rejected quantity-rule evaluation must not remove or change stale provenance.");
            if (malformedElement.UpdatedUtc != updatedUtc)
                throw new InvalidOperationException("Rejected quantity-rule evaluation must not change element freshness.");

            var valid = new ProjectState("QTY-RULE-VALID", "Quantity rule valid");
            var validElement = new ProjectElement("E1", ElementCategory.Room);
            valid.Elements.Add(validElement);
            valid.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Room, "Calculated", "2", "1"));

            var applied = new QuantityRuleEngine().ApplyMatching(valid, validElement);
            if (applied != 1)
                throw new InvalidOperationException("A valid matching quantity rule must report one applied rule.");
            if (!validElement.Quantities.TryGetValue("Calculated", out var calculated) || calculated != 2d)
                throw new InvalidOperationException("A valid matching quantity rule must publish its evaluated quantity.");
            if (!validElement.Properties.TryGetValue("Rule:Calculated", out var validProvenance) || !string.Equals(validProvenance, "R1@1", StringComparison.Ordinal))
                throw new InvalidOperationException("A valid matching quantity rule must publish canonical rule provenance.");
        }
    }
}
