using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepRecognitionCurrentIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            TokenLimitRejectsBeforeReadingOverrunCurrent();
            RuleLimitRejectsBeforeReadingOverrunCurrent();
            TokenKnownCountOverrunRejectsBeforeCurrent();
            RuleKnownCountOverrunRejectsBeforeCurrent();
            TokenKnownCountUnderYieldRejects();
            RuleKnownCountUnderYieldRejects();
            TokenTransientCountGrowthRejectsBeforeCurrent();
            RuleTransientCountShrinkRejectsBeforeCurrent();
            TokenTransientNegativeCountRejectsBeforeCurrent();
            ConflictingCountEvidenceRejectsBeforeEnumeration();
            StableCountedInputsRemainAccepted();
        }

        private static void TokenLimitRejectsBeforeReadingOverrunCurrent()
        {
            var source = new TokenLimitProbe();
            var error = Capture<ArgumentException>(() => new MepRecognitionRule(
                "current-integrity-token-limit",
                1,
                MepRecognitionDiscipline.Structure,
                "Structure",
                source,
                MepRecognitionSource.LayerOrBlockName));

            Contains("at most 100", error.Message,
                "MEP token limit must retain the existing bounded-input diagnostic.");
            Equal(MepRecognitionLimits.MaxTokensPerRule + 1, source.MoveNextCalls,
                "MEP token limit must observe exactly the first disallowed MoveNext.");
            Equal(MepRecognitionLimits.MaxTokensPerRule, source.CurrentReads,
                "MEP token limit must reject element 101 before reading caller Current.");
        }

        private static void RuleLimitRejectsBeforeReadingOverrunCurrent()
        {
            var source = new RuleLimitProbe();
            var error = Capture<ArgumentException>(() => new MepRecognitionProfile(source));

            Contains("at most 500", error.Message,
                "MEP rule limit must retain the existing bounded-input diagnostic.");
            Equal(MepRecognitionLimits.MaxRules + 1, source.MoveNextCalls,
                "MEP rule limit must observe exactly the first disallowed MoveNext.");
            Equal(MepRecognitionLimits.MaxRules, source.CurrentReads,
                "MEP rule limit must reject element 501 before reading caller Current.");
        }

        private static void TokenKnownCountOverrunRejectsBeforeCurrent()
        {
            var source = new CountProbe<string>(new[] { "A", "B" }, new[] { 1 });
            var error = Capture<ArgumentException>(() => Rule("token-overrun", source));
            Contains("reported known count was 1", error.Message, "Token Count overrun must fail closed.");
            Equal(2, source.MoveNextCalls, "Token Count overrun must observe the unexpected MoveNext.");
            Equal(1, source.CurrentReads, "Token Count overrun must reject before reading unexpected Current.");
        }

        private static void RuleKnownCountOverrunRejectsBeforeCurrent()
        {
            var source = new CountProbe<MepRecognitionRule>(
                new[] { Rule("rule-overrun-a", new[] { "A" }), Rule("rule-overrun-b", new[] { "B" }) },
                new[] { 1 });
            var error = Capture<ArgumentException>(() => new MepRecognitionProfile(source));
            Contains("reported known count was 1", error.Message, "Rule Count overrun must fail closed.");
            Equal(2, source.MoveNextCalls, "Rule Count overrun must observe the unexpected MoveNext.");
            Equal(1, source.CurrentReads, "Rule Count overrun must reject before reading unexpected Current.");
        }

        private static void TokenKnownCountUnderYieldRejects()
        {
            var source = new CountProbe<string>(new[] { "A" }, new[] { 2 });
            var error = Capture<ArgumentException>(() => Rule("token-under-yield", source));
            Contains("reported known count was 2", error.Message, "Token Count under-yield must fail closed.");
            Equal(1, source.CurrentReads, "Token under-yield may read only the yielded item.");
        }

        private static void RuleKnownCountUnderYieldRejects()
        {
            var source = new CountProbe<MepRecognitionRule>(
                new[] { Rule("rule-under-yield-a", new[] { "A" }) },
                new[] { 2 });
            var error = Capture<ArgumentException>(() => new MepRecognitionProfile(source));
            Contains("reported known count was 2", error.Message, "Rule Count under-yield must fail closed.");
            Equal(1, source.CurrentReads, "Rule under-yield may read only the yielded item.");
        }

        private static void TokenTransientCountGrowthRejectsBeforeCurrent()
        {
            var source = new CountProbe<string>(new[] { "A" }, new[] { 1, 1, 2 });
            var error = Capture<ArgumentException>(() => Rule("token-transient-growth", source));
            Contains("known count changed during traversal", error.Message, "Transient token Count growth must fail closed.");
            Equal(1, source.MoveNextCalls, "Transient token Count growth must be detected after first MoveNext.");
            Equal(0, source.CurrentReads, "Transient token Count growth must reject before Current.");
        }

        private static void RuleTransientCountShrinkRejectsBeforeCurrent()
        {
            var source = new CountProbe<MepRecognitionRule>(
                new[] { Rule("rule-transient-shrink-a", new[] { "A" }), Rule("rule-transient-shrink-b", new[] { "B" }) },
                new[] { 2, 2, 1 });
            var error = Capture<ArgumentException>(() => new MepRecognitionProfile(source));
            Contains("known count changed during traversal", error.Message, "Transient rule Count shrink must fail closed.");
            Equal(1, source.MoveNextCalls, "Transient rule Count shrink must be detected after first MoveNext.");
            Equal(0, source.CurrentReads, "Transient rule Count shrink must reject before Current.");
        }

        private static void TokenTransientNegativeCountRejectsBeforeCurrent()
        {
            var source = new CountProbe<string>(new[] { "A" }, new[] { 1, 1, -1 });
            var error = Capture<ArgumentException>(() => Rule("token-transient-negative", source));
            Contains("negative known count", error.Message, "Transient negative token Count must fail closed.");
            Equal(1, source.MoveNextCalls, "Transient negative token Count must be detected after first MoveNext.");
            Equal(0, source.CurrentReads, "Transient negative token Count must reject before Current.");
        }

        private static void ConflictingCountEvidenceRejectsBeforeEnumeration()
        {
            var source = new ConflictingCountProbe<string>(new[] { "A" }, 1, 2);
            var error = Capture<ArgumentException>(() => Rule("token-conflicting-count", source));
            Contains("conflicting known counts", error.Message, "Conflicting Count surfaces must fail closed at admission.");
            Equal(0, source.MoveNextCalls, "Conflicting Count surfaces must reject before enumeration.");
            Equal(0, source.CurrentReads, "Conflicting Count surfaces must reject before Current.");
        }

        private static void StableCountedInputsRemainAccepted()
        {
            var tokens = new CountProbe<string>(new[] { "PIPE", "PIPING" }, new[] { 2 });
            var rule = Rule("stable-counted-rule", tokens);
            Equal(2, rule.Tokens.Count, "Stable counted token input must preserve semantic snapshot.");

            var rules = new CountProbe<MepRecognitionRule>(new[] { rule }, new[] { 1 });
            var profile = new MepRecognitionProfile(rules);
            Equal(1, profile.Rules.Count, "Stable counted rule input must remain accepted.");
        }

        private static MepRecognitionRule Rule(string id, IEnumerable<string> tokens) =>
            new MepRecognitionRule(
                id,
                1,
                MepRecognitionDiscipline.Structure,
                "Structure",
                tokens,
                MepRecognitionSource.LayerOrBlockName);

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

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual=" + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountProbe<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly int[] _counts;
            private int _countReads;

            internal CountProbe(T[] items, int[] counts)
            {
                _items = items;
                _counts = counts;
            }

            public int Count
            {
                get
                {
                    var index = Math.Min(_countReads, _counts.Length - 1);
                    _countReads++;
                    return _counts[index];
                }
            }

            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly CountProbe<T> _owner;
                private int _index = -1;

                internal ProbeEnumerator(CountProbe<T> owner) => _owner = owner;

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class ConflictingCountProbe<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _collectionCount;
            private readonly int _readOnlyCount;

            internal ConflictingCountProbe(T[] items, int collectionCount, int readOnlyCount)
            {
                _items = items;
                _collectionCount = collectionCount;
                _readOnlyCount = readOnlyCount;
            }

            int ICollection<T>.Count => _collectionCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly ConflictingCountProbe<T> _owner;
                private int _index = -1;

                internal ProbeEnumerator(ConflictingCountProbe<T> owner) => _owner = owner;

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class TokenLimitProbe : IEnumerable<string>
        {
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<string>
            {
                private readonly TokenLimitProbe _owner;
                private int _index = -1;

                internal ProbeEnumerator(TokenLimitProbe owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return "DUPLICATE-TOKEN";
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index <= MepRecognitionLimits.MaxTokensPerRule;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class RuleLimitProbe : IEnumerable<MepRecognitionRule>
        {
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<MepRecognitionRule> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<MepRecognitionRule>
            {
                private readonly RuleLimitProbe _owner;
                private int _index = -1;

                internal ProbeEnumerator(RuleLimitProbe owner) => _owner = owner;

                public MepRecognitionRule Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return Rule("current-integrity-rule-" + _index, new[] { "RULE-" + _index });
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index <= MepRecognitionLimits.MaxRules;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}