using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemMappingCatalogTraversalCountSmoke
    {
        internal static void Run()
        {
            UnderEnumerationRejects();
            OverEnumerationRejectsEarly();
            KnownCountOverrunPrecedesUnexpectedMappingValidation();
            HonestKnownCountRemainsAccepted();
            PureStreamingRemainsAccepted();
        }

        private static void UnderEnumerationRejects()
        {
            var mappings = new ReportedCountCollection(reportedCount: 2, actualCount: 1);
            var error = Capture<InvalidOperationException>(() => new MeasurementWorkItemMappingCatalog(mappings));
            Contains("known Count does not match completed traversal cardinality", error.Message);
        }

        private static void OverEnumerationRejectsEarly()
        {
            var mappings = new ReportedCountCollection(reportedCount: 1, actualCount: 2);
            var error = Capture<InvalidOperationException>(() => new MeasurementWorkItemMappingCatalog(mappings));
            Contains("traversal produced more entries than its known Count reported 1", error.Message);
        }

        private static void KnownCountOverrunPrecedesUnexpectedMappingValidation()
        {
            var error = Capture<InvalidOperationException>(
                () => new MeasurementWorkItemMappingCatalog(new UnexpectedOverrunCollection()));
            Contains("traversal produced more entries than its known Count reported 1", error.Message);
        }

        private static void HonestKnownCountRemainsAccepted()
        {
            var catalog = new MeasurementWorkItemMappingCatalog(
                new ReportedCountCollection(reportedCount: 2, actualCount: 2));
            Equal(2, catalog.Mappings.Count, "Honest counted mapping input changed accepted cardinality.");
        }

        private static void PureStreamingRemainsAccepted()
        {
            var catalog = new MeasurementWorkItemMappingCatalog(new StreamingMappings(2));
            Equal(2, catalog.Mappings.Count, "Pure streaming mapping input must remain supported.");
        }

        private static MeasurementWorkItemMapping CreateMapping(int index)
        {
            var suffix = index.ToString("D5");
            return new MeasurementWorkItemMapping(
                "MAP-TRAVERSAL-" + suffix,
                ElementCategory.StructuralWall,
                "MEASURE-TRAVERSAL-" + suffix,
                "CLASS-TRAVERSAL-" + suffix,
                "WORK-TRAVERSAL-" + suffix);
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

        private static void Contains(string expected, string actual)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Expected diagnostic fragment '" + expected + "'. Actual: " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private sealed class ReportedCountCollection : ICollection<MeasurementWorkItemMapping>
        {
            private readonly int _reportedCount;
            private readonly int _actualCount;

            internal ReportedCountCollection(int reportedCount, int actualCount)
            {
                _reportedCount = reportedCount;
                _actualCount = actualCount;
            }

            public int Count => _reportedCount;
            public bool IsReadOnly => true;

            public IEnumerator<MeasurementWorkItemMapping> GetEnumerator()
            {
                for (var i = 0; i < _actualCount; i++)
                    yield return CreateMapping(i);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(MeasurementWorkItemMapping item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(MeasurementWorkItemMapping item) => false;
            public void CopyTo(MeasurementWorkItemMapping[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(MeasurementWorkItemMapping item) => throw new NotSupportedException();
        }

        private sealed class UnexpectedOverrunCollection : IReadOnlyCollection<MeasurementWorkItemMapping>
        {
            public int Count => 1;

            public IEnumerator<MeasurementWorkItemMapping> GetEnumerator()
            {
                yield return CreateMapping(0);
                yield return null!;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingMappings : IEnumerable<MeasurementWorkItemMapping>
        {
            private readonly int _count;

            internal StreamingMappings(int count)
            {
                _count = count;
            }

            public IEnumerator<MeasurementWorkItemMapping> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                    yield return CreateMapping(i);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}