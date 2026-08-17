using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class ClashDetectionBoundSmoke
    {
        private const int MaximumElements = 500;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        public static void Run()
        {
            KnownCountOversizeRejectsBeforeEnumeration();
            StreamingOversizeStopsAtFirstDisallowedElement();
            ExactBoundaryRemainsAccepted();
            OrdinaryClashSemanticsRemainStable();
            ExistingValidationRemainsStable();
        }

        private static void KnownCountOversizeRejectsBeforeEnumeration()
        {
            var source = new KnownCountCollection(MaximumElements + 1);

            Throws<InvalidOperationException>(() => new ClashDetectionService().Detect(source));

            Equal(0, source.EnumerationAttempts, "Known-count oversized coordination input was enumerated.");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedElement()
        {
            var counter = new ProductionCounter();

            Throws<InvalidOperationException>(() =>
                new ClashDetectionService().Detect(Stream(MaximumElements + 2, counter)));

            Equal(
                MaximumElements + 1,
                counter.Produced,
                "Coordination streaming bound requested element 502 or later.");
        }

        private static void ExactBoundaryRemainsAccepted()
        {
            var elements = new List<CoordinationElement>(MaximumElements);
            for (var index = 0; index < MaximumElements; index++)
                elements.Add(Element("E" + index.ToString("D3"), "Architecture", 0d, 0d, 0d, 1d, 1d, 1d));

            var results = new ClashDetectionService().Detect(elements);

            Equal(0, results.Count, "Exact 500-element coordination boundary changed same-discipline filtering.");
        }

        private static void OrdinaryClashSemanticsRemainStable()
        {
            var service = new ClashDetectionService();
            var right = Element("B", "MEP", 1d, 1d, 1d, 3d, 3d, 3d);
            var left = Element("A", "Architecture", 0d, 0d, 0d, 2d, 2d, 2d);

            var hard = service.Detect(new[] { right, left });
            Equal(1, hard.Count, "Ordinary hard-clash count changed.");
            Equal("A", hard[0].LeftElementId, "Hard-clash deterministic left ordering changed.");
            Equal("B", hard[0].RightElementId, "Hard-clash deterministic right ordering changed.");
            Equal(ClashKind.Hard, hard[0].Kind, "Hard-clash kind changed.");
            Equal(1d, hard[0].OverlapXM, "Hard-clash X overlap changed.");
            Equal(1d, hard[0].OverlapYM, "Hard-clash Y overlap changed.");
            Equal(1d, hard[0].OverlapZM, "Hard-clash Z overlap changed.");

            var sameA = Element("S1", "Architecture", 0d, 0d, 0d, 2d, 2d, 2d);
            var sameB = Element("S2", "architecture", 1d, 1d, 1d, 3d, 3d, 3d);
            Equal(0, service.Detect(new[] { sameA, sameB }).Count, "Default same-discipline filtering changed.");
            Equal(
                1,
                service.Detect(new[] { sameA, sameB }, includeSameDiscipline: true).Count,
                "Explicit same-discipline clash detection changed.");

            var clearanceLeft = Element("C", "Architecture", 0d, 0d, 0d, 1d, 1d, 1d);
            var clearanceRight = Element("D", "MEP", 1.25d, 0d, 0d, 2d, 1d, 1d);
            var clearance = service.Detect(new[] { clearanceRight, clearanceLeft }, clearanceM: 0.25d);
            Equal(1, clearance.Count, "Ordinary clearance-clash count changed.");
            Equal(ClashKind.Clearance, clearance[0].Kind, "Clearance-clash kind changed.");
            Equal(0.25d, clearance[0].SeparationM, "Clearance separation changed.");
        }

        private static void ExistingValidationRemainsStable()
        {
            var service = new ClashDetectionService();
            Throws<ArgumentNullException>(() => service.Detect(null!));
            Throws<ArgumentOutOfRangeException>(() => service.Detect(Array.Empty<CoordinationElement>(), -0.01d));
            Throws<ArgumentException>(() => service.Detect(new CoordinationElement[] { null! }));

            var duplicateA = Element("duplicate", "Architecture", 0d, 0d, 0d, 1d, 1d, 1d);
            var duplicateB = Element("DUPLICATE", "MEP", 2d, 2d, 2d, 3d, 3d, 3d);
            Throws<ArgumentException>(() => service.Detect(new[] { duplicateA, duplicateB }));
        }

        private static IEnumerable<CoordinationElement> Stream(int count, ProductionCounter counter)
        {
            for (var index = 0; index < count; index++)
            {
                counter.Produced++;
                yield return Element("STREAM-" + index.ToString("D3"), "Architecture", 0d, 0d, 0d, 1d, 1d, 1d);
            }
        }

        private static CoordinationElement Element(
            string id,
            string discipline,
            double minX,
            double minY,
            double minZ,
            double maxX,
            double maxY,
            double maxZ)
        {
            return new CoordinationElement(
                id,
                discipline,
                "Generic",
                "System",
                "Region",
                new AxisAlignedBox(minX, minY, minZ, maxX, maxY, maxZ));
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected: " + expected + "; actual: " + actual + ".");
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

        private sealed class ProductionCounter
        {
            internal int Produced { get; set; }
        }

        private sealed class KnownCountCollection : ICollection<CoordinationElement>
        {
            internal KnownCountCollection(int count)
            {
                Count = count;
            }

            internal int EnumerationAttempts { get; private set; }
            public int Count { get; }
            public bool IsReadOnly => true;

            public IEnumerator<CoordinationElement> GetEnumerator()
            {
                EnumerationAttempts++;
                throw new InvalidOperationException("Oversized known-count input must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(CoordinationElement item) => false;
            public void CopyTo(CoordinationElement[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(CoordinationElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(CoordinationElement item) => throw new NotSupportedException();
        }
    }
}
