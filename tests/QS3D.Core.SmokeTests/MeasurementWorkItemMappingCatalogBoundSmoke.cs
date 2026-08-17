using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemMappingCatalogBoundSmoke
    {
        private const int MaximumEntries = 10000;

        internal static void Run()
        {
            KnownCountOversizeRejectsBeforeEnumeration();
            StreamingOversizeStopsAtFirstDisallowedEntry();
            ExactBoundaryPreservesCatalogBehavior();
        }

        private static void KnownCountOversizeRejectsBeforeEnumeration()
        {
            var mappings = new KnownCountCollection(MaximumEntries + 1);
            Throws<InvalidOperationException>(() => new MeasurementWorkItemMappingCatalog(mappings));
            Equal(false, mappings.EnumerationStarted, "Known-count oversized mapping catalog must fail before enumeration.");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedEntry()
        {
            var mappings = new StreamingMappings(MaximumEntries + 2);
            Throws<InvalidOperationException>(() => new MeasurementWorkItemMappingCatalog(mappings));
            Equal(
                MaximumEntries + 1,
                mappings.YieldedCount,
                "Mapping catalog ingestion requested an entry after the first disallowed item.");
        }

        private static void ExactBoundaryPreservesCatalogBehavior()
        {
            var mappings = new List<MeasurementWorkItemMapping>(MaximumEntries);
            for (var i = 0; i < MaximumEntries; i++)
                mappings.Add(CreateMapping(i));

            var catalog = new MeasurementWorkItemMappingCatalog(mappings);

            Equal(MaximumEntries, catalog.Mappings.Count, "Mapping catalog boundary count changed.");
            Equal("MAP-00000", catalog.Mappings[0].MappingId, "Mapping catalog deterministic first item changed.");
            Equal("MAP-09999", catalog.Mappings[MaximumEntries - 1].MappingId, "Mapping catalog deterministic last item changed.");

            var resolution = catalog.Resolve(ElementCategory.StructuralWall, "MEASURE-05000");
            Equal(true, resolution.IsMapped, "Mapping resolution failed at accepted boundary.");
            Equal("MAP-05000", resolution.Mapping?.MappingId, "Mapping resolution identity changed at accepted boundary.");
        }

        private static MeasurementWorkItemMapping CreateMapping(int index)
        {
            var suffix = index.ToString("D5");
            return new MeasurementWorkItemMapping(
                "MAP-" + suffix,
                ElementCategory.StructuralWall,
                "MEASURE-" + suffix,
                "CLASS-" + suffix,
                "WORK-" + suffix);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private sealed class StreamingMappings : IEnumerable<MeasurementWorkItemMapping>
        {
            private readonly int _count;

            internal StreamingMappings(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<MeasurementWorkItemMapping> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return CreateMapping(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class KnownCountCollection : ICollection<MeasurementWorkItemMapping>
        {
            private readonly int _count;

            internal KnownCountCollection(int count)
            {
                _count = count;
            }

            internal bool EnumerationStarted { get; private set; }
            public int Count => _count;
            public bool IsReadOnly => true;

            public IEnumerator<MeasurementWorkItemMapping> GetEnumerator()
            {
                EnumerationStarted = true;
                throw new InvalidOperationException("Enumeration should not start for a rejected known-count collection.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(MeasurementWorkItemMapping item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(MeasurementWorkItemMapping item) => false;
            public void CopyTo(MeasurementWorkItemMapping[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(MeasurementWorkItemMapping item) => throw new NotSupportedException();
        }
    }
}
