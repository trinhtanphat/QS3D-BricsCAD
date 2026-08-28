using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailKnownCountOverrunSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ReadRejectsOverrunBeforeUnexpectedEventValidation();
            RecordRejectsOverrunBeforeUnexpectedEventValidation();
            ClearRejectsOverrunBeforeUnexpectedEventValidation();
        }

        private static void ReadRejectsOverrunBeforeUnexpectedEventValidation()
        {
            var history = new UnderreportedInvalidHistory();
            var trail = BuildTrail(history);

            ThrowsCountMismatch(() => _ = trail.Events, "read");

            Equal(1, history.EnumeratorRequests, "read enumerator requests");
            Equal(0, history.AddRequests, "read add requests");
            Equal(0, history.ClearRequests, "read clear requests");
        }

        private static void RecordRejectsOverrunBeforeUnexpectedEventValidation()
        {
            var history = new UnderreportedInvalidHistory();
            var trail = BuildTrail(history);

            ThrowsCountMismatch(() => trail.Record("new.action", "E2", "detail"), "record");

            Equal(1, history.EnumeratorRequests, "record enumerator requests");
            Equal(0, history.AddRequests, "record add requests");
            Equal(0, history.ClearRequests, "record clear requests");
        }

        private static void ClearRejectsOverrunBeforeUnexpectedEventValidation()
        {
            var history = new UnderreportedInvalidHistory();
            var trail = BuildTrail(history);

            ThrowsCountMismatch(trail.Clear, "clear");

            Equal(1, history.EnumeratorRequests, "clear enumerator requests");
            Equal(0, history.AddRequests, "clear add requests");
            Equal(0, history.ClearRequests, "clear mutation requests");
        }

        private static AuditTrail BuildTrail(IList<AuditEvent> history)
        {
            var constructor = typeof(AuditTrail).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(IList<AuditEvent>), typeof(ProjectState) },
                modifiers: null);
            if (constructor == null)
                throw new Exception("AuditTrailKnownCountOverrunSmoke could not resolve the bounded-history constructor.");

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

                throw new Exception(
                    "AuditTrailKnownCountOverrunSmoke " + label +
                    " processed the unexpected event before enforcing the known Count contract. Actual=" + ex.Message,
                    ex);
            }

            throw new Exception("AuditTrailKnownCountOverrunSmoke " + label + " expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(
                    "AuditTrailKnownCountOverrunSmoke " + label +
                    ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class UnderreportedInvalidHistory : IList<AuditEvent>
        {
            private readonly AuditEvent _unexpected = new AuditEvent
            {
                Utc = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Local),
                Action = string.Empty,
                ElementId = "E-unexpected",
                Detail = "unexpected"
            };

            internal int EnumeratorRequests { get; private set; }
            internal int AddRequests { get; private set; }
            internal int ClearRequests { get; private set; }

            public int Count => 0;
            public bool IsReadOnly => false;
            public AuditEvent this[int index]
            {
                get => index == 0 ? _unexpected : throw new ArgumentOutOfRangeException(nameof(index));
                set => throw new NotSupportedException();
            }

            public void Add(AuditEvent item)
            {
                AddRequests++;
                throw new InvalidOperationException("Unexpected audit history mutation.");
            }

            public void Clear()
            {
                ClearRequests++;
                throw new InvalidOperationException("Unexpected audit history clear.");
            }

            public bool Contains(AuditEvent item) => ReferenceEquals(item, _unexpected);
            public void CopyTo(AuditEvent[] array, int arrayIndex) => throw new NotSupportedException();
            public int IndexOf(AuditEvent item) => ReferenceEquals(item, _unexpected) ? 0 : -1;
            public void Insert(int index, AuditEvent item) => throw new NotSupportedException();
            public bool Remove(AuditEvent item) => throw new NotSupportedException();
            public void RemoveAt(int index) => throw new NotSupportedException();

            public IEnumerator<AuditEvent> GetEnumerator()
            {
                EnumeratorRequests++;
                yield return _unexpected;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
