using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Xml.Linq;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class CustomerWorkbookBusinessRowIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            StableBusinessRowsRetainOnlyHeaderAndTarget();
            DuplicateHeaderAndTargetFailClosed();
            MalformedUnrelatedRowFailsClosed();
            OutOfRangeUnrelatedRowFailsClosed();
            SurplusRowFailsBeforeUnexpectedCurrent();
        }

        private static void StableBusinessRowsRetainOnlyHeaderAndTarget()
        {
            var header = Row("1");
            var target = Row("7");
            var selected = InvokeSelector(new[] { header, Row("3"), target, Row("9") }, 7, 4);
            Same(header, selected.Item1, "selected header row");
            Same(target, selected.Item2, "selected target row");
        }

        private static void DuplicateHeaderAndTargetFailClosed()
        {
            Expect<InvalidDataException>(
                () => InvokeSelector(new[] { Row("1"), Row("1"), Row("7") }, 7, 3),
                "duplicate business header row");
            Expect<InvalidDataException>(
                () => InvokeSelector(new[] { Row("1"), Row("7"), Row("7") }, 7, 3),
                "duplicate business target row");
        }

        private static void MalformedUnrelatedRowFailsClosed()
        {
            Expect<InvalidDataException>(
                () => InvokeSelector(new[] { Row("1"), Row("7"), Row("broken") }, 7, 3),
                "malformed unrelated business row metadata");
        }

        private static void OutOfRangeUnrelatedRowFailsClosed()
        {
            Expect<InvalidDataException>(
                () => InvokeSelector(new[] { Row("1"), Row("7"), Row("1048577") }, 7, 3),
                "out-of-range unrelated business row metadata");
        }

        private static void SurplusRowFailsBeforeUnexpectedCurrent()
        {
            var source = new ProbeRows(3);
            Expect<InvalidDataException>(() => InvokeSelector(source, 2, 2), "business worksheet first row over declared ceiling");
            Equal(2, source.CurrentReads, "surplus business row must fail before unexpected Current");
            Equal(3, source.MoveNextCalls, "surplus business row must be discovered by MoveNext");
        }

        private static Tuple<XElement, XElement> InvokeSelector(IEnumerable<XElement> source, int targetRowNumber, int maximum)
        {
            var method = typeof(QsCustomerWorkbookTraceReader).GetMethod(
                "SelectBusinessRowsBounded",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Customer workbook bounded business-row selector was not found.");

            try
            {
                return (Tuple<XElement, XElement>)(method.Invoke(null, new object[] { source, targetRowNumber, maximum })
                    ?? throw new InvalidOperationException("Customer workbook bounded business-row selector returned null."));
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static XElement Row(string rowNumber) => new XElement("row", new XAttribute("r", rowNumber));

        private static void Expect<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException(label + " must fail with " + typeof(TException).Name + ".");
        }

        private static void Same(object expected, object actual, string label)
        {
            if (!ReferenceEquals(expected, actual))
                throw new InvalidOperationException(label + " must preserve the selected row instance.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class ProbeRows : IEnumerable<XElement>
        {
            private readonly int _count;

            internal ProbeRows(int count)
            {
                _count = count;
            }

            internal int CurrentReads { get; private set; }
            internal int MoveNextCalls { get; private set; }

            public IEnumerator<XElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<XElement>
            {
                private readonly ProbeRows _owner;
                private int _index = -1;

                internal Enumerator(ProbeRows owner)
                {
                    _owner = owner;
                }

                public XElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return Row((_index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_index + 1 >= _owner._count)
                    {
                        _index = _owner._count;
                        return false;
                    }
                    _index++;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
