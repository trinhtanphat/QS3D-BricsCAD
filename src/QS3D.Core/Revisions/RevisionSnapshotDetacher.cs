using System;
using System.Collections.Generic;

namespace QS3D.Core.Revisions
{
    internal static class RevisionSnapshotDetacher
    {
        private const int MaxElements = 100000;
        private const int MaxEntriesPerCollection = 100000;

        internal static RevisionSnapshot Capture(RevisionSnapshot source, string label)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var detached = new RevisionSnapshot
            {
                Id = source.Id,
                CreatedUtc = source.CreatedUtc,
                ProjectId = source.ProjectId
            };

            var elements = source.Elements;
            var elementCount = elements.Count;
            ValidateCount(elementCount, MaxElements, label + " elements");
            for (var index = 0; index < elementCount; index++)
            {
                if (elements.Count != elementCount)
                    throw Changed(label + " elements", elementCount, elements.Count);

                var element = elements[index];
                if (elements.Count != elementCount)
                    throw Changed(label + " elements", elementCount, elements.Count);
                if (element == null)
                {
                    detached.Elements.Add(null!);
                    continue;
                }

                var copy = new RevisionElementSnapshot
                {
                    ElementId = element.ElementId,
                    Category = element.Category,
                    FamilyId = element.FamilyId,
                    FloorId = element.FloorId,
                    ZoneId = element.ZoneId
                };

                CopyMap(element.Properties, copy.Properties, label + " element " + index + " properties");
                CopyMap(element.Quantities, copy.Quantities, label + " element " + index + " quantities");
                CopyList(element.SourceHandles, copy.SourceHandles, label + " element " + index + " source handles");
                CopyList(element.Dependencies, copy.Dependencies, label + " element " + index + " dependencies");
                detached.Elements.Add(copy);
            }

            if (elements.Count != elementCount)
                throw Changed(label + " elements", elementCount, elements.Count);

            return detached;
        }

        private static void CopyMap<T>(IDictionary<string, T> source, IDictionary<string, T> destination, string label)
        {
            if (source == null) throw new InvalidOperationException("Revision " + label + " collection is null.");
            var expectedCount = source.Count;
            ValidateCount(expectedCount, MaxEntriesPerCollection, label);
            var observed = 0;
            using (var enumerator = source.GetEnumerator())
            {
                if (source.Count != expectedCount)
                    throw Changed(label, expectedCount, source.Count);

                while (true)
                {
                    if (source.Count != expectedCount)
                        throw Changed(label, expectedCount, source.Count);

                    var moved = enumerator.MoveNext();
                    if (source.Count != expectedCount)
                        throw Changed(label, expectedCount, source.Count);
                    if (!moved) break;

                    var pair = enumerator.Current;
                    if (source.Count != expectedCount)
                        throw Changed(label, expectedCount, source.Count);

                    destination.Add(pair.Key, pair.Value);
                    observed++;
                    if (observed > expectedCount)
                        throw Changed(label, expectedCount, observed);
                }
            }

            if (observed != expectedCount || source.Count != expectedCount)
                throw Changed(label, expectedCount, observed);
        }

        private static void CopyList<T>(IList<T> source, IList<T> destination, string label)
        {
            if (source == null) throw new InvalidOperationException("Revision " + label + " collection is null.");
            var expectedCount = source.Count;
            ValidateCount(expectedCount, MaxEntriesPerCollection, label);
            for (var index = 0; index < expectedCount; index++)
            {
                if (source.Count != expectedCount)
                    throw Changed(label, expectedCount, source.Count);
                var item = source[index];
                if (source.Count != expectedCount)
                    throw Changed(label, expectedCount, source.Count);
                destination.Add(item);
            }
            if (source.Count != expectedCount)
                throw Changed(label, expectedCount, source.Count);
        }

        private static void ValidateCount(int count, int maximum, string label)
        {
            if (count < 0)
                throw new InvalidOperationException("Revision " + label + " reported a negative Count.");
            if (count > maximum)
                throw new InvalidOperationException("Revision " + label + " exceeds the supported bound of " + maximum + " entries.");
        }

        private static InvalidOperationException Changed(string label, int expected, int observed) =>
            new InvalidOperationException(
                "Revision " + label + " changed during snapshot capture; expected " + expected + " entries but observed " + observed + ".");
    }
}
