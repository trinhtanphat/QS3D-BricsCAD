using System;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateLookupSmoke
    {
        public static void Run()
        {
            LookupsNormalizeWhitespaceAndCase();
            BlankAndMissingLookupsReturnNull();
            DuplicateLookupsFailClosed();
        }

        private static void LookupsNormalizeWhitespaceAndCase()
        {
            var project = new ProjectState("P1", "Lookup");
            var element = new ProjectElement(" ELEMENT-1 ", ElementCategory.Wall, string.Empty, string.Empty, string.Empty);
            var family = new ProjectFamily(" FAMILY-1 ", "Family", ElementCategory.Wall);
            var rule = new QuantityRule(" RULE-1 ", ElementCategory.Wall, "VolumeM3", "1", "v1");
            project.Elements.Add(element);
            project.Families.Add(family);
            project.QuantityRules.Add(rule);

            Same(element, project.FindElement(" element-1 "));
            Same(family, project.FindFamily(" family-1 "));
            Same(rule, project.FindQuantityRule(" rule-1 "));
        }

        private static void BlankAndMissingLookupsReturnNull()
        {
            var project = new ProjectState("P1", "Lookup");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Wall, string.Empty, string.Empty, string.Empty));
            project.Families.Add(new ProjectFamily("F1", "Family", ElementCategory.Wall));
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Wall, "VolumeM3", "1", "v1"));

            Null(project.FindElement("   "));
            Null(project.FindFamily("   "));
            Null(project.FindQuantityRule("   "));
            Null(project.FindElement("missing"));
            Null(project.FindFamily("missing"));
            Null(project.FindQuantityRule("missing"));
        }

        private static void DuplicateLookupsFailClosed()
        {
            var project = new ProjectState("P1", "Duplicate lookup");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Wall, string.Empty, string.Empty, string.Empty));
            project.Elements.Add(new ProjectElement("e1", ElementCategory.Wall, string.Empty, string.Empty, string.Empty));
            project.Families.Add(new ProjectFamily("F1", "Family 1", ElementCategory.Wall));
            project.Families.Add(new ProjectFamily("f1", "Family 2", ElementCategory.Wall));
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Wall, "VolumeM3", "1", "v1"));
            project.QuantityRules.Add(new QuantityRule("r1", ElementCategory.Wall, "AreaM2", "1", "v1"));

            Throws<InvalidOperationException>(() => project.FindElement(" e1 "));
            Throws<InvalidOperationException>(() => project.FindFamily(" f1 "));
            Throws<InvalidOperationException>(() => project.FindQuantityRule(" r1 "));
        }

        private static void Same<T>(T expected, T actual) where T : class
        {
            if (!ReferenceEquals(expected, actual)) throw new Exception("Expected normalized lookup to return the stored semantic object.");
        }

        private static void Null(object value)
        {
            if (value != null) throw new Exception("Expected blank/missing project semantic lookup to return null.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
