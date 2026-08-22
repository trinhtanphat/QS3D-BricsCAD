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

        private static void Same<T>(T expected, T actual) where T : class
        {
            if (!ReferenceEquals(expected, actual)) throw new Exception("Expected normalized lookup to return the stored semantic object.");
        }

        private static void Null(object value)
        {
            if (value != null) throw new Exception("Expected blank/missing project semantic lookup to return null.");
        }
    }
}
