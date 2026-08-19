using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleNegativeResultAtomicitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsNegativeResultBeforeAnyMutation();
            AppliesNonNegativeRulesAndCleansStaleOutputs();
        }

        private static void RejectsNegativeResultBeforeAnyMutation()
        {
            var project = new ProjectState("RULE-NEGATIVE-ATOMIC", "Quantity rule atomicity");
            var element = new ProjectElement("BEAM-1", ElementCategory.Beam);
            project.Elements.Add(element);

            element.SetQuantity("Legacy", 7d);
            element.SetProperty("Rule:Legacy", "legacy@1");
            project.QuantityRules.Add(new QuantityRule("A-positive", ElementCategory.Beam, "Positive", "2", "1"));
            project.QuantityRules.Add(new QuantityRule("B-negative", ElementCategory.Beam, "Negative", "0-1", "1"));

            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));

            Equal(7d, RequiredQuantity(element, "Legacy"), "stale quantity changed after rejected negative rule");
            Equal("legacy@1", RequiredProperty(element, "Rule:Legacy"), "stale provenance changed after rejected negative rule");
            MissingQuantity(element, "Positive", "earlier positive rule was partially applied");
            MissingProperty(element, "Rule:Positive", "earlier positive provenance was partially applied");
            MissingQuantity(element, "Negative", "negative rule quantity was partially applied");
            MissingProperty(element, "Rule:Negative", "negative rule provenance was partially applied");
        }

        private static void AppliesNonNegativeRulesAndCleansStaleOutputs()
        {
            var project = new ProjectState("RULE-NONNEGATIVE-CONTROL", "Quantity rule successful control");
            var element = new ProjectElement("BEAM-2", ElementCategory.Beam);
            project.Elements.Add(element);

            element.SetQuantity("Legacy", 7d);
            element.SetProperty("Rule:Legacy", "legacy@1");
            project.QuantityRules.Add(new QuantityRule("A-base", ElementCategory.Beam, "Base", "2", "1"));
            project.QuantityRules.Add(new QuantityRule("B-derived", ElementCategory.Beam, "Derived", "Base+3", "1"));

            var changed = new QuantityRuleEngine().ApplyMatching(project, element);

            Equal(3, changed, "successful apply change count");
            MissingQuantity(element, "Legacy", "stale quantity was not removed");
            MissingProperty(element, "Rule:Legacy", "stale provenance was not removed");
            Equal(2d, RequiredQuantity(element, "Base"), "base quantity");
            Equal(5d, RequiredQuantity(element, "Derived"), "derived quantity");
            Equal("A-base@1", RequiredProperty(element, "Rule:Base"), "base provenance");
            Equal("B-derived@1", RequiredProperty(element, "Rule:Derived"), "derived provenance");
        }

        private static double RequiredQuantity(ProjectElement element, string name)
        {
            if (!element.Quantities.TryGetValue(name, out var value))
                throw new Exception("QuantityRuleNegativeResultAtomicitySmoke missing quantity: " + name + ".");
            return value;
        }

        private static string RequiredProperty(ProjectElement element, string name)
        {
            if (!element.Properties.TryGetValue(name, out var value))
                throw new Exception("QuantityRuleNegativeResultAtomicitySmoke missing property: " + name + ".");
            return value;
        }

        private static void MissingQuantity(ProjectElement element, string name, string message)
        {
            if (element.Quantities.ContainsKey(name))
                throw new Exception("QuantityRuleNegativeResultAtomicitySmoke " + message + ": " + name + ".");
        }

        private static void MissingProperty(ProjectElement element, string name, string message)
        {
            if (element.Properties.ContainsKey(name))
                throw new Exception("QuantityRuleNegativeResultAtomicitySmoke " + message + ": " + name + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception(
                    "QuantityRuleNegativeResultAtomicitySmoke " + label +
                    ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new Exception(
                "QuantityRuleNegativeResultAtomicitySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
