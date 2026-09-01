using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationConstructorIntegritySmoke
    {
        internal static void Run()
        {
            Throws<ArgumentNullException>(() =>
                new RegenerationEngine(null!, Array.Empty<IElementRegenerator>()));

            Throws<ArgumentNullException>(() =>
                new RegenerationEngine(new DependencyGraph(), null!));

            Throws<ArgumentException>(() =>
                new RegenerationEngine(
                    new DependencyGraph(),
                    new IElementRegenerator[] { null! }));

            RejectZeroCountOverYieldBeforeCurrent();
            RejectTransientMoveNextCountDrift();
            RejectTransientCurrentCountDrift();
            RejectKnownCountUnderYield();
            AcceptStableCountedSource();
            AcceptPureStreamingSource();

            _ = new RegenerationEngine(
                new DependencyGraph(),
                Array.Empty<IElementRegenerator>());
        }

        private static void RejectZeroCountOverYieldBeforeCurrent()
        {
            var source = new HostileRegeneratorSource(0, DriftPoint.None, yieldItem: true, stopAfterFirst: true);
            Throws<InvalidOperationException>(() => new RegenerationEngine(new DependencyGraph(), source));
            if (source.CurrentReads != 0)
                throw new InvalidOperationException("Regenerator Count=0 over-yield must fail before unexpected Current.");
        }

        private static void RejectTransientMoveNextCountDrift()
        {
            var source = new HostileRegeneratorSource(1, DriftPoint.MoveNext, yieldItem: true, stopAfterFirst: true);
            Throws<InvalidOperationException>(() => new RegenerationEngine(new DependencyGraph(), source));
            if (source.CurrentReads != 0)
                throw new InvalidOperationException("Regenerator MoveNext Count drift must fail before Current.");
        }

        private static void RejectTransientCurrentCountDrift()
        {
            var source = new HostileRegeneratorSource(1, DriftPoint.Current, yieldItem: true, stopAfterFirst: true);
            Throws<InvalidOperationException>(() => new RegenerationEngine(new DependencyGraph(), source));
            if (source.CurrentReads != 1)
                throw new InvalidOperationException("Regenerator Current Count drift probe should read Current exactly once.");
        }

        private static void RejectKnownCountUnderYield()
        {
            var source = new HostileRegeneratorSource(1, DriftPoint.None, yieldItem: false, stopAfterFirst: true);
            Throws<InvalidOperationException>(() => new RegenerationEngine(new DependencyGraph(), source));
        }

        private static void AcceptStableCountedSource()
        {
            var source = new HostileRegeneratorSource(1, DriftPoint.None, yieldItem: true, stopAfterFirst: true);
            _ = new RegenerationEngine(new DependencyGraph(), source);
            if (source.CurrentReads != 1)
                throw new InvalidOperationException("Stable counted regenerator source should be consumed exactly once.");
        }

        private static void AcceptPureStreamingSource()
        {
            IEnumerable<IElementRegenerator> Source()
            {
                yield return new StubRegenerator();
            }
            _ = new RegenerationEngine(new DependencyGraph(), Source());
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private enum DriftPoint
        {
            None,
            MoveNext,
            Current
        }

        private sealed class HostileRegeneratorSource : IEnumerable<IElementRegenerator>, IReadOnlyCollection<IElementRegenerator>, ICollection
        {
            private readonly int _admittedCount;
            private readonly DriftPoint _driftPoint;
            private readonly bool _yieldItem;
            private readonly bool _stopAfterFirst;
            private int _reportedCount;

            public HostileRegeneratorSource(int admittedCount, DriftPoint driftPoint, bool yieldItem, bool stopAfterFirst)
            {
                _admittedCount = admittedCount;
                _reportedCount = admittedCount;
                _driftPoint = driftPoint;
                _yieldItem = yieldItem;
                _stopAfterFirst = stopAfterFirst;
            }

            public int Count => _reportedCount;
            int ICollection.Count => _reportedCount;
            public object SyncRoot => this;
            public bool IsSynchronized => false;
            public int CurrentReads { get; private set; }

            public IEnumerator<IElementRegenerator> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<IElementRegenerator>
            {
                private readonly HostileRegeneratorSource _owner;
                private int _state;

                public Enumerator(HostileRegeneratorSource owner) { _owner = owner; }

                public IElementRegenerator Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._driftPoint == DriftPoint.Current)
                            _owner._reportedCount = _owner._admittedCount + 1;
                        return new StubRegenerator();
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (!_owner._yieldItem || (_owner._stopAfterFirst && _state > 0))
                    {
                        _owner._reportedCount = _owner._admittedCount;
                        return false;
                    }
                    _state++;
                    if (_owner._driftPoint == DriftPoint.MoveNext)
                        _owner._reportedCount = _owner._admittedCount + 1;
                    else
                        _owner._reportedCount = _owner._admittedCount;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StubRegenerator : IElementRegenerator
        {
            public bool CanRegenerate(ElementCategory category) => false;
            public void Regenerate(ProjectState project, ProjectElement element) { }
        }
    }
}
