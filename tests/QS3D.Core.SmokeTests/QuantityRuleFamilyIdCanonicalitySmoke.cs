using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleFamilyIdCanonicalitySmoke
    {
        internal static void Run()
        {
            PaddedFamilyIdFailsBeforeStaleCleanup();
            WhitespaceOnlyFamilyIdRemainsValid();
            CaseVariedCanonicalFamilyIdStillProjectsFamilyProperties();
        }

        private static void PaddedFamilyIdFailsBeforeStaleCleanup()
        {
            var project = new ProjectState("p-rule-familyid-padding", "Rule FamilyId padding");
            var family = new ProjectFamily("FAM-1", "Beam family", ElementCategory.Beam);
            family.Properties["Factor"] = "2";
            project.Families.Add(family);

            var element = new ProjectElement("B1", ElementCategory.Beam, family.Id, string.Empty, string.Empty);
            element.FamilyId = " FAM-1 ";
            Equal("FAM-1", element.FamilyId);
            SetRawFamilyId(element, " FAM-1 ");
            Equal(" FAM-1 ", element.FamilyId);
            element.Quantities["OldManaged"] = 7d;
            element.Properties["Rule:OldManaged"] = "old@1";
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("beam-factor", ElementCategory.Beam, "Computed", "Factor*2", "1"));

            ThrowsNonCanonicalFamily(() => new QuantityRuleEngine().ApplyMatching(project, element));

            Near(7d, element.Quantities["OldManaged"]);
            Equal("old@1", element.Properties["Rule:OldManaged"]);
            True(!element.Quantities.ContainsKey("Computed"));
            True(!element.Properties.ContainsKey("Rule:Computed"));
        }

        private static void WhitespaceOnlyFamilyIdRemainsValid()
        {
            var project = new ProjectState("p-rule-familyid-blank", "Rule blank FamilyId");
            var element = new ProjectElement("B1", ElementCategory.Beam);
            element.FamilyId = "   ";
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("beam-count", ElementCategory.Beam, "Computed", "Count", "1"));

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);

            Equal(1, applied);
            Near(1d, element.Quantities["Computed"]);
            Equal("beam-count@1", element.Properties["Rule:Computed"]);
        }

        private static void CaseVariedCanonicalFamilyIdStillProjectsFamilyProperties()
        {
            var project = new ProjectState("p-rule-familyid-case", "Rule FamilyId case");
            var family = new ProjectFamily("FAM-1", "Beam family", ElementCategory.Beam);
            family.Properties["Factor"] = "2";
            project.Families.Add(family);

            var element = new ProjectElement("B1", ElementCategory.Beam, "fam-1", string.Empty, string.Empty);
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("beam-factor", ElementCategory.Beam, "Computed", "Factor*2", "1"));

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);

            Equal(1, applied);
            Near(4d, element.Quantities["Computed"]);
            Equal("beam-factor@1", element.Properties["Rule:Computed"]);
        }

        private static void SetRawFamilyId(ProjectElement element, string value)
        {
            var field = typeof(ProjectElement).GetField("_familyId", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(string))
                throw new InvalidOperationException("QuantityRuleFamilyIdCanonicalitySmoke could not resolve raw FamilyId backing field.");
            field.SetValue(element, value);
        }

        private static void ThrowsNonCanonicalFamily(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                var message = ex.Message ?? string.Empty;
                if (message.IndexOf("non-canonical", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    message.IndexOf("family", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("Quantity Rule rejected padded FamilyId for an unrelated reason.", ex);
            }

            throw new InvalidOperationException("Expected Quantity Rule to reject a padded nonblank FamilyId.");
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
    }

    internal static class QuantityRuleFamilyIdCanonicalitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityRuleFamilyIdCanonicalitySmoke.Run();
    }
}
