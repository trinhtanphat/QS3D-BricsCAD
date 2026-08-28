using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepRecognitionSmoke
    {
        internal static void Run()
        {
            RecognitionInputBounds();
            RecognitionTextIntegrity();
            DefaultProfilePriorityAndCase();
            BlockNameRecognition();
            ExplicitPriority();
            AmbiguityFailsClosed();
            UnmatchedFailsClosed();
            QuantityKnownCountTraversalMismatchFailsClosed();
            QuantityKnownCountPreflightPrecedence();
            MepTbqProjectionSmoke.Run();
        }

        private static void RecognitionInputBounds()
        {
            var boundaryTokens = new List<string>(MepRecognitionLimits.MaxTokensPerRule);
            for (var i = 0; i < MepRecognitionLimits.MaxTokensPerRule; i++)
                boundaryTokens.Add("TOKEN-" + i);
            var boundaryRule = BuildingRule("boundary-token-rule", boundaryTokens);
            Equal(MepRecognitionLimits.MaxTokensPerRule, boundaryRule.Tokens.Count, "exact token boundary");

            var oversizedTokens = new List<string>(MepRecognitionLimits.MaxTokensPerRule + 1);
            for (var i = 0; i <= MepRecognitionLimits.MaxTokensPerRule; i++)
                oversizedTokens.Add("OVER-" + i);
            ArgumentContains(
                () => BuildingRule("oversized-token-rule", oversizedTokens),
                "at most 100",
                "101 recognition tokens");

            var duplicateInfiniteTokens = new InfiniteTokenEnumerable("DUPLICATE");
            ArgumentContains(
                () => BuildingRule("infinite-token-rule", duplicateInfiniteTokens),
                "at most 100",
                "infinite duplicate recognition tokens");
            True(
                duplicateInfiniteTokens.MoveNextCalls <= MepRecognitionLimits.MaxTokensPerRule + 1,
                "infinite token input must stop at the first disallowed element");

            var boundaryRules = new List<MepRecognitionRule>(MepRecognitionLimits.MaxRules);
            for (var i = 0; i < MepRecognitionLimits.MaxRules; i++)
                boundaryRules.Add(BuildingRule("boundary-rule-" + i, new[] { "R" + i }));
            var boundaryProfile = new MepRecognitionProfile(boundaryRules);
            Equal(MepRecognitionLimits.MaxRules, boundaryProfile.Rules.Count, "exact rule boundary");

            var oversizedRules = new List<MepRecognitionRule>(MepRecognitionLimits.MaxRules + 1);
            for (var i = 0; i <= MepRecognitionLimits.MaxRules; i++)
                oversizedRules.Add(BuildingRule("oversized-rule-" + i, new[] { "R" + i }));
            ArgumentContains(
                () => new MepRecognitionProfile(oversizedRules),
                "at most 500",
                "501 recognition rules");

            var infiniteRules = new InfiniteRuleEnumerable();
            ArgumentContains(
                () => new MepRecognitionProfile(infiniteRules),
                "at most 500",
                "infinite recognition rules");
            True(
                infiniteRules.MoveNextCalls <= MepRecognitionLimits.MaxRules + 1,
                "infinite rule input must stop at the first disallowed element");
        }

        private static void RecognitionTextIntegrity()
        {
            ArgumentContains(
                () => new MepRecognitionRule(
                    "bad-id-\ud800",
                    10,
                    MepRecognitionDiscipline.Structure,
                    "Structure",
                    new[] { "STRUCT" }),
                "well-formed UTF-16",
                "lone high surrogate recognition rule id");

            ArgumentContains(
                () => new MepRecognitionRule(
                    "bad-category",
                    10,
                    MepRecognitionDiscipline.Structure,
                    "Structure\udc00",
                    new[] { "STRUCT" }),
                "well-formed UTF-16",
                "lone low surrogate recognition category");

            ArgumentContains(
                () => new MepRecognitionRule(
                    "bad-token",
                    10,
                    MepRecognitionDiscipline.Structure,
                    "Structure",
                    new[] { "DUCT\ud800X" }),
                "well-formed UTF-16",
                "broken surrogate pair recognition token");

            var supplementary = char.ConvertFromUtf32(0x1F6A7);
            var rule = new MepRecognitionRule(
                "valid-" + supplementary,
                10,
                MepRecognitionDiscipline.Mep,
                "Pipe-" + supplementary,
                new[] { "PIPE-" + supplementary },
                MepRecognitionSource.Layer,
                MepElementKind.Pipe);
            Equal("valid-" + supplementary, rule.Id, "supplementary rule id preservation");
            Equal("Pipe-" + supplementary, rule.Category, "supplementary category preservation");
            Equal("PIPE-" + supplementary, rule.Tokens[0], "supplementary token preservation");

            var result = new MepRecognitionProfile(new[] { rule }).Recognize("SERVICE-PIPE-" + supplementary, null);
            Equal(MepRecognitionStatus.Matched, result.Status, "supplementary recognition status");
            Equal("Pipe-" + supplementary, result.Category, "supplementary recognition category");
            Equal("valid-" + supplementary, result.MatchedRuleIds[0], "supplementary recognition rule identity");
        }

        private static MepRecognitionRule BuildingRule(string id, IEnumerable<string> tokens) =>
            new MepRecognitionRule(
                id,
                1,
                MepRecognitionDiscipline.Structure,
                "Structure",
                tokens,
                MepRecognitionSource.LayerOrBlockName);

        private static void DefaultProfilePriorityAndCase()
        {
            var profile = MepRecognitionProfiles.CreateDefault();
            var cableTray = profile.Recognize("mEp_CaBlEtRaY_main", null);
            Equal(MepRecognitionStatus.Matched, cableTray.Status, "default cable tray status");
            Equal(MepRecognitionDiscipline.Mep, cableTray.Discipline!.Value, "default cable tray discipline");
            Equal(MepElementKind.CableTray, cableTray.MepKind!.Value, "cable tray must outrank embedded cable token");

            var beam = profile.Recognize("s-rc_beam_primary", null);
            Equal(MepRecognitionStatus.Matched, beam.Status, "default beam status");
            Equal(MepRecognitionDiscipline.Structure, beam.Discipline!.Value, "default beam discipline");
            Equal("Beam", beam.Category, "default beam category");
        }

        private static void BlockNameRecognition()
        {
            var result = MepRecognitionProfiles.CreateDefault().Recognize("0", "ahu-01");
            Equal(MepRecognitionStatus.Matched, result.Status, "block-name status");
            Equal(MepElementKind.Equipment, result.MepKind!.Value, "block-name equipment kind");
        }

        private static void ExplicitPriority()
        {
            var profile = new MepRecognitionProfile(new[]
            {
                new MepRecognitionRule("low", 10, MepRecognitionDiscipline.Mep, "Pipe", new[] { "PIPE" }, mepKind: MepElementKind.Pipe),
                new MepRecognitionRule("high", 20, MepRecognitionDiscipline.Mep, "Duct", new[] { "PIPE" }, mepKind: MepElementKind.Duct)
            });
            var result = profile.Recognize("pipe-main", null);
            Equal(MepRecognitionStatus.Matched, result.Status, "priority status");
            Equal(MepElementKind.Duct, result.MepKind!.Value, "higher priority rule");
            Equal(1, result.MatchedRuleIds.Count, "only highest-priority rules participate");
            Equal("high", result.MatchedRuleIds[0], "highest-priority rule id");
        }

        private static void AmbiguityFailsClosed()
        {
            var profile = new MepRecognitionProfile(new[]
            {
                new MepRecognitionRule("pipe", 50, MepRecognitionDiscipline.Mep, "Pipe", new[] { "SERVICE" }, mepKind: MepElementKind.Pipe),
                new MepRecognitionRule("duct", 50, MepRecognitionDiscipline.Mep, "Duct", new[] { "SERVICE" }, mepKind: MepElementKind.Duct)
            });
            var result = profile.Recognize("service-main", null);
            Equal(MepRecognitionStatus.Ambiguous, result.Status, "ambiguity status");
            True(!result.Discipline.HasValue, "ambiguous discipline must not be guessed");
            True(!result.MepKind.HasValue, "ambiguous MEP kind must not be guessed");
            Equal(2, result.MatchedRuleIds.Count, "ambiguous rule evidence count");
        }

        private static void UnmatchedFailsClosed()
        {
            var result = MepRecognitionProfiles.CreateDefault().Recognize("GENERIC-NOTES", "TITLEBLOCK");
            Equal(MepRecognitionStatus.Unmatched, result.Status, "unmatched status");
            True(!result.Discipline.HasValue, "unmatched discipline must remain empty");
            True(result.Category == null, "unmatched category must remain empty");
        }

        private static void QuantityKnownCountTraversalMismatchFailsClosed()
        {
            var service = new MepQuantityService();
            InvalidOperationContains(
                () => service.Aggregate(new ReportedCountCollection(
                    2,
                    new[] { QuantityElement("mep-count-short") })),
                "known count",
                "MEP quantity Count 2 -> yield 1");

            InvalidOperationContains(
                () => service.Aggregate(new ReportedCountCollection(
                    1,
                    new[] { QuantityElement("mep-count-long-1"), QuantityElement("mep-count-long-2") })),
                "known count",
                "MEP quantity Count 1 -> yield 2");
        }

        private static void QuantityKnownCountPreflightPrecedence()
        {
            var service = new MepQuantityService();

            var negative = new ReportedCountCollection(-1, new[] { QuantityElement("mep-negative") });
            InvalidOperationContains(
                () => service.Aggregate(negative),
                "negative known count",
                "negative MEP known Count");
            Equal(0, negative.EnumerationStarts, "negative known Count must fail before enumeration");

            var oversized = new ReportedCountCollection(10001, new[] { QuantityElement("mep-oversized") });
            InvalidOperationContains(
                () => service.Aggregate(oversized),
                "at most 10000",
                "oversized MEP known Count");
            Equal(0, oversized.EnumerationStarts, "oversized known Count must fail before enumeration");

            var conflicting = new ConflictingCountCollection(
                1,
                2,
                new[] { QuantityElement("mep-conflicting") });
            InvalidOperationContains(
                () => service.Aggregate(conflicting),
                "conflicting known counts",
                "conflicting MEP known Counts");
            Equal(0, conflicting.EnumerationStarts, "conflicting known Counts must fail before enumeration");

            var boundaryItems = new List<MepElement>(10001);
            for (var i = 0; i < 10001; i++)
                boundaryItems.Add(QuantityElement("mep-boundary-" + i));
            InvalidOperationContains(
                () => service.Aggregate(new ReportedCountCollection(10000, boundaryItems)),
                "at most 10000",
                "MEP traversal element 10001 must retain oversize precedence");
        }

        private static MepElement QuantityElement(string id)
        {
            return new MepElement(
                id,
                MepElementKind.Pipe,
                "CHW",
                "DN25",
                "L1",
                count: 1,
                lengthM: 1d);
        }

        private static void ArgumentContains(Action action, string expectedText, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                True(
                    ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0,
                    label + ": unexpected error message: " + ex.Message);
                return;
            }

            throw new InvalidOperationException(label + ": expected ArgumentException.");
        }

        private static void InvalidOperationContains(Action action, string expectedText, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                True(
                    ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0,
                    label + ": unexpected error message: " + ex.Message);
                return;
            }

            throw new InvalidOperationException(label + ": expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ".");
        }

        private sealed class InfiniteTokenEnumerable : IEnumerable<string>
        {
            private readonly string _value;
            internal InfiniteTokenEnumerable(string value) { _value = value; }
            internal int MoveNextCalls { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                while (true)
                {
                    MoveNextCalls++;
                    yield return _value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class InfiniteRuleEnumerable : IEnumerable<MepRecognitionRule>
        {
            internal int MoveNextCalls { get; private set; }

            public IEnumerator<MepRecognitionRule> GetEnumerator()
            {
                var index = 0;
                while (true)
                {
                    MoveNextCalls++;
                    yield return BuildingRule("infinite-rule-" + index, new[] { "R" + index });
                    index++;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ReportedCountCollection : ICollection<MepElement>
        {
            private readonly int _reportedCount;
            private readonly IReadOnlyList<MepElement> _items;

            internal ReportedCountCollection(int reportedCount, IReadOnlyList<MepElement> items)
            {
                _reportedCount = reportedCount;
                _items = items;
            }

            public int Count => _reportedCount;
            public bool IsReadOnly => true;
            internal int EnumerationStarts { get; private set; }

            public IEnumerator<MepElement> GetEnumerator()
            {
                EnumerationStarts++;
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(MepElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(MepElement item) => throw new NotSupportedException();
            public void CopyTo(MepElement[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(MepElement item) => throw new NotSupportedException();
        }

        private sealed class ConflictingCountCollection : ICollection<MepElement>, IReadOnlyCollection<MepElement>
        {
            private readonly int _collectionCount;
            private readonly int _readOnlyCount;
            private readonly IReadOnlyList<MepElement> _items;

            internal ConflictingCountCollection(
                int collectionCount,
                int readOnlyCount,
                IReadOnlyList<MepElement> items)
            {
                _collectionCount = collectionCount;
                _readOnlyCount = readOnlyCount;
                _items = items;
            }

            int ICollection<MepElement>.Count => _collectionCount;
            int IReadOnlyCollection<MepElement>.Count => _readOnlyCount;
            public bool IsReadOnly => true;
            internal int EnumerationStarts { get; private set; }

            public IEnumerator<MepElement> GetEnumerator()
            {
                EnumerationStarts++;
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(MepElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(MepElement item) => throw new NotSupportedException();
            public void CopyTo(MepElement[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(MepElement item) => throw new NotSupportedException();
        }
    }
}
