using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailTransientCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            EventsRejectsTransientGrowthBeforeCurrent();
            RecordRejectsTransientShrinkBeforeCurrentOrMutation();
            ClearRejectsTransientNegativeCountBeforeCurrentOrMutation();
            StableHistoryRemainsReadableAndMutable();
        }

        private static void EventsRejectsTransientGrowthBeforeCurrent()
        {
            var history = new TransientCountHistory(transientCount: 2);
            var trail = BuildTrail(history);

            ThrowsCountMismatch(() => _ = trail.Events, "Events transient growth");
            Equal(1, history.MoveNextCalls, "Events MoveNext calls");
            Equal(0, history.CurrentReads, "Events Current reads");
        }

        private static void RecordRejectsTransientShrinkBeforeCurrentOrMutation()
        {
            var history = new TransientCountHistory(transientCount: 0);
            var trail = BuildTrail(history);

            ThrowsCountMismatch(() => trail.Record("audit.record", "E2", "detail"), "Record transient shrink");
            Equal(1, history.MoveNextCalls, "Record MoveNext calls");
            Equal(0, history.CurrentReads, "Record Current reads");
            Equal(0, history.AddCalls, "Record Add calls");
        }

        private static void ClearRejectsTransientNegativeCountBeforeCurrentOrMutation()
        {
            var history = new TransientCountHistory(transientCount: -1);
            var trail = BuildTrail(history);

            try
            {
                trail.Clear();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("invalid negative event count", StringComparison.Ordinal) < 0)
                    throw new Exception("AuditTrailTransientCountStabilitySmoke Clear returned wrong failure: " + ex.Message, ex);

                Equal(1, history.MoveNextCalls, "Clear MoveNext calls");
                Equal(0, history.CurrentReads, "Clear Current reads");
                Equal(0, history.ClearCalls, "Clear mutation calls");
                return;
            }

            throw new Exception("AuditTrailTransientCountStabilitySmoke Clear expected InvalidOperationException.");
        }

        private static void StableHistoryRemainsReadableAndMutable()
        {
            var history = new List<AuditEvent>
            {
                Event("seed", "E1")
            };
            var trail = BuildTrail(history);

            var snapshot = trail.Events;
            Equal(1, snapshot.Count, "stable snapshot count");
            Equal("seed", snapshot[0].Action, "stable snapshot action");

            trail.Record("added", "E2", "stable");
            Equal(2, history.Count, "stable record count");

            trail.Clear();
            Equal(0, history.Count, "stable clear count");
        }

        private static AuditEvent Event(string action, string elementId)
            => new AuditEvent
            {
                Utc = new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
                Action = action,
                ElementId = elementId,
                Detail = "detail"
            };

        private static AuditTrail BuildTrail(IList<AuditEvent> history)
        {
            var constructor = typeof(AuditTrail).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(IList<AuditEvent>), typeof(ProjectState) },
                modifiers: null);
            if (constructor == null)
                throw new Exception("AuditTrailTransientCountStabilitySmoke could not resolve the bounded-history constructor.");

            return (AuditTrail)constructor.Invoke(new object?[] { history, null });
        }

        private static void ThrowsCountMismatch(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("event count does not match stored history traversal", StringComparison.Ordinal) >= 0)
                    return;
                throw new Exception("AuditTrailTransientCountStabilitySmoke " + label + " returned wrong failure: " + ex.Message, ex);
            }

            throw new Exception("AuditTrailTransientCountStabilitySmoke " + label + " expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception("AuditTrailTransientCountStabilitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class TransientCountHistory : IList<AuditEvent>
        {
            private readonly AuditEvent _event = Event("stable", "E1");
            private readonly int _transientCount;
            private int _reportedCount = 1;

            internal TransientCountHistory(int transientCount)
            {
                _transientCount = transientCount;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            internal int AddCalls { get; private set; }
            internal int ClearCalls { get; private set; }

            public int Count => _reportedCount;
            public bool IsReadOnly => false;
            public AuditEvent this[int index]
            {
                get => index == 0 ? _event : throw new ArgumentOutOfRangeException(nameof(index));
                set => throw new NotSupportedException();
            }

            public void Add(AuditEvent item)
            {
                AddCalls++;
                throw new InvalidOperationException("Unexpected Add after transient Count drift.");
            }

            public void Clear()
            {
                ClearCalls++;
                throw new InvalidOperationException("Unexpected Clear after transient Count drift.");
            }

            public bool Contains(AuditEvent item) => ReferenceEquals(item, _event);
            public void CopyTo(AuditEvent[] array, int arrayIndex) => throw new NotSupportedException();
            public int IndexOf(AuditEvent item) => ReferenceEquals(item, _event) ? 0 : -1;
            public void Insert(int index, AuditEvent item) => throw new NotSupportedException();
            public bool Remove(AuditEvent item) => throw new NotSupportedException();
            public void RemoveAt(int index) => throw new NotSupportedException();

            public IEnumerator<AuditEvent> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<AuditEvent>
            {
                private readonly TransientCountHistory _owner;
                private bool _moved;

                internal Enumerator(TransientCountHistory owner)
                {
                    _owner = owner;
                }

                public AuditEvent Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._reportedCount = 1;
                        return _owner._event;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved)
                    {
                        _owner._reportedCount = 1;
                        return false;
                    }

                    _moved = true;
                    _owner._reportedCount = _owner._transientCount;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
