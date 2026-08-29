using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemMappingKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            GenericCountDriftRejects();
            ReadOnlyCountDriftRejects();
            NonGenericCountDriftRejects();
            NegativePostTraversalCountRejects();
            ConflictingPostTraversalCountsReject();
            StableCountedSourceSucceeds();
            PureStreamingSourceSucceeds();
        }

        private static void GenericCountDriftRejects()
        {
            var error = Capture<InvalidOperationException>(() =>
                new MeasurementWorkItemMappingCatalog(new GenericDriftCollection(1, 2)));
            Equal("Measurement/work-item mapping source known Count changed during traversal.", error.Message);
        }

        private static void ReadOnlyCountDriftRejects()
        {
            var error = Capture<InvalidOperationException>(() =>
                new MeasurementWorkItemMappingCatalog(new ReadOnlyDriftCollection(1, 2)));
            Equal("Measurement/work-item mapping source known Count changed during traversal.", error.Message);
        }

        private static void NonGenericCountDriftRejects()
        {
            var error = Capture<InvalidOperationException>(() =>
                new MeasurementWorkItemMappingCatalog(new NonGenericDriftCollection(1, 2)));
            Equal("Measurement/work-item mapping source known Count changed during traversal.", error.Message);
        }

        private static void NegativePostTraversalCountRejects()
        {
            var error = Capture<InvalidOperationException>(() =>
                new MeasurementWorkItemMappingCatalog(new GenericDriftCollection(1, -1)));
            Equal("Measurement/work-item mapping source exposes an invalid negative known Count value after traversal.", error.Message);
        }

        private static void ConflictingPostTraversalCountsReject()
        {
            var error = Capture<InvalidOperationException>(() =>
                new MeasurementWorkItemMappingCatalog(new ConflictingAfterTraversalCollection()));
            Equal("Measurement/work-item mapping source exposes conflicting known Count values after traversal.", error.Message);
        }

        private static void StableCountedSourceSucceeds()
        {
            var catalog = new MeasurementWorkItemMappingCatalog(
                new List<MeasurementWorkItemMapping> { Mapping(1) });
            Equal(1, catalog.Mappings.Count);
            Equal("MAP-STABILITY-00001", catalog.Mappings[0].MappingId);
        }

        private static void PureStreamingSourceSucceeds()
        {
            var catalog = new MeasurementWorkItemMappingCatalog(new StreamingMappings());
            Equal(1, catalog.Mappings.Count);
            Equal("MAP-STABILITY-00001", catalog.Mappings[0].MappingId);
        }

        private static MeasurementWorkItemMapping Mapping(int index)
        {
            var suffix = index.ToString("D5");
            return new MeasurementWorkItemMapping(
                "MAP-STABILITY-" + suffix,
                ElementCategory.StructuralWall,
                "MEASURE-STABILITY-" + suffix,
                "CLASS-STABILITY-" + suffix,
                "WORK-STABILITY-" + suffix);
        }

        private abstract class DriftEnumerableBase : IEnumerable<MeasurementWorkItemMapping>
        {
            protected bool Traversed;

            public IEnumerator<MeasurementWorkItemMapping> GetEnumerator()
            {
                try
                {
                    yield return Mapping(1);
                }
                finally
                {
                    Traversed = true;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class GenericDriftCollection : DriftEnumerableBase, ICollection<MeasurementWorkItemMapping>
        {
            private readonly int _before;
            private readonly int _after;

            internal GenericDriftCollection(int before, int after)
            {
                _before = before;
                _after = after;
            }

            public int Count => Traversed ? _after : _before;
            public bool IsReadOnly => true;
            public void Add(MeasurementWorkItemMapping item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(MeasurementWorkItemMapping item) => false;
            public void CopyTo(MeasurementWorkItemMapping[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(MeasurementWorkItemMapping item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyDriftCollection : DriftEnumerableBase, IReadOnlyCollection<MeasurementWorkItemMapping>
        {
            private readonly int _before;
            private readonly int _after;

            internal ReadOnlyDriftCollection(int before, int after)
            {
                _before = before;
                _after = after;
            }

            public int Count => Traversed ? _after : _before;
        }

        private sealed class NonGenericDriftCollection : DriftEnumerableBase, ICollection
        {
            private readonly int _before;
            private readonly int _after;

            internal NonGenericDriftCollection(int before, int after)
            {
                _before = before;
                _after = after;
            }

            public int Count => Traversed ? _after : _before;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ConflictingAfterTraversalCollection : DriftEnumerableBase,
            ICollection<MeasurementWorkItemMapping>, IReadOnlyCollection<MeasurementWorkItemMapping>
        {
            int ICollection<MeasurementWorkItemMapping>.Count => 1;
            int IReadOnlyCollection<MeasurementWorkItemMapping>.Count => Traversed ? 2 : 1;
            bool ICollection<MeasurementWorkItemMapping>.IsReadOnly => true;
            void ICollection<MeasurementWorkItemMapping>.Add(MeasurementWorkItemMapping item) => throw new NotSupportedException();
            void ICollection<MeasurementWorkItemMapping>.Clear() => throw new NotSupportedException();
            bool ICollection<MeasurementWorkItemMapping>.Contains(MeasurementWorkItemMapping item) => false;
            void ICollection<MeasurementWorkItemMapping>.CopyTo(MeasurementWorkItemMapping[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<MeasurementWorkItemMapping>.Remove(MeasurementWorkItemMapping item) => throw new NotSupportedException();
        }

        private sealed class StreamingMappings : IEnumerable<MeasurementWorkItemMapping>
        {
            public IEnumerator<MeasurementWorkItemMapping> GetEnumerator()
            {
                yield return Mapping(1);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("MeasurementWorkItemMappingKnownCountStabilitySmoke expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
