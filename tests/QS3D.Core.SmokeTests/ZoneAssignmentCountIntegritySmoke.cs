using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ZoneAssignmentCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            KnownCountOverrunRejectsBeforeCurrentRead();
            ExactTraversalCountDriftFailsClosed();
            AcquisitionCountDriftRejectsBeforeMoveNext();
            MoveNextCountDriftRejectsBeforeCurrentRead();
            CurrentCountDriftRejectsBeforeMutation();
            StableCountedInputAssigns();
            StreamingInputRemainsAccepted();
            StreamingHardCapRejectsBeforeCurrentRead();
        }

        private static void KnownCountOverrunRejectsBeforeCurrentRead()
        {
            var project = CreateProject(out var zone, out var element);
            var source = new InstrumentedCountedTargets(new[] { element, element }, 1, 1, int.MaxValue);

            ThrowsContaining<InvalidOperationException>(
                () => ProjectZoneService.Assign(project, zone.Id, source),
                "known count does not match");

            Equal(2, source.MoveNextReads);
            Equal(1, source.CurrentReads);
            Equal(string.Empty, element.ZoneId);
        }

        private static void ExactTraversalCountDriftFailsClosed()
        {
            var project = CreateProject(out var zone, out var element);
            var source = new InstrumentedCountedTargets(new[] { element }, 1, 2, 8);

            ThrowsContaining<InvalidOperationException>(
                () => ProjectZoneService.Assign(project, zone.Id, source),
                "known count changed during enumeration");

            Require(source.CountReads >= 8, "Zone assignment must repeatedly rebound known Count through traversal.");
            Equal(2, source.MoveNextReads);
            Equal(1, source.CurrentReads);
            Equal(string.Empty, element.ZoneId);
        }

        private static void AcquisitionCountDriftRejectsBeforeMoveNext()
        {
            var project = CreateProject(out var zone, out var element);
            var source = new TransientCountTargets(element, DriftPoint.Acquisition);

            ThrowsContaining<InvalidOperationException>(
                () => ProjectZoneService.Assign(project, zone.Id, source),
                "known count changed during enumeration");

            Equal(1, source.GetEnumeratorReads);
            Equal(0, source.MoveNextReads);
            Equal(0, source.CurrentReads);
            Equal(string.Empty, element.ZoneId);
        }

        private static void MoveNextCountDriftRejectsBeforeCurrentRead()
        {
            var project = CreateProject(out var zone, out var element);
            var source = new TransientCountTargets(element, DriftPoint.MoveNext);

            ThrowsContaining<InvalidOperationException>(
                () => ProjectZoneService.Assign(project, zone.Id, source),
                "known count changed during enumeration");

            Equal(1, source.MoveNextReads);
            Equal(0, source.CurrentReads);
            Equal(string.Empty, element.ZoneId);
        }

        private static void CurrentCountDriftRejectsBeforeMutation()
        {
            var project = CreateProject(out var zone, out var element);
            var beforeVersion = project.ChangeVersion;
            var source = new TransientCountTargets(element, DriftPoint.Current);

            ThrowsContaining<InvalidOperationException>(
                () => ProjectZoneService.Assign(project, zone.Id, source),
                "known count changed during enumeration");

            Equal(1, source.MoveNextReads);
            Equal(1, source.CurrentReads);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(string.Empty, element.ZoneId);
        }

        private static void StableCountedInputAssigns()
        {
            var project = CreateProject(out var zone, out var element);
            var source = new InstrumentedCountedTargets(new[] { element }, 1, 1, int.MaxValue);

            Equal(1, ProjectZoneService.Assign(project, zone.Id, source));
            Equal(zone.Id, element.ZoneId);
            Require(source.CountReads >= 6, "Stable counted Zone assignment must be rebound throughout traversal.");
            Equal(2, source.MoveNextReads);
            Equal(1, source.CurrentReads);
        }

        private static void StreamingInputRemainsAccepted()
        {
            var project = CreateProject(out var zone, out var element);
            Equal(1, ProjectZoneService.Assign(project, zone.Id, Stream(element)));
            Equal(zone.Id, element.ZoneId);
        }

        private static void StreamingHardCapRejectsBeforeCurrentRead()
        {
            var project = CreateProject(out var zone, out var element);
            var source = new RepeatingStreamingTargets(element, 10001);

            ThrowsContaining<InvalidOperationException>(
                () => ProjectZoneService.Assign(project, zone.Id, source),
                "at most 10000 target entries");

            Equal(10001, source.MoveNextReads);
            Equal(10000, source.CurrentReads);
            Equal(string.Empty, element.ZoneId);
        }

        private static ProjectState CreateProject(out ZoneDefinition zone, out ProjectElement element)
        {
            var project = new ProjectState("P-ZONE-COUNT", "Zone Count integrity");
            zone = new ZoneDefinition("ZONE-1", "Zone 1");
            element = new ProjectElement("E-1", ElementCategory.Room);
            project.Zones.Add(zone);
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<ProjectElement> Stream(ProjectElement element)
        {
            yield return element;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void ThrowsContaining<T>(Action action, string expectedText) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new Exception("Expected exception containing '" + expectedText + "', got '" + ex.Message + "'.");
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private sealed class InstrumentedCountedTargets : IReadOnlyCollection<ProjectElement>
        {
            private readonly ProjectElement[] _items;
            private readonly int _initialCount;
            private readonly int _reboundCount;
            private readonly int _firstReboundRead;

            public InstrumentedCountedTargets(ProjectElement[] items, int initialCount, int reboundCount, int firstReboundRead)
            {
                _items = items;
                _initialCount = initialCount;
                _reboundCount = reboundCount;
                _firstReboundRead = firstReboundRead;
            }

            public int CountReads { get; private set; }
            public int MoveNextReads { get; private set; }
            public int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return CountReads < _firstReboundRead ? _initialCount : _reboundCount;
                }
            }

            public IEnumerator<ProjectElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly InstrumentedCountedTargets _owner;
                private int _index = -1;

                public Enumerator(InstrumentedCountedTargets owner) { _owner = owner; }

                public bool MoveNext()
                {
                    _owner.MoveNextReads++;
                    _index++;
                    return _index < _owner._items.Length;
                }

                public ProjectElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private enum DriftPoint
        {
            Acquisition,
            MoveNext,
            Current
        }

        private sealed class TransientCountTargets : IReadOnlyCollection<ProjectElement>
        {
            private readonly ProjectElement _element;
            private readonly DriftPoint _point;
            private bool _drift;

            public TransientCountTargets(ProjectElement element, DriftPoint point)
            {
                _element = element;
                _point = point;
            }

            public int Count => _drift ? 2 : 1;
            public int GetEnumeratorReads { get; private set; }
            public int MoveNextReads { get; private set; }
            public int CurrentReads { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                GetEnumeratorReads++;
                if (_point == DriftPoint.Acquisition) _drift = true;
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly TransientCountTargets _owner;
                private int _index = -1;

                public Enumerator(TransientCountTargets owner) { _owner = owner; }

                public bool MoveNext()
                {
                    _owner.MoveNextReads++;
                    if (_owner._point == DriftPoint.Acquisition) _owner._drift = false;
                    if (_owner._point == DriftPoint.MoveNext && _index < 0) _owner._drift = true;
                    if (_owner._point == DriftPoint.Current && _index >= 0) _owner._drift = false;
                    _index++;
                    return _index == 0;
                }

                public ProjectElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._point == DriftPoint.MoveNext) _owner._drift = false;
                        if (_owner._point == DriftPoint.Current) _owner._drift = true;
                        return _owner._element;
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class RepeatingStreamingTargets : IEnumerable<ProjectElement>
        {
            private readonly ProjectElement _element;
            private readonly int _count;

            public RepeatingStreamingTargets(ProjectElement element, int count)
            {
                _element = element;
                _count = count;
            }

            public int MoveNextReads { get; private set; }
            public int CurrentReads { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly RepeatingStreamingTargets _owner;
                private int _index = -1;

                public Enumerator(RepeatingStreamingTargets owner) { _owner = owner; }

                public bool MoveNext()
                {
                    _owner.MoveNextReads++;
                    _index++;
                    return _index < _owner._count;
                }

                public ProjectElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._element;
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}