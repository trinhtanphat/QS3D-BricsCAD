using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripTransientKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            DimensionGrowthFailsBeforeCurrent();
            ProvenanceShrinkFailsBeforeCurrent();
            ProjectionNegativeCountFailsBeforeCurrent();
            DimensionConflictingCountFailsBeforeCurrent();
            StableCountedInputsRemainAccepted();
        }

        private static void DimensionGrowthFailsBeforeCurrent()
        {
            var dimensions = new DriftAfterMoveCollection<IfcRoundTripNumericProperty>(
                admittedCount: 1,
                driftCount: 2,
                new IfcRoundTripNumericProperty("Length", 2d, "m"));

            var error = Capture<InvalidOperationException>(() =>
                NewProjection("Q-DIM-DRIFT", "IFC-DIM-DRIFT", dimensions, new[] { "SRC" }));

            Contains("dimension source Count changed during traversal", error.Message,
                "Transient dimension Count growth must fail closed.");
            Equal(0, dimensions.CurrentReads,
                "Transient dimension Count growth must fail before Current is observed.");
        }

        private static void ProvenanceShrinkFailsBeforeCurrent()
        {
            var provenance = new DriftAfterMoveCollection<string>(
                admittedCount: 1,
                driftCount: 0,
                "SRC");

            var error = Capture<InvalidOperationException>(() =>
                NewProjection(
                    "Q-PROV-DRIFT",
                    "IFC-PROV-DRIFT",
                    new[] { new IfcRoundTripNumericProperty("Length", 2d, "m") },
                    provenance));

            Contains("provenance source Count changed during traversal", error.Message,
                "Transient provenance Count shrink must fail closed.");
            Equal(0, provenance.CurrentReads,
                "Transient provenance Count shrink must fail before Current is observed.");
        }

        private static void ProjectionNegativeCountFailsBeforeCurrent()
        {
            var projections = new DriftAfterMoveCollection<IfcRoundTripProjection>(
                admittedCount: 1,
                driftCount: -1,
                NewProjection("Q-NEG-DRIFT", "IFC-NEG-DRIFT"));

            var error = Capture<InvalidOperationException>(() =>
                IfcRoundTripProjectionSet.Create(projections));

            Contains("invalid negative known Count value during traversal", error.Message,
                "Transient negative projection Count must fail closed.");
            Equal(0, projections.CurrentReads,
                "Transient negative projection Count must fail before Current is observed.");
        }

        private static void DimensionConflictingCountFailsBeforeCurrent()
        {
            var dimensions = new ConflictAfterMoveCollection<IfcRoundTripNumericProperty>(
                new IfcRoundTripNumericProperty("Length", 2d, "m"));

            var error = Capture<InvalidOperationException>(() =>
                NewProjection("Q-CONFLICT-DRIFT", "IFC-CONFLICT-DRIFT", dimensions, new[] { "SRC" }));

            Contains("conflicting known Count values during traversal", error.Message,
                "Transient conflicting dimension Count evidence must fail closed.");
            Equal(0, dimensions.CurrentReads,
                "Transient conflicting Count evidence must fail before Current is observed.");
        }

        private static void StableCountedInputsRemainAccepted()
        {
            var dimensions = new DriftAfterMoveCollection<IfcRoundTripNumericProperty>(
                admittedCount: 1,
                driftCount: 1,
                new IfcRoundTripNumericProperty("Length", 2d, "m"));
            var provenance = new DriftAfterMoveCollection<string>(1, 1, "SRC");
            var projection = NewProjection(
                "Q-STABLE-TRANSIENT",
                "IFC-STABLE-TRANSIENT",
                dimensions,
                provenance);

            Equal(1, projection.Dimensions.Count,
                "Stable counted dimensions must remain accepted.");
            Equal(1, projection.Provenance.Count,
                "Stable counted provenance must remain accepted.");
            Equal(1, dimensions.CurrentReads,
                "Stable dimensions should read Current exactly once.");
            Equal(1, provenance.CurrentReads,
                "Stable provenance should read Current exactly once.");

            var projections = new DriftAfterMoveCollection<IfcRoundTripProjection>(1, 1, projection);
            Equal(1, IfcRoundTripProjectionSet.Create(projections).Items.Count,
                "Stable counted projection sets must remain accepted.");
            Equal(1, projections.CurrentReads,
                "Stable projection sets should read Current exactly once.");
        }

        private static IfcRoundTripProjection NewProjection(
            string qs3dId,
            string ifcId,
            IEnumerable<IfcRoundTripNumericProperty>? dimensions = null,
            IEnumerable<string>? provenance = null)
        {
            return new IfcRoundTripProjection(
                qs3dId,
                ifcId,
                "Wall",
                dimensions ?? new[] { new IfcRoundTripNumericProperty("Length", 1d, "m") },
                1d,
                "m3",
                provenance ?? new[] { "SRC" });
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

        private sealed class DriftAfterMoveCollection<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly int _admittedCount;
            private readonly int _driftCount;
            private bool _advanced;

            internal DriftAfterMoveCollection(int admittedCount, int driftCount, params T[] items)
            {
                _admittedCount = admittedCount;
                _driftCount = driftCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count => _advanced ? _driftCount : _admittedCount;
            public bool IsReadOnly => true;
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly DriftAfterMoveCollection<T> _owner;
                private int _index = -1;

                internal Enumerator(DriftAfterMoveCollection<T> owner) { _owner = owner; }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _index++;
                    var hasItem = _index < _owner._items.Length;
                    if (hasItem)
                        _owner._advanced = true;
                    return hasItem;
                }

                public void Reset() { throw new NotSupportedException(); }
                public void Dispose() { }
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class ConflictAfterMoveCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private bool _advanced;

            internal ConflictAfterMoveCollection(params T[] items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            int ICollection<T>.Count => 1;
            int IReadOnlyCollection<T>.Count => _advanced ? 2 : 1;
            public bool IsReadOnly => true;
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly ConflictAfterMoveCollection<T> _owner;
                private int _index = -1;

                internal Enumerator(ConflictAfterMoveCollection<T> owner) { _owner = owner; }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _index++;
                    var hasItem = _index < _owner._items.Length;
                    if (hasItem)
                        _owner._advanced = true;
                    return hasItem;
                }

                public void Reset() { throw new NotSupportedException(); }
                public void Dispose() { }
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }

    internal static class IfcRoundTripTransientKnownCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            IfcRoundTripTransientKnownCountStabilitySmoke.Run();
        }
    }
}
