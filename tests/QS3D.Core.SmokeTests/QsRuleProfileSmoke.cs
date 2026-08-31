using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class QsRuleProfileSmoke
    {
        private const int MaximumRules = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            DeterministicDetachedProfile();
            ResolvesStrictlyByHealthCode();
            RejectsAmbiguousOrMalformedProfiles();
            RejectsKnownOverBoundBeforeEnumeration();
            RejectsStreamingOverBoundBeforeUnexpectedCurrent();
            RejectsTransientCurrentCountDrift();
        }

        private static void DeterministicDetachedProfile()
        {
            var warning = new QsRuleDefinition("QSC.B", "MISSING_MATERIAL", HealthSeverity.Warning, "Material is required.");
            var error = new QsRuleDefinition("QSC.A", "ORPHAN_HANDLE", HealthSeverity.Error, "Source CAD object is missing.");
            var input = new List<QsRuleDefinition> { warning, error };
            var profile = new QsRuleProfile("PROFILE.DEFAULT", input);

            Equal("PROFILE.DEFAULT", profile.ProfileId, "profile id");
            Equal(2, profile.Rules.Count, "rule count");
            Equal("QSC.A", profile.Rules[0].RuleId, "first deterministic rule");
            Equal("QSC.B", profile.Rules[1].RuleId, "second deterministic rule");

            input.Clear();
            Equal(2, profile.Rules.Count, "profile must be detached from caller collection");

            var mutableView = profile.Rules as IList<QsRuleDefinition>;
            if (mutableView == null)
                throw new InvalidOperationException("Rules must expose a read-only list view.");
            Throws<NotSupportedException>(() => mutableView[0] = warning);
        }

        private static void ResolvesStrictlyByHealthCode()
        {
            var rule = new QsRuleDefinition("QSC.ORPHAN", "ORPHAN_HANDLE", HealthSeverity.Error, "Source CAD object is missing.");
            var profile = new QsRuleProfile("PROFILE.RESOLUTION", new[] { rule });

            var issue = new ModelHealthIssue("ORPHAN_HANDLE", HealthSeverity.Info, "Different runtime message.", "E-1");
            if (!profile.TryResolve(issue, out var resolved) || !ReferenceEquals(rule, resolved))
                throw new InvalidOperationException("Existing health issue code must resolve to its declarative rule metadata.");
            if (!ReferenceEquals(rule, profile.Resolve(issue)))
                throw new InvalidOperationException("Resolve must use the same code-only mapping as TryResolve.");

            var caseVariant = new ModelHealthIssue("orphan_handle", HealthSeverity.Warning, "Case variant code.");
            if (!ReferenceEquals(rule, profile.Resolve(caseVariant)))
                throw new InvalidOperationException("Health issue code identity must use the profile's canonical case-insensitive identity contract.");

            var unmapped = new ModelHealthIssue("UNMAPPED_HEALTH_CODE", HealthSeverity.Error, "Not configured.");
            if (profile.TryResolve(unmapped, out var unexpected) || unexpected != null || profile.Resolve(unmapped) != null)
                throw new InvalidOperationException("Unmapped health issues must remain explicitly unmapped.");

            Throws<ArgumentNullException>(() => profile.Resolve(null!));
        }

        private static void RejectsAmbiguousOrMalformedProfiles()
        {
            var first = new QsRuleDefinition("QSC.ONE", "CODE_ONE", HealthSeverity.Info, "First rule.");
            var duplicateId = new QsRuleDefinition("qsc.one", "CODE_TWO", HealthSeverity.Warning, "Duplicate id.");
            Throws<ArgumentException>(() => new QsRuleProfile("PROFILE.DUP-ID", new[] { first, duplicateId }));

            var duplicateCode = new QsRuleDefinition("QSC.TWO", "code_one", HealthSeverity.Error, "Duplicate health code.");
            Throws<ArgumentException>(() => new QsRuleProfile("PROFILE.DUP-CODE", new[] { first, duplicateCode }));

            Throws<ArgumentNullException>(() => new QsRuleProfile("PROFILE.NULL", null!));
            Throws<ArgumentException>(() => new QsRuleProfile("PROFILE.NULL-RULE", new QsRuleDefinition[1]));
            Throws<ArgumentException>(() => new QsRuleProfile(" ", new[] { first }));
            Throws<ArgumentException>(() => new QsRuleProfile(" PROFILE.PADDED ", new[] { first }));
            Throws<ArgumentException>(() => new QsRuleProfile("PROFILE BAD", new[] { first }));

            Throws<ArgumentException>(() => new QsRuleDefinition(" ", "CODE", HealthSeverity.Info, "Explanation."));
            Throws<ArgumentException>(() => new QsRuleDefinition(" QSC.PADDED ", "CODE", HealthSeverity.Info, "Explanation."));
            Throws<ArgumentException>(() => new QsRuleDefinition("QSC.PADDED", " CODE ", HealthSeverity.Info, "Explanation."));
            Throws<ArgumentException>(() => new QsRuleDefinition("QSC.BAD ID", "CODE", HealthSeverity.Info, "Explanation."));
            Throws<ArgumentException>(() => new QsRuleDefinition("QSC.BAD", "CODE/BAD", HealthSeverity.Info, "Explanation."));
            Throws<ArgumentOutOfRangeException>(() => new QsRuleDefinition("QSC.BAD", "CODE", (HealthSeverity)999, "Explanation."));
            Throws<ArgumentException>(() => new QsRuleDefinition("QSC.BAD", "CODE", HealthSeverity.Info, " "));
            Throws<ArgumentException>(() => new QsRuleDefinition("QSC.BAD", "CODE", HealthSeverity.Info, "Bad\nexplanation"));
        }

        private static void RejectsKnownOverBoundBeforeEnumeration()
        {
            var source = new KnownCountRules(MaximumRules + 1);
            Throws<InvalidOperationException>(() => new QsRuleProfile("PROFILE.KNOWN-BOUND", source));
            Equal(0, source.GetEnumeratorCalls, "known over-bound GetEnumerator calls");
        }

        private static void RejectsStreamingOverBoundBeforeUnexpectedCurrent()
        {
            var source = new StreamingRules(MaximumRules + 1);
            Throws<InvalidOperationException>(() => new QsRuleProfile("PROFILE.STREAM-BOUND", source));
            Equal(MaximumRules + 1, source.MoveNextCalls, "streaming over-bound MoveNext calls");
            Equal(MaximumRules, source.CurrentReads, "streaming over-bound Current reads");
        }

        private static void RejectsTransientCurrentCountDrift()
        {
            var source = new CurrentCountDriftRules();
            Throws<InvalidOperationException>(() => new QsRuleProfile("PROFILE.CURRENT-DRIFT", source));
            Equal(1, source.CurrentReads, "Current drift Current reads");
        }

        private static QsRuleDefinition Rule(int index) =>
            new QsRuleDefinition("QSC.R" + index, "CODE_" + index, HealthSeverity.Info, "Rule " + index + ".");

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
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

            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }

        private sealed class KnownCountRules : IReadOnlyCollection<QsRuleDefinition>
        {
            internal KnownCountRules(int count) => Count = count;
            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }
            public IEnumerator<QsRuleDefinition> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return ((IEnumerable<QsRuleDefinition>)Array.Empty<QsRuleDefinition>()).GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingRules : IEnumerable<QsRuleDefinition>
        {
            private readonly int _count;
            internal StreamingRules(int count) => _count = count;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<QsRuleDefinition> GetEnumerator() => new Enumerator(this, _count);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<QsRuleDefinition>
            {
                private readonly StreamingRules _owner;
                private readonly int _count;
                private int _index = -1;
                internal Enumerator(StreamingRules owner, int count) { _owner = owner; _count = count; }
                public bool MoveNext() { _owner.MoveNextCalls++; _index++; return _index < _count; }
                public QsRuleDefinition Current { get { _owner.CurrentReads++; return Rule(_index); } }
                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class CurrentCountDriftRules : IReadOnlyCollection<QsRuleDefinition>
        {
            private bool _driftNextCount;
            internal int CurrentReads { get; private set; }
            public int Count
            {
                get
                {
                    if (!_driftNextCount) return 1;
                    _driftNextCount = false;
                    return 2;
                }
            }
            public IEnumerator<QsRuleDefinition> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<QsRuleDefinition>
            {
                private readonly CurrentCountDriftRules _owner;
                private bool _moved;
                internal Enumerator(CurrentCountDriftRules owner) => _owner = owner;
                public bool MoveNext() { if (_moved) return false; _moved = true; return true; }
                public QsRuleDefinition Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._driftNextCount = true;
                        return Rule(1);
                    }
                }
                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
