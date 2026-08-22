using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleDuplicateIdPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var malformed = new ProjectState("QTY-RULE-DUP-ID", "Quantity rule duplicate id");
            var malformedElement = new ProjectElement("E1", ElementCategory.Room);
            malformed.Elements.Add(malformedElement);
            malformedElement.SetQuantity("Stale", 7d);
            malformedElement.Properties["Rule:Stale"] = "OLD@1";
            malformed.QuantityRules.Add(new QuantityRule("RULE-1", ElementCategory.Room, "OutputA", "1", "1"));
            malformed.QuantityRules.Add(new QuantityRule("rule-1", ElementCategory.Door, "OutputB", "2", "1"));

            var updatedUtc = malformedElement.UpdatedUtc;
            var staleValue = malformedElement.Quantities["Stale"];
            var staleProvenance = malformedElement.Properties["Rule:Stale"];

            try
            {
                new QuantityRuleEngine().ApplyMatching(malformed, malformedElement);
                throw new InvalidOperationException("Quantity rule matching must reject duplicate persisted rule ids.");
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "Project contains duplicate quantity rule id: rule-1", StringComparison.Ordinal))
                    throw new InvalidOperationException("Quantity rule matching must fail closed with the canonical duplicate-rule-id error.", ex);
            }

            if (!malformedElement.Quantities.TryGetValue("Stale", out var remaining) || remaining != staleValue)
                throw new InvalidOperationException("Rejected duplicate rule identities must not remove or change stale managed quantities.");
            if (!malformedElement.Properties.TryGetValue("Rule:Stale", out var provenance) || !string.Equals(provenance, staleProvenance, StringComparison.Ordinal))
                throw new InvalidOperationException("Rejected duplicate rule identities must not remove or change stale provenance.");
            if (malformedElement.UpdatedUtc != updatedUtc)
                throw new InvalidOperationException("Rejected duplicate rule identities must not change element freshness.");

            var valid = new ProjectState("QTY-RULE-DISTINCT-ID", "Quantity rule distinct ids");
            var validElement = new ProjectElement("E1", ElementCategory.Room);
            valid.Elements.Add(validElement);
            valid.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Room, "OutputA", "1", "1"));
            valid.QuantityRules.Add(new QuantityRule("R2", ElementCategory.Room, "OutputB", "2", "3"));

            var applied = new QuantityRuleEngine().ApplyMatching(valid, validElement);
            if (applied != 2)
                throw new InvalidOperationException("Distinct matching Quantity Rule ids must continue to apply normally.");
            if (!validElement.Properties.TryGetValue("Rule:OutputA", out var provenanceA) || !string.Equals(provenanceA, "R1@1", StringComparison.Ordinal))
                throw new InvalidOperationException("Distinct rule R1 must publish its canonical provenance.");
            if (!validElement.Properties.TryGetValue("Rule:OutputB", out var provenanceB) || !string.Equals(provenanceB, "R2@3", StringComparison.Ordinal))
                throw new InvalidOperationException("Distinct rule R2 must publish its canonical provenance.");
        }
    }
}
