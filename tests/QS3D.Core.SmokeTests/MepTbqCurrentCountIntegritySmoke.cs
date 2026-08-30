using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepTbqCurrentCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CurrentInducedDriftWinsBeforeGroupValidation();
            StableCountedCurrentSucceeds();
        }

        private static void CurrentInducedDriftWinsBeforeGroupValidation()
        {
            var source = new CurrentDriftGroups(null!, driftOnCurrent: true);
            var error = Throws<InvalidOperationException>(() => new MepTbqProjectionService().BuildReport(source));
            Equal("MEP/TBQ report source Count changed during enumeration.", error.Message);
            Equal(1, source.CurrentReads);
        }

        private static void StableCountedCurrentSucceeds()
        {
            var source = new CurrentDriftGroups(Group(), driftOnCurrent: false);
            var report = new MepTbqProjectionService().BuildReport(source);
            Equal(1, report.Count);
            Equal(1, source.CurrentReads);
            Equal("CHW", report[0].System);
        }

        private static MepQuantityGroup Group() =>
            new MepQuantityService().Aggregate(new[]
            {
                new MepElement("E-CURRENT", MepElementKind.Pipe, "CHW", "DN50", "L1", lengthM: 1d)
            })[0];

        private sealed class CurrentDriftGroups : IReadOnlyCollection<MepQuantityGroup>
        {
            private readonly MepQuantityGroup _group;
            private readonly bool _driftOnCurrent;
            private int _count = 1;

            internal CurrentDriftGroups(MepQuantityGroup group, bool driftOnCurrent)
            {
                _group = group;
                _driftOnCurrent = driftOnCurrent;
            }

            public int Count => _count;
            internal int CurrentReads { get; private set; }
            public IEnumerator<MepQuantityGroup> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<MepQuantityGroup>
            {
                private readonly CurrentDriftGroups _owner;
                private bool _moved;
                internal Enumerator(CurrentDriftGroups owner) => _owner = owner;

                public MepQuantityGroup Current
                {
                    get
                    {
                        if (!_moved) throw new InvalidOperationException("Enumerator is not positioned.");
                        _owner.CurrentReads++;
                        if (_owner._driftOnCurrent) _owner._count = 2;
                        return _owner._group;
                    }
                }

                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException error) { return error; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("MepTbqCurrentCountIntegritySmoke expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
