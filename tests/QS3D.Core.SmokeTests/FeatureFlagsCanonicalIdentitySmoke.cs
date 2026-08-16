using System;
using System.Collections.Generic;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class FeatureFlagsCanonicalIdentitySmoke
    {
        internal static void Run()
        {
            TrimmingAndCaseInsensitiveIdentityRemainCompatible();
            ControlCharacterNamesFailClosedBeforeMutation();
            MalformedLookupsDoNotResolveStoredFlags();
            SnapshotIsDetachedAndReadOnly();
        }

        private static void TrimmingAndCaseInsensitiveIdentityRemainCompatible()
        {
            var flags = new FeatureFlags();
            flags.Set("  QuantityReview  ", true);

            Require(flags.IsEnabled("quantityreview"), "Feature lookup lost case-insensitive identity semantics.");
            Require(flags.IsEnabled("  QUANTITYREVIEW  "), "Feature lookup lost outer-whitespace normalization.");

            flags.Set("quantityreview", false);
            Require(!flags.IsEnabled("QuantityReview"), "Feature disable did not update the canonical existing key.");
            Require(flags.Snapshot().Count == 1, "Case-insensitive update created a duplicate feature key.");
        }

        private static void ControlCharacterNamesFailClosedBeforeMutation()
        {
            var flags = new FeatureFlags();
            flags.Set("Stable", true);
            var before = flags.Snapshot();

            Throws<ArgumentException>(() => flags.Set("Bad\nName", true));
            Throws<ArgumentException>(() => flags.Set("Bad\rName", true));
            Throws<ArgumentException>(() => flags.Set("Bad\tName", true));
            Throws<ArgumentException>(() => flags.Set("Bad\0Name", true));

            var after = flags.Snapshot();
            Require(after.Count == before.Count, "Rejected feature-name mutation changed the flag count.");
            Require(after.ContainsKey("Stable") && after["Stable"], "Rejected feature-name mutation changed existing state.");
        }

        private static void MalformedLookupsDoNotResolveStoredFlags()
        {
            var flags = new FeatureFlags();
            flags.Set("Stable", true);

            Require(!flags.IsEnabled("Sta\nble"), "Control-character lookup unexpectedly resolved a feature.");
            Require(!flags.IsEnabled("Sta\tble"), "Tab-containing lookup unexpectedly resolved a feature.");
            Require(!flags.IsEnabled(null!), "Null feature lookup unexpectedly resolved a feature.");
            Require(!flags.IsEnabled("   "), "Whitespace feature lookup unexpectedly resolved a feature.");
        }

        private static void SnapshotIsDetachedAndReadOnly()
        {
            var flags = new FeatureFlags();
            flags.Set("Alpha", true);
            var snapshot = flags.Snapshot();

            flags.Set("Alpha", false);
            flags.Set("Beta", true);

            Require(snapshot.Count == 1, "Feature snapshot changed after later flag mutations.");
            Require(snapshot["Alpha"], "Feature snapshot did not preserve the captured value.");

            var mutableView = snapshot as IDictionary<string, bool>;
            Require(mutableView != null, "Feature snapshot no longer exposes the expected read-only dictionary contract.");
            Throws<NotSupportedException>(() => mutableView!["Injected"] = true);
            Require(!flags.IsEnabled("Injected"), "Mutating a snapshot unexpectedly changed live feature state.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
