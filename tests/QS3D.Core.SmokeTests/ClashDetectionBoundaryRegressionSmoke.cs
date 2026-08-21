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
            SameDisciplineAndOrderingAreDeterministic();
            ElementCountBoundariesAreFailClosed();
            KnownCountContractsAreFailClosed();
            ResultCountBoundaryIsFailClosed();
            ExtremeFiniteCoordinatesAreFailClosedWithoutSilentNumericCorruption();
            DuplicateAndNullInputsAreRejected();
        }

        private static void HardClearanceAndTouchingMatrix()
        {
            var service = new ClashDetectionService();
            var hard = service.Detect(new[]
            {
                Element("B", "MEP", Box(0, 0, 0, 2, 2, 2)),
                Element("A", "STR", Box(1, 1, 1, 3, 3, 3))
            });
            Require(hard.Count == 1, "Positive 3-axis overlap must produce one hard clash.");
            Require(hard[0].Kind == ClashKind.Hard, "Positive 3-axis overlap must be Hard.");
            Require(hard[0].LeftElementId == "A" && hard[0].RightElementId == "B", "Hard-clash IDs must use deterministic sorted order.");
            Require(hard[0].SeparationM == 0d, "Hard clash separation must be zero.");
            Require(hard[0].OverlapXM == 1d && hard[0].OverlapYM == 1d && hard[0].OverlapZM == 1d, "Hard overlap extents must be exact for the control geometry.");

            var faceTouch = new[]
            {
                Element("A", "STR", Box(0, 0, 0, 1, 1, 1)),
                Element("B", "MEP", Box(1, 0, 0, 2, 1, 1))
            };
            Require(service.Detect(faceTouch).Count == 0, "Face touching is not positive-volume hard overlap at zero clearance.");
            var faceClearance = service.Detect(faceTouch, 0.01d);
            Require(faceClearance.Count == 1 && faceClearance[0].Kind == ClashKind.Clearance && faceClearance[0].SeparationM == 0d,
                "Face touching must be represented as a zero-separation clearance clash when clearance review is requested.");

            var edgeTouch = service.Detect(new[]
            {
                Element("A", "STR", Box(0, 0, 0, 1, 1, 1)),
                Element("B", "MEP", Box(1, 1, 0, 2, 2, 1))
            }, 0.01d);
            Require(edgeTouch.Count == 1 && edgeTouch[0].Kind == ClashKind.Clearance && edgeTouch[0].SeparationM == 0d,
                "Edge touching must be a zero-separation clearance clash, not Hard.");

            var pointTouch = service.Detect(new[]
            {
                Element("A", "STR", Box(0, 0, 0, 1, 1, 1)),
                Element("B", "MEP", Box(1, 1, 1, 2, 2, 2))
            }, 0.01d);
            Require(pointTouch.Count == 1 && pointTouch[0].Kind == ClashKind.Clearance && pointTouch[0].SeparationM == 0d,
                "Point touching must be a zero-separation clearance clash, not Hard.");

            var diagonal = service.Detect(new[]
            {
                Element("A", "STR", Box(0, 0, 0, 1, 1, 1)),
                Element("B", "MEP", Box(1.3d, 1.4d, 1d, 2d, 2d, 2d))
            }, 0.5d);
            Require(diagonal.Count == 1, "3-4-5 clearance control must be detected at the exact threshold.");
            Require(Math.Abs(diagonal[0].SeparationM - 0.5d) <= 1e-14d, "Scaled Euclidean separation must preserve the 3-4-5 control distance.");
            Require(service.Detect(new[]
            {
                Element("A", "STR", Box(0, 0, 0, 1, 1, 1)),
                Element("B", "MEP", Box(1.3d, 1.4d, 1d, 2d, 2d, 2d))
            }, 0.499999d).Count == 0, "A pair beyond the requested clearance must remain excluded.");
        }

        private static void SameDisciplineAndOrderingAreDeterministic()
        {
            var service = new ClashDetectionService();
            var input = new[]
            {
                Element("z", "STR", Box(0, 0, 0, 2, 2, 2)),
                Element("B", "MEP", Box(1, 1, 1, 3, 3, 3)),
                Element("a", "MEP", Box(1, 1, 1, 3, 3, 3))
            };

            var defaultResults = service.Detect(input);
            Require(defaultResults.Count == 2, "Default detection must exclude the same-discipline MEP pair only.");
            Require(defaultResults[0].LeftElementId == "a" && defaultResults[0].RightElementId == "z", "First result must follow canonical element ordering.");
            Require(defaultResults[1].LeftElementId == "B" && defaultResults[1].RightElementId == "z", "Second result must follow canonical element ordering.");

            var reversed = service.Detect(input.Reverse().ToArray());
            Require(Signature(defaultResults) == Signature(reversed), "Input traversal order must not change semantic clash ordering.");

            var withSame = service.Detect(input, includeSameDiscipline: true);
            Require(withSame.Count == 3, "Explicit same-discipline inclusion must restore the MEP/MEP pair.");
            Require(withSame[0].LeftElementId == "a" && withSame[0].RightElementId == "B", "Same-discipline result must retain canonical ID ordering.");
        }

        private static void ElementCountBoundariesAreFailClosed()
        {
            var service = new ClashDetectionService();
            var exact = Enumerable.Range(0, 500)
                .Select(i => Element("E" + i.ToString("D3"), i % 2 == 0 ? "STR" : "MEP", Box(i * 10d, 0, 0, i * 10d + 1d, 1, 1)))
                .ToArray();
            Require(service.Detect(exact).Count == 0, "Exactly 500 non-overlapping elements must remain supported.");

            var knownOversize = new ThrowingOversizeCollection(501);
            Expect<InvalidOperationException>(() => service.Detect(knownOversize), "Known 501-element input must be rejected before enumeration.");
            Require(!knownOversize.WasEnumerated, "Known oversize rejection must occur before traversal.");

            var lazy = new CountingEnumerable(501);
            Expect<InvalidOperationException>(() => service.Detect(lazy), "Lazy 501-element input must stop at boundary+1.");
            Require(lazy.YieldCount == 501, "Lazy oversize detection must consume boundary+1 and must not over-read.");
        }

        private static void KnownCountContractsAreFailClosed()
        {
            var service = new ClashDetectionService();
            Expect<InvalidOperationException>(() => service.Detect(new ConflictingCountCollection()), "Conflicting generic/read-only/non-generic counts must fail closed.");
            Expect<InvalidOperationException>(() => service.Detect(new AdvertisedCountCollection(2, 1)), "Advertised-vs-observed count mismatch must fail closed.");
        }

        private static void ResultCountBoundaryIsFailClosed()
        {
            var service = new ClashDetectionService();
            var within = Enumerable.Range(0, 141)
                .Select(i => Element("R" + i.ToString("D3"), "D" + i, Box(0, 0, 0, 1, 1, 1)))
                .ToArray();
            Require(service.Detect(within).Count == 9870, "141 all-overlapping distinct-discipline elements must remain below the 10,000-result boundary.");

            var over = Enumerable.Range(0, 142)
                .Select(i => Element("R" + i.ToString("D3"), "D" + i, Box(0, 0, 0, 1, 1, 1)))
                .ToArray();
            Expect<InvalidOperationException>(() => service.Detect(over), "The 10,001st result must be refused rather than materialized.");
        }

        private static void ExtremeFiniteCoordinatesAreFailClosedWithoutSilentNumericCorruption()
        {
            var service = new ClashDetectionService();
            var overlappingExtreme = service.Detect(new[]
            {
                Element("A", "STR", Box(1e300d, 1e300d, 1e300d, 1.000000000000001e300d, 1.000000000000001e300d, 1.000000000000001e300d)),
                Element("B", "MEP", Box(1e300d, 1e300d, 1e300d, 1.0000000000000005e300d, 1.0000000000000005e300d, 1.0000000000000005e300d))
            });
            Require(overlappingExtreme.Count == 1 && overlappingExtreme[0].Kind == ClashKind.Hard,
                "Representable extreme finite overlap must remain deterministic and finite.");
            Require(IsFinite(overlappingExtreme[0].OverlapXM) && overlappingExtreme[0].OverlapXM > 0d,
                "Extreme overlap evidence must remain finite and positive.");

            Expect<OverflowException>(() => service.Detect(new[]
            {
                Element("A", "STR", Box(-1e308d, 0, 0, -9e307d, 1, 1)),
                Element("B", "MEP", Box(9e307d, 0, 0, 1e308d, 1, 1))
            }, double.MaxValue), "Unrepresentable finite-coordinate subtraction must fail closed instead of producing Infinity/NaN evidence.");

            Expect<InvalidOperationException>(() => service.Detect(new[]
            {
                Element("A", "STR", Box(0, 0, 0, 1e300d, 1, 1)),
                Element("B", "MEP", Box(1e300d + 1e284d, 1e-100d, 0, 2e300d, 1, 1))
            }, 1e300d), "A non-zero orthogonal gap lost at double precision must fail closed.");

            Expect<ArgumentOutOfRangeException>(() => service.Detect(Array.Empty<CoordinationElement>(), double.PositiveInfinity), "Infinite clearance must be rejected.");
            Expect<ArgumentOutOfRangeException>(() => new AxisAlignedBox(double.NaN, 0, 0, 1, 1, 1), "NaN coordinates must be rejected.");
        }

        private static void DuplicateAndNullInputsAreRejected()
        {
            var service = new ClashDetectionService();
            Expect<ArgumentException>(() => service.Detect(new[]
            {
                Element("A", "STR", Box(0, 0, 0, 1, 1, 1)),
                Element("a", "MEP", Box(2, 0, 0, 3, 1, 1))
            }), "Element IDs must remain unique case-insensitively.");

            Expect<ArgumentException>(() => service.Detect(new CoordinationElement[]
            {
                Element("A", "STR", Box(0, 0, 0, 1, 1, 1)),
                null!
            }), "Null rows must be rejected.");
        }

        private static CoordinationElement Element(string id, string discipline, AxisAlignedBox bounds)
            => new CoordinationElement(id, discipline, "Category", "System", "Region", bounds);

        private static AxisAlignedBox Box(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
            => new AxisAlignedBox(minX, minY, minZ, maxX, maxY, maxZ);

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static string Signature(IReadOnlyList<ClashResult> results)
            => string.Join("|", results.Select(r => r.LeftElementId + ">" + r.RightElementId + ":" + r.Kind + ":" + r.SeparationM));

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Expect<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private sealed class ThrowingOversizeCollection : ICollection<CoordinationElement>
        {
            private readonly int _count;
            internal ThrowingOversizeCollection(int count) => _count = count;
            internal bool WasEnumerated { get; private set; }
            public int Count => _count;
            public bool IsReadOnly => true;
            public IEnumerator<CoordinationElement> GetEnumerator() { WasEnumerated = true; throw new InvalidOperationException("Must not enumerate."); }
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
                    yield return Element("L" + i.ToString("D3"), i % 2 == 0 ? "STR" : "MEP", Box(i * 10d, 0, 0, i * 10d + 1d, 1, 1));
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ConflictingCountCollection : ICollection<CoordinationElement>, IReadOnlyCollection<CoordinationElement>, ICollection
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

        private sealed class AdvertisedCountCollection : ICollection<CoordinationElement>
        {
            private readonly int _advertised;
            private readonly int _observed;
            internal AdvertisedCountCollection(int advertised, int observed) { _advertised = advertised; _observed = observed; }
            public int Count => _advertised;
            public bool IsReadOnly => true;
            public IEnumerator<CoordinationElement> GetEnumerator()
            {
                for (var i = 0; i < _observed; i++)
                    yield return Element("C" + i, i % 2 == 0 ? "STR" : "MEP", Box(i * 2d, 0, 0, i * 2d + 1d, 1, 1));
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
