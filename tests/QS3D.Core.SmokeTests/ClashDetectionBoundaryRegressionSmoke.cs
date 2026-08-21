using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class ClashDetectionBoundaryRegressionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            HardClearanceAndTouchingMatrix();
            OrderingAndSameDisciplineAreDeterministic();
            ElementBoundariesAndTraversalAreFailClosed();
            KnownCountContractsAreFailClosed();
            ResultBoundaryIsFailClosed();
            ExtremeFiniteCoordinatesAreFailClosed();
            InvalidRowsAreRejected();
        }

        private static void HardClearanceAndTouchingMatrix()
        {
            var service = new ClashDetectionService();
            var hard = service.Detect(new[]
            {
                E("B", "MEP", B(0, 0, 0, 2, 2, 2)),
                E("A", "STR", B(1, 1, 1, 3, 3, 3))
            });
            Require(hard.Count == 1 && hard[0].Kind == ClashKind.Hard, "Positive 3-axis overlap must be Hard.");
            Require(hard[0].LeftElementId == "A" && hard[0].RightElementId == "B", "Hard result IDs must be sorted.");
            Require(hard[0].SeparationM == 0d && hard[0].OverlapXM == 1d && hard[0].OverlapYM == 1d && hard[0].OverlapZM == 1d,
                "Hard overlap evidence mismatch.");

            var face = new[] { E("A", "STR", B(0, 0, 0, 1, 1, 1)), E("B", "MEP", B(1, 0, 0, 2, 1, 1)) };
            Require(service.Detect(face).Count == 0, "Face touch is not positive-volume hard overlap at zero clearance.");
            AssertZeroClearanceTouch(service.Detect(face, 0.01d), "face");
            AssertZeroClearanceTouch(service.Detect(new[]
            {
                E("A", "STR", B(0, 0, 0, 1, 1, 1)), E("B", "MEP", B(1, 1, 0, 2, 2, 1))
            }, 0.01d), "edge");
            AssertZeroClearanceTouch(service.Detect(new[]
            {
                E("A", "STR", B(0, 0, 0, 1, 1, 1)), E("B", "MEP", B(1, 1, 1, 2, 2, 2))
            }, 0.01d), "point");

            var diagonalPair = new[]
            {
                E("A", "STR", B(0, 0, 0, 1, 1, 1)), E("B", "MEP", B(1.3d, 1.4d, 1d, 2, 2, 2))
            };
            var diagonal = service.Detect(diagonalPair, 0.5d);
            Require(diagonal.Count == 1 && Math.Abs(diagonal[0].SeparationM - 0.5d) <= 1e-14d,
                "Scaled Euclidean distance must preserve the 3-4-5 threshold control.");
            Require(service.Detect(diagonalPair, 0.499999d).Count == 0, "Beyond-clearance pair must remain excluded.");
        }

        private static void OrderingAndSameDisciplineAreDeterministic()
        {
            var service = new ClashDetectionService();
            var input = new[]
            {
                E("z", "STR", B(0, 0, 0, 2, 2, 2)),
                E("B", "MEP", B(1, 1, 1, 3, 3, 3)),
                E("a", "MEP", B(1, 1, 1, 3, 3, 3))
            };
            var first = service.Detect(input);
            Require(first.Count == 2, "Default detection must exclude only the same-discipline pair.");
            Require(first[0].LeftElementId == "a" && first[0].RightElementId == "z", "First canonical result mismatch.");
            Require(first[1].LeftElementId == "B" && first[1].RightElementId == "z", "Second canonical result mismatch.");
            var reversed = service.Detect(Enumerable.Reverse(input).ToArray());
            Require(Signature(first) == Signature(reversed), "Traversal order must not affect semantic result ordering.");
            var same = service.Detect(input, includeSameDiscipline: true);
            Require(same.Count == 3 && same[0].LeftElementId == "a" && same[0].RightElementId == "B",
                "Explicit same-discipline inclusion must restore the canonical MEP pair.");
        }

        private static void ElementBoundariesAndTraversalAreFailClosed()
        {
            var service = new ClashDetectionService();
            var exact = Enumerable.Range(0, 500)
                .Select(i => E("E" + i.ToString("D3"), i % 2 == 0 ? "STR" : "MEP", B(i * 10d, 0, 0, i * 10d + 1d, 1, 1)))
                .ToArray();
            Require(service.Detect(exact).Count == 0, "Exactly 500 non-overlapping elements must remain supported.");

            var known = new OversizeCollection(501);
            Expect<InvalidOperationException>(() => service.Detect(known), "Known 501-element collection must be rejected.");
            Require(!known.WasEnumerated, "Known oversize collection must be rejected before enumeration.");

            var lazy = new CountingEnumerable(501);
            Expect<InvalidOperationException>(() => service.Detect(lazy), "Lazy boundary+1 input must be rejected.");
            Require(lazy.YieldCount == 501, "Lazy oversize traversal must stop exactly at boundary+1.");
        }

        private static void KnownCountContractsAreFailClosed()
        {
            var service = new ClashDetectionService();
            Expect<InvalidOperationException>(() => service.Detect(new ConflictingCounts()), "Conflicting known counts must fail closed.");
            Expect<InvalidOperationException>(() => service.Detect(new AdvertisedCount(2, 1)), "Advertised-vs-observed mismatch must fail closed.");
        }

        private static void ResultBoundaryIsFailClosed()
        {
            var service = new ClashDetectionService();
            var within = Enumerable.Range(0, 141).Select(i => E("R" + i.ToString("D3"), "D" + i, B(0, 0, 0, 1, 1, 1))).ToArray();
            Require(service.Detect(within).Count == 9870, "141 all-overlapping elements must remain below 10,000 results.");
            var over = Enumerable.Range(0, 142).Select(i => E("R" + i.ToString("D3"), "D" + i, B(0, 0, 0, 1, 1, 1))).ToArray();
            Expect<InvalidOperationException>(() => service.Detect(over), "The 10,001st result must be refused.");
        }

        private static void ExtremeFiniteCoordinatesAreFailClosed()
        {
            var service = new ClashDetectionService();
            var extremeOverlap = service.Detect(new[]
            {
                E("A", "STR", B(1e300d, 1e300d, 1e300d, 1.000000000000001e300d, 1.000000000000001e300d, 1.000000000000001e300d)),
                E("B", "MEP", B(1e300d, 1e300d, 1e300d, 1.0000000000000005e300d, 1.0000000000000005e300d, 1.0000000000000005e300d))
            });
            Require(extremeOverlap.Count == 1 && extremeOverlap[0].Kind == ClashKind.Hard && IsFinite(extremeOverlap[0].OverlapXM) && extremeOverlap[0].OverlapXM > 0d,
                "Representable extreme finite overlap must remain finite and deterministic.");

            Expect<OverflowException>(() => service.Detect(new[]
            {
                E("A", "STR", B(-1e308d, 0, 0, -9e307d, 1, 1)), E("B", "MEP", B(9e307d, 0, 0, 1e308d, 1, 1))
            }, double.MaxValue), "Unrepresentable finite subtraction must fail closed instead of leaking Infinity/NaN.");

            Expect<InvalidOperationException>(() => service.Detect(new[]
            {
                E("A", "STR", B(0, 0, 0, 1e300d, 0d, 1d)),
                E("B", "MEP", B(1e300d + 1e284d, 1e-100d, 0, 2e300d, 1d, 1d))
            }, 1e300d), "A non-zero orthogonal gap lost at double precision must fail closed.");

            Expect<ArgumentOutOfRangeException>(() => service.Detect(Array.Empty<CoordinationElement>(), double.PositiveInfinity), "Infinite clearance must be rejected.");
            Expect<ArgumentOutOfRangeException>(() => new AxisAlignedBox(double.NaN, 0, 0, 1, 1, 1), "NaN coordinates must be rejected.");
        }

        private static void InvalidRowsAreRejected()
        {
            var service = new ClashDetectionService();
            Expect<ArgumentException>(() => service.Detect(new[]
            {
                E("A", "STR", B(0, 0, 0, 1, 1, 1)), E("a", "MEP", B(2, 0, 0, 3, 1, 1))
            }), "IDs must remain unique case-insensitively.");
            Expect<ArgumentException>(() => service.Detect(new CoordinationElement[] { E("A", "STR", B(0, 0, 0, 1, 1, 1)), null! }), "Null rows must be rejected.");
        }

        private static void AssertZeroClearanceTouch(IReadOnlyList<ClashResult> results, string label)
            => Require(results.Count == 1 && results[0].Kind == ClashKind.Clearance && results[0].SeparationM == 0d,
                label + " touching must be zero-separation Clearance when clearance review is enabled.");

        private static CoordinationElement E(string id, string discipline, AxisAlignedBox bounds)
            => new CoordinationElement(id, discipline, "Category", "System", "Region", bounds);
        private static AxisAlignedBox B(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
            => new AxisAlignedBox(minX, minY, minZ, maxX, maxY, maxZ);
        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static string Signature(IReadOnlyList<ClashResult> results)
            => string.Join("|", results.Select(r => r.LeftElementId + ">" + r.RightElementId + ":" + r.Kind + ":" + r.SeparationM));
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Expect<T>(Action action, string message) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new InvalidOperationException(message);
        }

        private sealed class OversizeCollection : ICollection<CoordinationElement>
        {
            private readonly int _count;
            internal OversizeCollection(int count) => _count = count;
            internal bool WasEnumerated { get; private set; }
            public int Count => _count;
            public bool IsReadOnly => true;
            public IEnumerator<CoordinationElement> GetEnumerator() { WasEnumerated = true; throw new InvalidOperationException("must not enumerate"); }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(CoordinationElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(CoordinationElement item) => false;
            public void CopyTo(CoordinationElement[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(CoordinationElement item) => throw new NotSupportedException();
        }

        private sealed class CountingEnumerable : IEnumerable<CoordinationElement>
        {
            private readonly int _count;
            internal CountingEnumerable(int count) => _count = count;
            internal int YieldCount { get; private set; }
            public IEnumerator<CoordinationElement> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldCount++;
                    yield return E("L" + i.ToString("D3"), i % 2 == 0 ? "STR" : "MEP", B(i * 10d, 0, 0, i * 10d + 1d, 1, 1));
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ConflictingCounts : ICollection<CoordinationElement>, IReadOnlyCollection<CoordinationElement>, ICollection
        {
            int ICollection<CoordinationElement>.Count => 1;
            int IReadOnlyCollection<CoordinationElement>.Count => 2;
            int ICollection.Count => 3;
            bool ICollection<CoordinationElement>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            public IEnumerator<CoordinationElement> GetEnumerator() => Enumerable.Empty<CoordinationElement>().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<CoordinationElement>.Add(CoordinationElement item) => throw new NotSupportedException();
            void ICollection<CoordinationElement>.Clear() => throw new NotSupportedException();
            bool ICollection<CoordinationElement>.Contains(CoordinationElement item) => false;
            void ICollection<CoordinationElement>.CopyTo(CoordinationElement[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<CoordinationElement>.Remove(CoordinationElement item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class AdvertisedCount : ICollection<CoordinationElement>
        {
            private readonly int _advertised;
            private readonly int _observed;
            internal AdvertisedCount(int advertised, int observed) { _advertised = advertised; _observed = observed; }
            public int Count => _advertised;
            public bool IsReadOnly => true;
            public IEnumerator<CoordinationElement> GetEnumerator()
            {
                for (var i = 0; i < _observed; i++) yield return E("C" + i, i % 2 == 0 ? "STR" : "MEP", B(i * 2d, 0, 0, i * 2d + 1d, 1, 1));
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(CoordinationElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(CoordinationElement item) => false;
            public void CopyTo(CoordinationElement[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(CoordinationElement item) => throw new NotSupportedException();
        }
    }
}
