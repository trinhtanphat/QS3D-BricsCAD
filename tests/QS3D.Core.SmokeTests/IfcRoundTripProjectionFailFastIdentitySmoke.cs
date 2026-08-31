using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripProjectionFailFastIdentitySmoke
    {
        internal static void Run()
        {
            RejectsDuplicateIfcIdentityBeforeTailEnumeration();
            RejectsDuplicateQs3dIdentityBeforeTailEnumeration();
            RejectsNullProjectionBeforeTailEnumeration();
        }

        private static void RejectsDuplicateIfcIdentityBeforeTailEnumeration()
        {
            var first = Projection("BEAM-01", "ifc-shared");
            var duplicate = Projection("COLUMN-01", "ifc-shared");
            var source = new ThrowingTailSource(first, duplicate);

            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));

            Contains("Duplicate IFC global identity", error.Message,
                "Projection-set duplicate IFC identity must win over unrelated caller tail behavior.");
            Equal(2, source.MoveNextCalls,
                "Duplicate IFC identity must reject immediately after the duplicate item is admitted.");
            Equal(2, source.CurrentReads,
                "Duplicate IFC identity must not observe a later caller-controlled Current.");
        }

        private static void RejectsDuplicateQs3dIdentityBeforeTailEnumeration()
        {
            var first = Projection("BEAM-01", "ifc-a");
            var duplicate = Projection("beam-01", "ifc-b");
            var source = new ThrowingTailSource(first, duplicate);

            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));

            Contains("Duplicate QS3D element identity", error.Message,
                "Projection-set duplicate QS3D identity must win over unrelated caller tail behavior.");
            Equal(2, source.MoveNextCalls,
                "Duplicate QS3D identity must reject immediately after the duplicate item is admitted.");
            Equal(2, source.CurrentReads,
                "Duplicate QS3D identity must not observe a later caller-controlled Current.");
        }

        private static void RejectsNullProjectionBeforeTailEnumeration()
        {
            var source = new ThrowingTailSource(Projection("BEAM-01", "ifc-a"), null!);

            var error = Capture<ArgumentException>(() => IfcRoundTripProjectionSet.Create(source));

            Contains("cannot contain null entries", error.Message,
                "Projection-set null rejection must win over unrelated caller tail behavior.");
            Equal(2, source.MoveNextCalls,
                "Null projection must reject immediately after the null item is admitted.");
            Equal(2, source.CurrentReads,
                "Null projection must not observe a later caller-controlled Current.");
        }

        private static IfcRoundTripProjection Projection(string qs3dId, string ifcId)
        {
            return new IfcRoundTripProjection(
                qs3dId,
                ifcId,
                "IfcBuildingElementProxy",
                new[] { new IfcRoundTripNumericProperty("Length", 1d, "m") },
                1d,
                "m",
                new[] { "source:test" });
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class ThrowingTailSource : IEnumerable<IfcRoundTripProjection>
        {
            private readonly IfcRoundTripProjection[] _items;

            internal ThrowingTailSource(params IfcRoundTripProjection[] items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<IfcRoundTripProjection> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<IfcRoundTripProjection>
            {
                private readonly ThrowingTailSource _owner;
                private int _index = -1;

                internal Enumerator(ThrowingTailSource owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index < _owner._items.Length)
                        return true;
                    throw new InvalidOperationException("Caller tail was enumerated after a decisive projection-set semantic error.");
                }

                public IfcRoundTripProjection Current
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
    }

    internal static class IfcRoundTripProjectionFailFastIdentityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            IfcRoundTripProjectionFailFastIdentitySmoke.Run();
        }
    }
}
