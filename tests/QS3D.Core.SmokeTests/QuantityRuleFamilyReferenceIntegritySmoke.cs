using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleFamilyReferenceIntegritySmoke
    {
        public static void Run()
        {
            DanglingFamilyFailsBeforeStaleCleanup();
            MismatchedFamilyFailsBeforeRuleMutation();
            BlankFamilyRemainsValid();
            ValidFamilyProjectionPreservesInstancePrecedence();
        }

        private static void DanglingFamilyFailsBeforeStaleCleanup()
        {
            var project = new ProjectState("p-dangling-family", "Dangling family");
            var element = new ProjectElement("B1", ElementCategory.Beam, "missing-family", string.Empty, string.Empty);
            element.Quantities["OldManaged"] = 7d;
            element.Properties["Rule:OldManaged"] = "old@1";
            project.Elements.Add(element);

            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));

            Near(7d, element.Quantities["OldManaged"]);
            Equal("old@1", element.Properties["Rule:OldManaged"]);
        }

        private static void MismatchedFamilyFailsBeforeRuleMutation()
        {
            var project = new ProjectState("p-mismatched-family", "Mismatched family");
            var family = new ProjectFamily("shared-family", "Slab family", ElementCategory.Slab);
            family.Properties["FamilyFactor"] = "7";
            project.Families.Add(family);
            var element = new ProjectElement("B1", ElementCategory.Beam, family.Id, string.Empty, string.Empty);
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("beam-family-factor", ElementCategory.Beam, "Computed", "FamilyFactor", "1"));

            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));

            True(!element.Quantities.ContainsKey("Computed"));
            True(!element.Properties.ContainsKey("Rule:Computed"));
        }

        private static void BlankFamilyRemainsValid()
        {
            var project = new ProjectState("p-blank-family", "Blank family");
            var element = new ProjectElement("B1", ElementCategory.Beam, "   ", string.Empty, string.Empty);
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("beam-count", ElementCategory.Beam, "Computed", "Count", "1"));

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);

            Equal(1, applied);
            Near(1d, element.Quantities["Computed"]);
            Equal("beam-count@1", element.Properties["Rule:Computed"]);
        }

        private static void ValidFamilyProjectionPreservesInstancePrecedence()
        {
            var project = new ProjectState("p-valid-family", "Valid family");
            var family = new ProjectFamily("beam-family", "Beam family", ElementCategory.Beam);
            family.Properties["Factor"] = "2";
            project.Families.Add(family);
            var element = new ProjectElement("B1", ElementCategory.Beam, family.Id, string.Empty, string.Empty);
            element.Properties["Factor"] = "3";
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("beam-factor", ElementCategory.Beam, "Computed", "Factor*2", "1"));

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);

            Equal(1, applied);
            Near(6d, element.Quantities["Computed"]);
            Equal("beam-factor@1", element.Properties["Rule:Computed"]);
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9d)
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected true.");
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
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class QuantityRuleFamilyReferenceIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            QuantityRuleFamilyReferenceIntegritySmoke.Run();
        }
    }
}
