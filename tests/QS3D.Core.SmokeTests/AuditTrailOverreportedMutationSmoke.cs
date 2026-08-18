using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailOverreportedMutationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsOverreportedRecordBeforeMutation();
            RejectsOverreportedClearBeforeMutation();
        }

        private static void RejectsOverreportedRecordBeforeMutation()
        {
            var history = new OverreportedHistory(CanonicalEvent());
            var trail = BuildTrail(history);

            Throws<InvalidOperationException>(() => trail.Record("new.action", "E2", "detail"));

            Equal(1, history.EnumeratorRequests, "record enumeration requests");
            Equal(0, history.AddRequests, "record add requests");
            Equal(0, history.ClearRequests, "record clear requests");
            Equal(1, history.ActualCount, "record actual count");
        }

        private static void RejectsOverreportedClearBeforeMutation()
        {
            var history = new OverreportedHistory(CanonicalEvent());
            var trail = BuildTrail(history);

            Throws<InvalidOperationException>(() => trail.Clear());

            Equal(1, history.EnumeratorRequests, "clear enumeration requests");
            Equal(0, history.AddRequests, "clear add requests");
            Equal(0, history.ClearRequests, "clear mutation requests");
            Equal(1, history.ActualCount, "clear actual count");
        }

        private static AuditTrail BuildTrail(IList<AuditEvent> history)
        {
            var constructor = typeof(AuditTrail).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(IList<AuditEvent>), typeof(ProjectState) },
                modifiers: null);
            if (constructor == null)
                throw new Exception("AuditTrailOverreportedMutationSmoke could not resolve the bounded-history constructor.");
            return (AuditTrail)constructor.Invoke(new object?[] { history, null });
        }

        private static AuditEvent CanonicalEvent()
        {
            return new AuditEvent
            {
                Utc = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc),
                Action = "history.event",
                ElementId = "E1",
                Detail = "canonical"
            };
        }

        private sealed class OverreportedHistory : IList<AuditEvent>
        {
            private readonly List<AuditEvent> _items;

            internal OverreportedHistory(params AuditEvent[] items)
            {
                _items = new List<AuditEvent>(items);
            }

            internal int EnumeratorRequests { get; private set; }
            internal int AddRequests { get; private set; }
            internal int ClearRequests { get; private set; }
            internal int ActualCount => _items.Count;

            public int Count => _items.Count + 1;
            public bool IsReadOnly => false;
            public AuditEvent this[int index] { get => _items[index]; set => _items[index] = value; }

            public void Add(AuditEvent item)
            {
                AddRequests++;
                _items.Add(item);
            }

            public void Clear()
            {
                ClearRequests++;
                _items.Clear();
            }

            public bool Contains(AuditEvent item) => _items.Contains(item);
            public void CopyTo(AuditEvent[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public int IndexOf(AuditEvent item) => _items.IndexOf(item);
            public void Insert(int index, AuditEvent item) => _items.Insert(index, item);
            public bool Remove(AuditEvent item) => _items.Remove(item);
            public void RemoveAt(int index) => _items.RemoveAt(index);

            public IEnumerator<AuditEvent> GetEnumerator()
            {
                EnumeratorRequests++;
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("AuditTrailOverreportedMutationSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("AuditTrailOverreportedMutationSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
