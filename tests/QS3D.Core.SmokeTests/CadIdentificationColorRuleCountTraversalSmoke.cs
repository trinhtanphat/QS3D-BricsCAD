using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class CadIdentificationColorRuleCountTraversalSmoke
    {
        internal static void Run()
        {
            UnderEnumerationRejects();
            OverEnumerationRejects();
            ExactKnownCountRemainsAccepted();
            PureStreamingRemainsAccepted();
            NegativeKnownCountRejectsBeforeTraversal();
            ConflictingKnownCountsRejectBeforeTraversal();
            OversizedKnownCountRejectsBeforeTraversal();
            ExistingNullAndDuplicateValidationRemain();
        }

        private static void UnderEnumerationRejects()
        {
            var error = Capture<InvalidOperationException>(() =>
                CreateOptions(new ReportedCountCollection(2, Rule(1))));
            Contains("known Count does not match completed traversal cardinality", error.Message,
                "Under-enumerated color rules must reject a supported Count/traversal mismatch.");
        }

        private static void OverEnumerationRejects()
        {
            var error = Capture<InvalidOperationException>(() =>
                CreateOptions(new ReportedCountCollection(1, Rule(1), Rule(2))));
            Contains("known Count does not match completed traversal cardinality", error.Message,
                "Over-enumerated color rules must reject a supported Count/traversal mismatch.");
        }

        private static void ExactKnownCountRemainsAccepted()
        {
            var options = CreateOptions(new ReportedCountCollection(2, Rule(1), Rule(2)));
            Equal(2, options.ClassificationByColor.Count,
                "Exact supported Count/traversal agreement must remain accepted.");
        }

        private static void PureStreamingRemainsAccepted()
        {
            var options = CreateOptions(Stream(Rule(1), Rule(2)));
            Equal(2, options.ClassificationByColor.Count,
                "Pure streaming color-rule inputs without known Count evidence must remain accepted.");
        }

        private static void NegativeKnownCountRejectsBeforeTraversal()
        {
            var records = new ReportedCountCollection(-1, Rule(1));
            var error = Capture<InvalidOperationException>(() => CreateOptions(records));
            Contains("invalid negative known Count", error.Message,
                "Negative supported Count evidence must fail closed.");
            Equal(0, records.EnumerationCount,
                "Negative supported Count evidence must reject before traversing color rules.");
        }

        private static void ConflictingKnownCountsRejectBeforeTraversal()
        {
            var records = new ConflictingCountCollection();
            var error = Capture<InvalidOperationException>(() => CreateOptions(records));
            Contains("conflicting known Count values", error.Message,
                "Conflicting supported Count evidence must fail closed.");
            Equal(0, records.EnumerationCount,
                "Conflicting supported Count evidence must reject before traversing color rules.");
        }

        private static void OversizedKnownCountRejectsBeforeTraversal()
        {
            var records = new ReportedCountCollection(257, Rule(1));
            var error = Capture<InvalidOperationException>(() => CreateOptions(records));
            Contains("at most 256 entries", error.Message,
                "Known Count above the CAD color-index domain must fail closed.");
            Equal(0, records.EnumerationCount,
                "Oversized supported Count evidence must reject before traversing color rules.");
        }

        private static void ExistingNullAndDuplicateValidationRemain()
        {
            var nullError = Capture<ArgumentException>(() =>
                CreateOptions(new ReportedCountCollection(
                    1,
                    new IdentificationColorRule[] { null! })));
            Contains("null item", nullError.Message,
                "Existing null color-rule validation must remain active.");

            var duplicateError = Capture<ArgumentException>(() =>
                CreateOptions(new ReportedCountCollection(2, Rule(7), Rule(7))));
            Contains("Duplicate identification color index", duplicateError.Message,
                "Existing duplicate color-index validation must remain active.");
        }

        private static CadIdentificationOptions CreateOptions(IEnumerable<IdentificationColorRule> rules)
        {
            return new CadIdentificationOptions(selectByColor: true, colorRules: rules);
        }

        private static IdentificationColorRule Rule(int colorIndex)
        {
            return new IdentificationColorRule(colorIndex, "CLASS-" + colorIndex);
        }

        private static IEnumerable<IdentificationColorRule> Stream(params IdentificationColorRule[] rules)
        {
            for (var index = 0; index < rules.Length; index++)
                yield return rules[index];
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
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
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class ReportedCountCollection : IReadOnlyCollection<IdentificationColorRule>
        {
            private readonly IdentificationColorRule[] _items;

            internal ReportedCountCollection(int reportedCount, params IdentificationColorRule[] items)
            {
                Count = reportedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count { get; }
            public int EnumerationCount { get; private set; }

            public IEnumerator<IdentificationColorRule> GetEnumerator()
            {
                EnumerationCount++;
                for (var index = 0; index < _items.Length; index++)
                    yield return _items[index];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ConflictingCountCollection : ICollection<IdentificationColorRule>, IReadOnlyCollection<IdentificationColorRule>
        {
            int ICollection<IdentificationColorRule>.Count => 1;
            int IReadOnlyCollection<IdentificationColorRule>.Count => 2;
            public bool IsReadOnly => true;
            public int EnumerationCount { get; private set; }

            public IEnumerator<IdentificationColorRule> GetEnumerator()
            {
                EnumerationCount++;
                yield return Rule(1);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(IdentificationColorRule item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(IdentificationColorRule item) => false;
            public void CopyTo(IdentificationColorRule[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(IdentificationColorRule item) => throw new NotSupportedException();
        }
    }

    internal static class CadIdentificationColorRuleCountTraversalRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CadIdentificationColorRuleCountTraversalSmoke.Run();
        }
    }
}