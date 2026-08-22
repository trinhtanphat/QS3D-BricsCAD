using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleNumericPropertyKeyCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsPaddedElementNumericKey();
            RejectsPaddedFamilyNumericKey();
            PreservesCanonicalNumericKeys();
            PreservesNonNumericPropertyHandling();
        }

        private static void RejectsPaddedElementNumericKey()
        {
            var project = ProjectWithRule("LengthM");
            var element = new ProjectElement("E-PAD", ElementCategory.Beam, "", "", "");
            element.Properties[" LengthM "] = "2.5";
            project.Elements.Add(element);

            ExpectInvalid(() => new QuantityRuleEngine().ApplyMatching(project, element));
            Assert(!element.Quantities.ContainsKey("Computed"), "Rejected padded element numeric key mutated the rule output.");
        }

        private static void RejectsPaddedFamilyNumericKey()
        {
            var project = ProjectWithRule("LengthM");
            var family = new ProjectFamily("F-PAD", "Padded family", ElementCategory.Beam);
            family.Properties[" LengthM "] = "3.5";
            project.Families.Add(family);
            var element = new ProjectElement("E-FAMILY-PAD", ElementCategory.Beam, family.Id, "", "");
            project.Elements.Add(element);

            ExpectInvalid(() => new QuantityRuleEngine().ApplyMatching(project, element));
            Assert(!element.Quantities.ContainsKey("Computed"), "Rejected padded family numeric key mutated the rule output.");
        }

        private static void PreservesCanonicalNumericKeys()
        {
            var project = ProjectWithRule("LengthM");
            var element = new ProjectElement("E-OK", ElementCategory.Beam, "", "", "");
            element.Properties["LengthM"] = "4.25";
            project.Elements.Add(element);

            var changed = new QuantityRuleEngine().ApplyMatching(project, element);
            Assert(changed == 1, "Canonical numeric rule did not apply exactly once.");
            Assert(element.Quantities.TryGetValue("Computed", out var value) && value == 4.25d, "Canonical numeric key behavior changed.");
        }

        private static void PreservesNonNumericPropertyHandling()
        {
            var project = ProjectWithRule("Count");
            var element = new ProjectElement("E-TEXT", ElementCategory.Beam, "", "", "");
            element.Properties[" Label "] = "not-a-number";
            project.Elements.Add(element);

            new QuantityRuleEngine().ApplyMatching(project, element);
            Assert(element.Quantities.TryGetValue("Computed", out var value) && value == 1d, "Non-numeric property normalization behavior changed.");
        }

        private static ProjectState ProjectWithRule(string expression)
        {
            var project = new ProjectState("quantity-rule-key-smoke", "Quantity rule key smoke");
            project.QuantityRules.Add(new QuantityRule("RULE-1", ElementCategory.Beam, "Computed", expression, "1"));
            return project;
        }

        private static void ExpectInvalid(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Padded numeric property key must fail closed.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
