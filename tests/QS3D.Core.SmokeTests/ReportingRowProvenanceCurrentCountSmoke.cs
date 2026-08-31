using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ReportingRowProvenanceCurrentCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CurrentInducedCountDriftPreemptsMalformedItemValidation();
        }

        private static void CurrentInducedCountDriftPreemptsMalformedItemValidation()
        {
            var target = new List<string> { "AA" };
            var source = new CurrentDriftSource("   ", admittedCount: 1, driftedCount: 2);

            ThrowsContaining(
                () => Append(target, source),
                "known Count changed during traversal from 1 to 2");

            Equal(1, source.MoveNextCalls, "MoveNext calls");
            Equal(1, source.CurrentReads, "Current reads");
            Equal(1, target.Count, "target count");
            Equal("AA", target[0], "target seed");
        }

        private static void Append(IList<string> target, IEnumerable<string> source)
        {
            var type = typeof(DoorOpeningScheduleBuilder).Assembly.GetType(
                "QS3D.Core.Reporting.ReportingRowProvenance",
                throwOnError: true)!;
            var method = type.GetMethod("AppendSourceHandles", BindingFlags.Static | BindingFlags.NonPublic)!;
            try
            {
                method.Invoke(null, new object[] { target, source });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void ThrowsContaining(Action action, string expectedText)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException(
                    "Expected Current-induced reporting provenance failure containing '" + expectedText +
                    "', got '" + ex.Message + "'.",
                    ex);
            }

            throw new InvalidOperationException(
                "Expected Current-induced reporting provenance failure containing '" + expectedText + "'.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "ReportingRowProvenanceCurrentCountSmoke " + label +
                    ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CurrentDriftSource : IReadOnlyCollection<string>
        {
            private readonly string _value;
            private readonly int _admittedCount;
            private readonly int _driftedCount;
            private bool _drifted;

            public CurrentDriftSource(string value, int admittedCount, int driftedCount)
            {
                _value = value;
                _admittedCount = admittedCount;
                _driftedCount = driftedCount;
            }

            public int Count => _drifted ? _driftedCount : _admittedCount;
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly CurrentDriftSource _owner;
                private int _index = -1;

                public Enumerator(CurrentDriftSource owner)
                {
                    _owner = owner;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._drifted = true;
                        return _owner._value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index == 0;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
