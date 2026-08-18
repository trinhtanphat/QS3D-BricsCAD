using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class FeatureFlagsCanonicalNameSmoke
    {
        internal static void Run()
        {
            PaddedSetNamesAreRejected();
            PaddedLookupNamesDoNotAliasCanonicalFlags();
            EmbeddedControlsRemainRejected();
            CanonicalNamesRemainCaseInsensitive();
            SnapshotIsIsolatedAndCanonical();
        }

        private static void PaddedSetNamesAreRejected()
        {
            var flags = new FeatureFlags();

            Throws<ArgumentException>(() => flags.Set(" Feature.A ", true), "space-padded feature names must fail closed");
            Throws<ArgumentException>(() => flags.Set("\tFeature.A", true), "tab-padded feature names must fail closed");
            Throws<ArgumentException>(() => flags.Set("Feature.A\r\n", true), "line-padded feature names must fail closed");

            Equal(0, flags.Snapshot().Count, "rejected padded names must not mutate feature state");
        }

        private static void PaddedLookupNamesDoNotAliasCanonicalFlags()
        {
            var flags = new FeatureFlags();
            flags.Set("Feature.A", true);

            False(flags.IsEnabled(" Feature.A"), "leading-space lookup must not alias a canonical feature");
            False(flags.IsEnabled("Feature.A "), "trailing-space lookup must not alias a canonical feature");
            False(flags.IsEnabled("\tFeature.A"), "leading-tab lookup must not alias a canonical feature");
            False(flags.IsEnabled("Feature.A\n"), "trailing-newline lookup must not alias a canonical feature");
        }

        private static void EmbeddedControlsRemainRejected()
        {
            var flags = new FeatureFlags();

            Throws<ArgumentException>(() => flags.Set("Feature.\tA", true), "embedded controls must remain invalid");
            False(flags.IsEnabled("Feature.\tA"), "embedded-control lookup must remain disabled");
        }

        private static void CanonicalNamesRemainCaseInsensitive()
        {
            var flags = new FeatureFlags();
            flags.Set("Feature.A", true);

            True(flags.IsEnabled("feature.a"), "canonical feature lookup must remain case-insensitive");
            flags.Set("FEATURE.A", false);
            False(flags.IsEnabled("Feature.A"), "case variants must continue to address one canonical feature identity");
            Equal(1, flags.Snapshot().Count, "case variants must not create duplicate feature identities");
        }

        private static void SnapshotIsIsolatedAndCanonical()
        {
            var flags = new FeatureFlags();
            flags.Set("Feature.A", true);

            var snapshot = flags.Snapshot();
            True(snapshot.ContainsKey("Feature.A"), "snapshot must contain the canonical stored feature key");
            Throws<NotSupportedException>(
                () => ((IDictionary<string, bool>)snapshot).Add("Feature.B", true),
                "snapshot must remain read-only");

            flags.Set("Feature.B", true);
            Equal(1, snapshot.Count, "snapshot must remain isolated from later mutations");
        }

        private static void Throws<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void False(bool value, string message)
        {
            if (value) throw new InvalidOperationException(message);
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + $" Expected {expected}, got {actual}.");
        }
    }

    internal static class FeatureFlagsCanonicalNameRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            FeatureFlagsCanonicalNameSmoke.Run();
        }
    }
}
