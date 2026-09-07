using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemMappingCatalogKnownCountGenerationSmoke
    {
        internal static void Run()
        {
            MoveNextInducedCountDriftFailsBeforeCurrent();
            CurrentInducedCountDriftFailsBeforeMappingAcceptance();
            StableCountedSourceRemainsAccepted();
            PureStreamingSourceRemainsAccepted();
        }

        private static void MoveNextInducedCountDriftFailsBeforeCurrent()
        {
            var source = new DriftingCountSequence<MeasurementWorkItemMapping>(
                CreateMapping("map-move"),
                driftOnMoveNext: true,
                driftOnCurrent: false);

            var error = Capture<InvalidOperationException>(() => new MeasurementWorkItemMappingCatalog(source));

            Contains("known Count changed during MoveNext from 1 to 2", error.Message,
                "MoveNext-induced Count drift must fail at the catalog generation boundary.");
            Equal(1, source.MoveNextCalls, "MoveNext drift fixture must advance exactly once.");
            Equal(0, source.CurrentReads, "MoveNext Count drift must fail before Current is read.");
        }

        private static void CurrentInducedCountDriftFailsBeforeMappingAcceptance()
        {
            var source = new DriftingCountSequence<MeasurementWorkItemMapping>(
                CreateMapping("map-current"),
                driftOnMoveNext: false,
                driftOnCurrent: true);

            var error = Capture<InvalidOperationException>(() => new MeasurementWorkItemMappingCatalog(source));

            Contains("known Count changed during Current from 1 to 2", error.Message,
                "Current-induced Count drift must fail before the returned mapping is accepted.");
            Equal(1, source.MoveNextCalls, "Current drift fixture must advance exactly once.");
            Equal(1, source.CurrentReads, "Current drift fixture must read exactly one unstable item.");
        }

        private static void StableCountedSourceRemainsAccepted()
        {
            var mapping = CreateMapping("map-stable");
            var catalog = new MeasurementWorkItemMappingCatalog(
                new StableCountedSequence<MeasurementWorkItemMapping>(mapping));

            Equal(1, catalog.Mappings.Count, "Stable known-Count mapping source must remain accepted.");
            Equal(mapping.MappingId, catalog.Mappings[0].MappingId, "Stable mapping identity changed.");
        }

        private static void PureStreamingSourceRemainsAccepted()
        {
            var mapping = CreateMapping("map-stream");
            var catalog = new MeasurementWorkItemMappingCatalog(Stream(mapping));

            Equal(1, catalog.Mappings.Count, "Pure streaming source without a supported Count must remain accepted.");
            Equal(mapping.MappingId, catalog.Mappings[0].MappingId, "Streaming mapping identity changed.");
        }

        private static MeasurementWorkItemMapping CreateMapping(string id) =>
            new MeasurementWorkItemMapping(id, ElementCategory.Column, "LengthM", "classification", "work-item");

        private static IEnumerable<MeasurementWorkItemMapping> Stream(MeasurementWorkItemMapping mapping)
        {
            yield return mapping;
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class DriftingCountSequence<T> : ICollection<T>
        {
            private readonly T _item;
            private readonly bool _driftOnMoveNext;
            private readonly bool _driftOnCurrent;
            private int _count = 1;

            internal DriftingCountSequence(T item, bool driftOnMoveNext, bool driftOnCurrent)
            {
                _item = item;
                _driftOnMoveNext = driftOnMoveNext;
                _driftOnCurrent = driftOnCurrent;
            }

            public int Count => _count;
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => false;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly DriftingCountSequence<T> _owner;
                private bool _yielded;

                internal Enumerator(DriftingCountSequence<T> owner)
                {
                    _owner = owner;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._driftOnCurrent)
                            _owner._count = 2;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_yielded)
                    {
                        _owner._count = 1;
                        return false;
                    }

                    _yielded = true;
                    if (_owner._driftOnMoveNext)
                        _owner._count = 2;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StableCountedSequence<T> : ICollection<T>
        {
            private readonly T _item;

            internal StableCountedSequence(T item)
            {
                _item = item;
            }

            public int Count => 1;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                yield return _item;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => false;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        }
    }

    internal static class MeasurementWorkItemMappingCatalogKnownCountGenerationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasurementWorkItemMappingCatalogKnownCountGenerationSmoke.Run();
        }
    }
}
