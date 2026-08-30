using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailCurrentCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            EventsCurrentCountDriftPreemptsMalformedEventValidation();
            ClearCurrentCountDriftPreemptsMalformedEventValidation();
        }

        private static void EventsCurrentCountDriftPreemptsMalformedEventValidation()
        {
            var source = new CurrentDriftAuditList(MalformedEvent(), admittedCount: 1, driftedCount: 2);
            var trail = CreateTrail(source);

            ThrowsContaining(
                () => _ = trail.Events,
                "event count does not match stored history traversal");

            Equal(1, source.MoveNextCalls, "Events MoveNext calls");
            Equal(1, source.CurrentReads, "Events Current reads");
            Equal(0, source.ClearCalls, "Events source clear calls");
        }

        private static void ClearCurrentCountDriftPreemptsMalformedEventValidation()
        {
            var source = new CurrentDriftAuditList(MalformedEvent(), admittedCount: 1, driftedCount: 2);
            var trail = CreateTrail(source);

            ThrowsContaining(
                trail.Clear,
                "event count does not match stored history traversal");

            Equal(1, source.MoveNextCalls, "Clear MoveNext calls");
            Equal(1, source.CurrentReads, "Clear Current reads");
            Equal(0, source.ClearCalls, "Clear source clear calls");
        }

        private static AuditEvent MalformedEvent() => new AuditEvent
        {
            Utc = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified),
            Action = string.Empty,
            ElementId = string.Empty,
            Detail = string.Empty,
            Actor = string.Empty,
            CorrelationId = string.Empty
        };

        private static AuditTrail CreateTrail(IList<AuditEvent> source)
        {
            foreach (var constructor in typeof(AuditTrail).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(IList<AuditEvent>))
                    return (AuditTrail)constructor.Invoke(new object?[] { source, null });
            }

            throw new InvalidOperationException("AuditTrail private history constructor was not found.");
        }

        private static void ThrowsContaining(Action action, string token)
        {
            try
            {
                action();
            }
            catch (Exception ex) when (ex.Message.Contains(token, StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException("Expected exception containing: " + token);
        }

        private static void Equal(int expected, int actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + " expected " + expected + " but got " + actual + ".");
        }

        private sealed class CurrentDriftAuditList : IList<AuditEvent>
        {
            private readonly AuditEvent _item;
            private readonly int _driftedCount;
            private int _count;

            internal CurrentDriftAuditList(AuditEvent item, int admittedCount, int driftedCount)
            {
                _item = item;
                _count = admittedCount;
                _driftedCount = driftedCount;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            internal int ClearCalls { get; private set; }

            public int Count => _count;
            public bool IsReadOnly => false;

            public AuditEvent this[int index]
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public IEnumerator<AuditEvent> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Clear()
            {
                ClearCalls++;
                throw new InvalidOperationException("Audit source mutation must not be reached after Count drift.");
            }

            public void Add(AuditEvent item) => throw new NotSupportedException();
            public bool Remove(AuditEvent item) => throw new NotSupportedException();
            public void Insert(int index, AuditEvent item) => throw new NotSupportedException();
            public void RemoveAt(int index) => throw new NotSupportedException();
            public bool Contains(AuditEvent item) => ReferenceEquals(_item, item);
            public int IndexOf(AuditEvent item) => ReferenceEquals(_item, item) ? 0 : -1;
            public void CopyTo(AuditEvent[] array, int arrayIndex) => array[arrayIndex] = _item;

            private sealed class Enumerator : IEnumerator<AuditEvent>
            {
                private readonly CurrentDriftAuditList _owner;
                private bool _moved;

                internal Enumerator(CurrentDriftAuditList owner) => _owner = owner;

                public AuditEvent Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._count = _owner._driftedCount;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
