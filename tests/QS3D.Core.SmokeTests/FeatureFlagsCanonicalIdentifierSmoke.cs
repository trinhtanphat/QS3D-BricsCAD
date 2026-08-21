using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class FeatureFlagsCanonicalIdentifierSmoke
    {
        internal static void Run()
        {
            var flags = new FeatureFlags();
            flags.Set("Alpha", true);
            flags.Set("Beta", true);

            Require(flags.IsEnabled("alpha"), "Feature flags must preserve case-insensitive identity.");
            Require(flags.IsEnabled("BETA"), "Feature flags must preserve case-insensitive identity.");

            Require(!flags.IsEnabled(" Alpha "), "Whitespace-padded feature lookups must fail closed.");
            Require(!flags.IsEnabled("\tBeta\t"), "Control-whitespace feature lookups must fail closed.");

            AssertRejectedWithoutMutation(flags, "  Alpha  ");
            AssertRejectedWithoutMutation(flags, "\tBeta\t");
            AssertRejectedWithoutMutation(flags, "Alpha\nInjected");
            AssertRejectedWithoutMutation(flags, "Alpha\rInjected");
            AssertRejectedWithoutMutation(flags, "Alpha\tInjected");
            AssertRejectedWithoutMutation(flags, "Alpha\0Injected");

            Require(!flags.IsEnabled("Alpha\nInjected"), "Malformed control-character lookups must fail closed.");
            Require(flags.IsEnabled("ALPHA"), "Rejected identifiers must not mutate existing flags.");
            Require(flags.IsEnabled("BETA"), "Rejected identifiers must preserve unrelated flags.");

            var snapshot = flags.Snapshot();
            Require(snapshot.Count == 2, "Rejected identifiers must not enter the feature snapshot.");
            Require(snapshot.ContainsKey("Alpha") && snapshot.ContainsKey("Beta"), "Snapshot must preserve canonical feature identifiers.");
        }

        private static void AssertRejectedWithoutMutation(FeatureFlags flags, string name)
        {
            var before = flags.Snapshot().Count;
            try
            {
                flags.Set(name, true);
                throw new InvalidOperationException("FeatureFlags.Set accepted a non-canonical identifier.");
            }
            catch (ArgumentException)
            {
            }

            Require(flags.Snapshot().Count == before, "Rejected feature identifiers must not mutate state.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
