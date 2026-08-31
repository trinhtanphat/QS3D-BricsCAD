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
    internal static class CustomerWorkbookWorksheetRowBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ExactLimitIsRetainedWithoutOverread();
            FirstOverLimitFailsBeforeUnexpectedCurrent();
        }

        private static void ExactLimitIsRetainedWithoutOverread()
        {
            var source = new ProbeRows(2);
            var rows = InvokeMaterializer(source, 2);
            Equal(2, rows.Count, "exact-limit row count");
            Equal(2, source.CurrentReads, "exact-limit Current reads");
            Equal(3, source.MoveNextCalls, "exact-limit MoveNext calls including terminal false");
        }

        private static void FirstOverLimitFailsBeforeUnexpectedCurrent()
        {
            var source = new ProbeRows(3);
            Expect<InvalidDataException>(() => InvokeMaterializer(source, 2), "first row over the declared ceiling");
            Equal(2, source.CurrentReads, "surplus row must be rejected before unexpected Current");
            Equal(3, source.MoveNextCalls, "surplus row must be discovered by MoveNext before rejection");
        }

        private static IReadOnlyList<XElement> InvokeMaterializer(IEnumerable<XElement> source, int maximum)
        {
            var method = typeof(QsCustomerWorkbookTraceReader).GetMethod(
                "MaterializeWorksheetRowsBounded",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Customer workbook bounded worksheet-row materializer was not found.");

            try
            {
                return (IReadOnlyList<XElement>)(method.Invoke(null, new object[] { source, maximum })
                    ?? throw new InvalidOperationException("Customer workbook bounded worksheet-row materializer returned null."));
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

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
                        return new XElement("row", new XAttribute("r", _index + 1));
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
