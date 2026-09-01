using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportSelectionTransientCountSmoke
    {
        internal static void Run()
        {
            MoveNextCountDriftFailsBeforeCurrent();
            CurrentCountDriftFailsBeforeSelectionRetention();
            StableCountedSelectionUsesTraversalWideRebounds();
            PureStreamingSelectionRemainsSinglePass();
        }

        private static void MoveNextCountDriftFailsBeforeCurrent()
        {
            var project = ProjectWithElement("move-drift");
            var source = new TransientCountSelection("E1", driftAfterMoveNext: true, driftAfterCurrent: false);
            ExpectInvalidOperation(() => ProjectQuantityReportBuilder.Detail(project, source));
            if (source.CurrentReads != 0)
                throw new Exception("Quantity report selection must reject post-MoveNext Count drift before reading Current.");
        }

        private static void CurrentCountDriftFailsBeforeSelectionRetention()
        {
            var project = ProjectWithElement("current-drift");
            var source = new TransientCountSelection("E1", driftAfterMoveNext: false, driftAfterCurrent: true);
            ExpectInvalidOperation(() => ProjectQuantityReportBuilder.Detail(project, source));
            if (source.CurrentReads != 1)
                throw new Exception("Quantity report selection Current must be read exactly once before the post-Current Count rebound.");
        }

        private static void StableCountedSelectionUsesTraversalWideRebounds()
        {
            var project = ProjectWithElement("stable-counted");
            var source = new StableCountSelection("E1");
            var rows = ProjectQuantityReportBuilder.Detail(project, source);
            if (rows.Count != 1 || rows[0].ElementIds.Count != 1 || rows[0].ElementIds[0] != "E1")
                throw new Exception("Stable counted quantity report selection must remain accepted.");
            if (source.CountReads != 7)
                throw new Exception("Stable one-item quantity report selection must rebind Count at admission, around both MoveNext calls, after Current, and after traversal. Expected 7, got " + source.CountReads + ".");
            if (source.CurrentReads != 1)
                throw new Exception("Stable one-item quantity report selection must read Current exactly once.");
        }

        private static void PureStreamingSelectionRemainsSinglePass()
        {
            var project = ProjectWithElement("streaming");
            var source = new OnePassStream("E1");
            var rows = ProjectQuantityReportBuilder.Detail(project, source);
            if (rows.Count != 1 || rows[0].ElementIds.Count != 1)
                throw new Exception("Pure streaming quantity report selection must remain accepted.");
            if (source.EnumeratorRequests != 1 || source.CurrentReads != 1)
                throw new Exception("Pure streaming quantity report selection must remain one-pass with one Current read.");
        }

        private static ProjectState ProjectWithElement(string suffix)
        {
            var project = new ProjectState("quantity-selection-transient-" + suffix, "Quantity selection transient " + suffix);
            project.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("zone", "Zone"));
            var family = new ProjectFamily("family", "Family", ElementCategory.Slab);
            project.Families.Add(family);
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, family.Id, "floor", "zone"));
            return project;
        }

        private static void ExpectInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception("Expected InvalidOperationException for transient quantity report selection Count drift.");
        }

        private sealed class TransientCountSelection : IReadOnlyCollection<string>
        {
            private readonly string _item;
            private readonly bool _driftAfterMoveNext;
            private readonly bool _driftAfterCurrent;
            private int _count = 1;

            internal TransientCountSelection(string item, bool driftAfterMoveNext, bool driftAfterCurrent)
            {
                _item = item;
                _driftAfterMoveNext = driftAfterMoveNext;
                _driftAfterCurrent = driftAfterCurrent;
            }

            public int Count => _count;
            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly TransientCountSelection _owner;
                private int _state;

                internal Enumerator(TransientCountSelection owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._driftAfterMoveNext) _owner._count = 1;
                        if (_owner._driftAfterCurrent) _owner._count = 2;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_state == 0)
                    {
                        _state = 1;
                        if (_owner._driftAfterMoveNext) _owner._count = 2;
                        return true;
                    }

                    _owner._count = 1;
                    _state = 2;
                    return false;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StableCountSelection : IReadOnlyCollection<string>
        {
            private readonly string _item;
            internal StableCountSelection(string item) => _item = item;
            public int Count { get { CountReads++; return 1; } }
            internal int CountReads { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly StableCountSelection _owner;
                private int _state;
                internal Enumerator(StableCountSelection owner) => _owner = owner;
                public string Current { get { _owner.CurrentReads++; return _owner._item; } }
                object IEnumerator.Current => Current;
                public bool MoveNext() { if (_state++ == 0) return true; return false; }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class OnePassStream : IEnumerable<string>
        {
            private readonly string _item;
            internal OnePassStream(string item) => _item = item;
            internal int EnumeratorRequests { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequests++;
                if (EnumeratorRequests != 1) throw new InvalidOperationException("Streaming selection was enumerated more than once.");
                return new Enumerator(this);
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly OnePassStream _owner;
                private int _state;
                internal Enumerator(OnePassStream owner) => _owner = owner;
                public string Current { get { _owner.CurrentReads++; return _owner._item; } }
                object IEnumerator.Current => Current;
                public bool MoveNext() { if (_state++ == 0) return true; return false; }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
