using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationRuleCollectionBoundSmoke
    {
        private const int MaximumEntries = 10000;

        internal static void Run()
        {
            CountedRuleOversizeFailsBeforeEnumeration();
            StreamingRuleOversizeStopsAtFirstDisallowedEntry();
            ExactRuleBoundaryIsAccepted();
            CountedProfileOversizeFailsBeforeEnumeration();
            StreamingProfileOversizeStopsAtFirstDisallowedEntry();
            ExactProfileBoundaryIsAccepted();
        }

        private static void CountedRuleOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<CoordinationRule>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() => new CoordinationRuleProfile("COUNTED", 1, source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted coordination rules must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted coordination-rule oversize must report the rule bound.");
        }

        private static void StreamingRuleOversizeStopsAtFirstDisallowedEntry()
        {
            var source = new StreamingRules(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() => new CoordinationRuleProfile("STREAM", 1, source));

            Equal(MaximumEntries + 1, source.YieldedCount,
                "Streaming coordination-rule ingestion must stop after observing rule 10,001.");
            Contains("at most 10000", error.Message, "Streaming coordination-rule oversize must report the rule bound.");
        }

        private static void ExactRuleBoundaryIsAccepted()
        {
            var rules = new CoordinationRule[MaximumEntries];
            for (var i = 0; i < rules.Length; i++)
                rules[i] = Rule(i);

            var profile = new CoordinationRuleProfile("BOUNDARY", 1, rules);
            Equal(MaximumEntries, profile.Rules.Count, "Coordination profile must accept exactly 10,000 rules.");
        }

        private static void CountedProfileOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<CoordinationRuleProfile>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() => new CoordinationRuleProfileCatalog(source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted coordination profiles must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted coordination-profile oversize must report the profile bound.");
        }

        private static void StreamingProfileOversizeStopsAtFirstDisallowedEntry()
        {
            var source = new StreamingProfiles(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() => new CoordinationRuleProfileCatalog(source));

            Equal(MaximumEntries + 1, source.YieldedCount,
                "Streaming coordination-profile ingestion must stop after observing profile 10,001.");
            Contains("at most 10000", error.Message, "Streaming coordination-profile oversize must report the profile bound.");
        }

        private static void ExactProfileBoundaryIsAccepted()
        {
            var profiles = new CoordinationRuleProfile[MaximumEntries];
            for (var i = 0; i < profiles.Length; i++)
                profiles[i] = Profile(i);

            var catalog = new CoordinationRuleProfileCatalog(profiles);
            Equal(MaximumEntries, catalog.Profiles.Count, "Coordination catalog must accept exactly 10,000 profile revisions.");
        }

        private static CoordinationRule Rule(int index)
        {
            return new CoordinationRule(
                "RULE-" + index.ToString("D5", CultureInfo.InvariantCulture),
                1,
                "Pipe",
                "Beam",
                CoordinationRuleKind.HardClash,
                "High",
                0d);
        }

        private static CoordinationRuleProfile Profile(int index)
        {
            return new CoordinationRuleProfile(
                "PROFILE-" + index.ToString("D5", CultureInfo.InvariantCulture),
                1,
                Array.Empty<CoordinationRule>());
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountedNeverEnumerated<T> : IReadOnlyCollection<T>
        {
            internal CountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingRules : IEnumerable<CoordinationRule>
        {
            private readonly int _count;

            internal StreamingRules(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<CoordinationRule> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return Rule(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingProfiles : IEnumerable<CoordinationRuleProfile>
        {
            private readonly int _count;

            internal StreamingProfiles(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<CoordinationRuleProfile> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return Profile(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class CoordinationRuleCollectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CoordinationRuleCollectionBoundSmoke.Run();
        }
    }
}
