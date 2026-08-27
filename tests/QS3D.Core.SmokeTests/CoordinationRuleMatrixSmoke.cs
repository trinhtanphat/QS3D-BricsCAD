using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationRuleMatrixSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactRuleWinsAndPairOrderIsSymmetric();
            EquallySpecificRulesFailClosed();
            DisabledRulesAreIgnored();
            RuleValidationFailsClosed();
            ProfileIdentityRejectsNonCanonicalWhitespace();
            ActualCategoriesRejectWildcardButRuleWildcardsRemainValid();
            ResolutionCarriesProfileAndRuleVersion();
        }

        private static void ExactRuleWinsAndPairOrderIsSymmetric()
        {
            var profile = new CoordinationRuleProfile(
                "DEFAULT-MEP",
                3,
                new[]
                {
                    new CoordinationRule("GENERIC-MEP", 1, "Pipe", "*", CoordinationRuleKind.Clearance, "Warning", 0.05d),
                    new CoordinationRule("PIPE-BEAM-HARD", 4, "Pipe", "Beam", CoordinationRuleKind.HardClash, "Error", 0d)
                });

            var forward = profile.Resolve("Pipe", "Beam") ?? throw new InvalidOperationException("Expected forward rule resolution.");
            var reverse = profile.Resolve("beam", "pipe") ?? throw new InvalidOperationException("Expected reverse rule resolution.");
            Equal("PIPE-BEAM-HARD", forward.RuleId, "exact rule did not outrank wildcard rule");
            Equal(forward.RuleId, reverse.RuleId, "rule resolution changed when pair order changed");
            Equal(CoordinationRuleKind.HardClash, reverse.Kind, "resolved rule kind changed when pair order changed");
        }

        private static void EquallySpecificRulesFailClosed()
        {
            var profile = new CoordinationRuleProfile(
                "AMBIGUOUS",
                1,
                new[]
                {
                    new CoordinationRule("PIPE-ANY", 1, "Pipe", "*", CoordinationRuleKind.Clearance, "Warning", 0.05d),
                    new CoordinationRule("ANY-BEAM", 1, "*", "Beam", CoordinationRuleKind.Clearance, "Warning", 0.10d)
                });

            Throws<InvalidOperationException>(() => profile.Resolve("Pipe", "Beam"));
        }

        private static void DisabledRulesAreIgnored()
        {
            var profile = new CoordinationRuleProfile(
                "DISABLED",
                1,
                new[]
                {
                    new CoordinationRule("OLD", 1, "Pipe", "Beam", CoordinationRuleKind.HardClash, "Error", 0d, false),
                    new CoordinationRule("FALLBACK", 2, "*", "*", CoordinationRuleKind.Clearance, "Info", 0.025d)
                });

            var resolved = profile.Resolve("Pipe", "Beam") ?? throw new InvalidOperationException("Expected fallback rule resolution.");
            Equal("FALLBACK", resolved.RuleId, "disabled exact rule participated in resolution");
        }

        private static void RuleValidationFailsClosed()
        {
            Throws<ArgumentException>(() =>
                new CoordinationRule("BAD-HARD", 1, "Pipe", "Beam", CoordinationRuleKind.HardClash, "Error", 0.01d));
            Throws<ArgumentOutOfRangeException>(() =>
                new CoordinationRule("BAD-CLEARANCE", 1, "Pipe", "Beam", CoordinationRuleKind.Clearance, "Warning", double.NaN));
            Throws<ArgumentException>(() =>
                new CoordinationRuleProfile(
                    "DUPLICATE-ID",
                    1,
                    new[]
                    {
                        new CoordinationRule("RULE", 1, "Pipe", "Beam", CoordinationRuleKind.HardClash, "Error", 0d),
                        new CoordinationRule("rule", 2, "Pipe", "Wall", CoordinationRuleKind.HardClash, "Error", 0d)
                    }));
        }

        private static void ProfileIdentityRejectsNonCanonicalWhitespace()
        {
            var rule = new CoordinationRule(
                "PROFILE-ID-BOUNDARY",
                1,
                "Pipe",
                "Beam",
                CoordinationRuleKind.HardClash,
                "Error",
                0d);

            Throws<ArgumentException>(() =>
                new CoordinationRuleProfile(" PROJECT-A ", 1, new[] { rule }));
        }

        private static void ActualCategoriesRejectWildcardButRuleWildcardsRemainValid()
        {
            var profile = new CoordinationRuleProfile(
                "WILDCARD-BOUNDARY",
                1,
                new[]
                {
                    new CoordinationRule("PIPE-ANY", 1, "Pipe", "*", CoordinationRuleKind.Clearance, "Warning", 0.05d),
                    new CoordinationRule("FALLBACK", 1, "*", "*", CoordinationRuleKind.Clearance, "Info", 0.10d)
                });

            var resolved = profile.Resolve("Pipe", "Wall") ??
                           throw new InvalidOperationException("Expected wildcard rule to match concrete categories.");
            Equal("PIPE-ANY", resolved.RuleId, "rule wildcard stopped matching concrete category input");

            Throws<ArgumentException>(() => profile.Resolve("*", "Beam"));
            Throws<ArgumentException>(() => profile.Resolve("Pipe", "*"));
        }

        private static void ResolutionCarriesProfileAndRuleVersion()
        {
            var profile = new CoordinationRuleProfile(
                "PROJECT-A",
                7,
                new[]
                {
                    new CoordinationRule("DUCT-WALL-CLEARANCE", 12, "Duct", "Wall", CoordinationRuleKind.Clearance, "Warning", 0.075d)
                });

            var resolved = profile.Resolve("Wall", "Duct") ?? throw new InvalidOperationException("Expected trace rule resolution.");
            Equal("PROJECT-A", resolved.ProfileId, "profile identity was not projected");
            Equal(7, resolved.ProfileVersion, "profile version was not projected");
            Equal("DUCT-WALL-CLEARANCE", resolved.RuleId, "rule identity was not projected");
            Equal(12, resolved.RuleVersion, "rule version was not projected");
            Equal(0.075d, resolved.Clearance, "clearance was not projected");

            if (profile.Resolve("Cable", "Slab") != null)
                throw new InvalidOperationException("CoordinationRuleMatrixSmoke: unmatched category pair unexpectedly resolved a rule.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("CoordinationRuleMatrixSmoke: expected " + typeof(T).Name + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "CoordinationRuleMatrixSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    "CoordinationRuleMatrixSmoke: " + message + ". Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(double expected, double actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    "CoordinationRuleMatrixSmoke: " + message + ". Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(CoordinationRuleKind expected, CoordinationRuleKind actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    "CoordinationRuleMatrixSmoke: " + message + ". Expected " + expected + ", got " + actual + ".");
        }
    }
}
