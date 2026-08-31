using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationRuleKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MoveNextInducedDriftFailsBeforeCurrent();
            CurrentInducedDriftFailsBeforeNullRuleAcceptance();
            StableCountedProfileSucceeds();
            StreamingProfileRemainsSupported();
        }

        private static void MoveNextInducedDriftFailsBeforeCurrent()
        {
            var source = new HostileRuleCollection(Rule(), DriftPoint.MoveNext);
            var error = Throws<InvalidOperationException>(() => new CoordinationRuleProfile("P-MOVENEXT", 1, source));
            Equal("Coordination rule profile known Count changed during traversal from 1 to 2.", error.Message);
            Equal(0, source.CurrentReads, "MoveNext drift must fail before Current is read");
        }

        private static void CurrentInducedDriftFailsBeforeNullRuleAcceptance()
        {
            var source = new HostileRuleCollection(null!, DriftPoint.Current);
            var error = Throws<InvalidOperationException>(() => new CoordinationRuleProfile("P-CURRENT", 1, source));
            Equal("Coordination rule profile known Count changed during traversal from 1 to 2.", error.Message);
            Equal(1, source.CurrentReads, "Current drift must be detected at the first Current read");
        }

        private static void StableCountedProfileSucceeds()
        {
            var source = new HostileRuleCollection(Rule(), DriftPoint.None);
            var profile = new CoordinationRuleProfile("P-STABLE-CURRENT", 1, source);
            Equal(1, profile.Rules.Count, "stable counted profile must retain its rule");
            Equal(1, source.CurrentReads, "stable counted profile must read Current once");
            Equal("R-STABLE", profile.Resolve("Pipe", "Beam")?.RuleId ?? string.Empty);
        }

        private static void StreamingProfileRemainsSupported()
        {
            var profile = new CoordinationRuleProfile("P-STREAM-CURRENT", 1, StreamRule());
            Equal(1, profile.Rules.Count, "streaming profile must retain its rule");
            Equal("R-STABLE", profile.Resolve("Beam", "Pipe")?.RuleId ?? string.Empty);
        }

        private static CoordinationRule Rule() =>
            new CoordinationRule("R-STABLE", 1, "Pipe", "Beam", CoordinationRuleKind.HardClash, "Error", 0d);

        private static IEnumerable<CoordinationRule> StreamRule()
        {
            yield return Rule();
        }

        private enum DriftPoint
        {
            None,
            MoveNext,
            Current
        }

        private sealed class HostileRuleCollection : IReadOnlyCollection<CoordinationRule>
        {
            private readonly CoordinationRule _rule;
            private readonly DriftPoint _driftPoint;
            private int _count = 1;

            internal HostileRuleCollection(CoordinationRule rule, DriftPoint driftPoint)
            {
                _rule = rule;
                _driftPoint = driftPoint;
            }

            public int Count => _count;
            internal int CurrentReads { get; private set; }
            public IEnumerator<CoordinationRule> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<CoordinationRule>
            {
                private readonly HostileRuleCollection _owner;
                private bool _moved;

                internal Enumerator(HostileRuleCollection owner) => _owner = owner;

                public CoordinationRule Current
                {
                    get
                    {
                        if (!_moved) throw new InvalidOperationException("Enumerator is not positioned.");
                        _owner.CurrentReads++;
                        if (_owner._driftPoint == DriftPoint.Current)
                            _owner._count = 2;
                        return _owner._rule;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_moved) return false;
                    _moved = true;
                    if (_owner._driftPoint == DriftPoint.MoveNext)
                        _owner._count = 2;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException(
                "CoordinationRuleKnownCountStabilitySmoke: expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string? message = null)
        {
            if (Equals(expected, actual)) return;
            throw new InvalidOperationException(
                "CoordinationRuleKnownCountStabilitySmoke: " + (message ?? "values differ") +
                ". Expected " + expected + ", got " + actual + ".");
        }
    }
}