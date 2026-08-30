using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripProjectionCurrentCountIntegritySmoke
    {
        internal static void Run()
        {
            DimensionCurrentDriftWinsOverNullSemanticFailure();
            ProvenanceCurrentDriftWinsOverTokenSemanticFailure();
            ProjectionCurrentDriftWinsOverNullSemanticFailure();
            StableCountedInputsRemainAccepted();
        }

        private static void DimensionCurrentDriftWinsOverNullSemanticFailure()
        {
            var dimensions = new CurrentCountCollection<IfcRoundTripNumericProperty>(null!, true);

            var error = Capture<InvalidOperationException>(() =>
                NewProjection("Q-DIM-CURRENT", "IFC-DIM-CURRENT", dimensions, new[] { "SRC" }));

            Contains("dimension source Count changed during traversal", error.Message,
                "Current-induced dimension Count drift must be rejected before null-item semantic validation.");
            Equal(1, dimensions.CurrentReads,
                "Hostile dimension source should expose Current exactly once.");
            Equal(4, dimensions.CountReads,
                "Dimension Count drift must be observed by the immediate post-Current rebound.");
            Equal(1, dimensions.MoveNextCalls,
                "Dimension Count drift must be rejected before another MoveNext call.");
        }

        private static void ProvenanceCurrentDriftWinsOverTokenSemanticFailure()
        {
            var provenance = new CurrentCountCollection<string>(" ", true);

            var error = Capture<InvalidOperationException>(() =>
                NewProjection(
                    "Q-PROV-CURRENT",
                    "IFC-PROV-CURRENT",
                    new[] { new IfcRoundTripNumericProperty("Length", 2d, "m") },
                    provenance));

            Contains("provenance source Count changed during traversal", error.Message,
                "Current-induced provenance Count drift must be rejected before token semantic validation.");
            Equal(1, provenance.CurrentReads,
                "Hostile provenance source should expose Current exactly once.");
            Equal(4, provenance.CountReads,
                "Provenance Count drift must be observed by the immediate post-Current rebound.");
            Equal(1, provenance.MoveNextCalls,
                "Provenance Count drift must be rejected before another MoveNext call.");
        }

        private static void ProjectionCurrentDriftWinsOverNullSemanticFailure()
        {
            var projections = new CurrentCountCollection<IfcRoundTripProjection>(null!, true);

            var error = Capture<InvalidOperationException>(() =>
                IfcRoundTripProjectionSet.Create(projections));

            Contains("projection source Count changed during traversal", error.Message,
                "Current-induced projection Count drift must be rejected before null-projection semantic validation.");
            Equal(1, projections.CurrentReads,
                "Hostile projection source should expose Current exactly once.");
            Equal(4, projections.CountReads,
                "Projection Count drift must be observed by the immediate post-Current rebound.");
            Equal(1, projections.MoveNextCalls,
                "Projection Count drift must be rejected before another MoveNext call.");
        }

        private static void StableCountedInputsRemainAccepted()
        {
            var dimensions = new CurrentCountCollection<IfcRoundTripNumericProperty>(
                new IfcRoundTripNumericProperty("Length", 2d, "m"), false);
            var provenance = new CurrentCountCollection<string>("SRC", false);
            var projection = NewProjection(
                "Q-STABLE-CURRENT",
                "IFC-STABLE-CURRENT",
                dimensions,
                provenance);

            Equal(1, projection.Dimensions.Count,
                "Stable counted dimensions must remain accepted.");
            Equal(1, projection.Provenance.Count,
                "Stable counted provenance must remain accepted.");
            Equal(6, dimensions.CountReads,
                "Stable dimension Count evidence must be rebound at admission, around traversal, after Current, and after traversal.");
            Equal(6, provenance.CountReads,
                "Stable provenance Count evidence must be rebound at admission, around traversal, after Current, and after traversal.");

            var projections = new CurrentCountCollection<IfcRoundTripProjection>(projection, false);
            Equal(1, IfcRoundTripProjectionSet.Create(projections).Items.Count,
                "Stable counted projection sets must remain accepted.");
            Equal(6, projections.CountReads,
                "Stable projection Count evidence must be rebound at admission, around traversal, after Current, and after traversal.");
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

        private sealed class CurrentCountCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T _item;
            private readonly bool _driftFromCurrent;
            private bool _currentWasRead;

            internal CurrentCountCollection(T item, bool driftFromCurrent)
            {
                _item = item;
                _driftFromCurrent = driftFromCurrent;
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _currentWasRead && _driftFromCurrent ? 2 : 1;
                }
            }

            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CurrentCountCollection<T> _owner;
                private int _index = -1;

                internal Enumerator(CurrentCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index == 0;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._currentWasRead = true;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class IfcRoundTripProjectionCurrentCountIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            IfcRoundTripProjectionCurrentCountIntegritySmoke.Run();
        }
    }
}
