using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationRuleTokenControlSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsRuleTokenControlsBeforeTrim();
            RejectsProfileTokenControlsBeforeTrim();
            RejectsBindingTokenControlsBeforeTrim();
            PreservesEditableSpaceNormalizationAndRejectsBindingPadding();
        }

        private static void RejectsRuleTokenControlsBeforeTrim()
        {
            Throws<ArgumentException>(() => Rule("\tRULE-1", "Pipe", "Beam", "High"), "leading-tab rule id");
            Throws<ArgumentException>(() => Rule("RULE-1\r", "Pipe", "Beam", "High"), "trailing-cr rule id");
            Throws<ArgumentException>(() => Rule("RULE-1", "\nPipe", "Beam", "High"), "leading-lf category");
            Throws<ArgumentException>(() => Rule("RULE-1", "Pipe", "Beam\t", "High"), "trailing-tab category");
            Throws<ArgumentException>(() => Rule("RULE-1", "Pipe", "Beam", "High\n"), "trailing-lf severity");
            Throws<ArgumentException>(() => Rule("RULE\t1", "Pipe", "Beam", "High"), "embedded-tab rule id");
        }

        private static void RejectsProfileTokenControlsBeforeTrim()
        {
            var rule = Rule("RULE-1", "Pipe", "Beam", "High");
            Throws<ArgumentException>(() => new CoordinationRuleProfile("\tPROFILE-1", 1, new[] { rule }), "leading-tab profile id");
            Throws<ArgumentException>(() => new CoordinationRuleProfile("PROFILE-1\r", 1, new[] { rule }), "trailing-cr profile id");
        }

        private static void RejectsBindingTokenControlsBeforeTrim()
        {
            Throws<ArgumentException>(() => new CoordinationRuleProfileBinding("\nPROFILE-1", 1), "leading-lf binding id");
            Throws<ArgumentException>(() => new CoordinationRuleProfileBinding("PROFILE-1\t", 1), "trailing-tab binding id");
        }

        private static void PreservesEditableSpaceNormalizationAndRejectsBindingPadding()
        {
            var rule = Rule("  RULE-1  ", "  Pipe  ", " Beam ", "  High  ");
            Equal("RULE-1", rule.RuleId, "rule id trim");
            Equal("Pipe", rule.LeftCategory, "left category trim");
            Equal("Beam", rule.RightCategory, "right category trim");
            Equal("High", rule.Severity, "severity trim");

            var profile = new CoordinationRuleProfile("PROFILE-1", 1, new[] { rule });
            Equal("PROFILE-1", profile.ProfileId, "canonical profile id");

            Throws<ArgumentException>(
                () => new CoordinationRuleProfileBinding("  PROFILE-1  ", 1),
                "padded exact binding id");

            var resolution = profile.Resolve(" Pipe ", " Beam ");
            Equal("RULE-1", resolution?.RuleId, "ordinary-space resolution");
        }

        private static CoordinationRule Rule(string ruleId, string left, string right, string severity)
        {
            return new CoordinationRule(ruleId, 1, left, right, CoordinationRuleKind.HardClash, severity, 0d);
        }

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("CoordinationRuleTokenControlSmoke " + label + ": expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("CoordinationRuleTokenControlSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
