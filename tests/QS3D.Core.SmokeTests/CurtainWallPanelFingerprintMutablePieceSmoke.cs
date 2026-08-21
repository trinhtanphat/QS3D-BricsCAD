using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallPanelFingerprintMutablePieceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PostValidationMutationUsesValidatedSnapshot();
            StablePieceOrderingRemainsCanonical();
            CountDriftStillFailsClosed();
        }

        private static void PostValidationMutationUsesValidatedSnapshot()
        {
            var expected = CurtainWallPanelFingerprint.Compute(Input(new[]
            {
                Piece(0, 0d, 0d, 2d, 3d),
                Piece(1, 2d, 0d, 1d, 3d)
            }));

            var first = Piece(0, 0d, 0d, 2d, 3d);
            var second = Piece(1, 2d, 0d, 1d, 3d);
            var pieces = new MutatingPieceList(first, second, () =>
            {
                first.SourcePanelIndex = -7;
                first.X_M = double.NaN;
                first.WidthM = double.PositiveInfinity;
            });

            var actual = CurtainWallPanelFingerprint.Compute(Input(pieces));
            Equal(expected, actual, "post-validation mutation must not alter the validated fingerprint snapshot");
            Equal(2, pieces.IndexReads, "fingerprint should read each source piece once");
        }

        private static void StablePieceOrderingRemainsCanonical()
        {
            var ordered = CurtainWallPanelFingerprint.Compute(Input(new[]
            {
                Piece(0, 0d, 0d, 2d, 3d),
                Piece(1, 2d, 0d, 1d, 3d)
            }));
            var reversed = CurtainWallPanelFingerprint.Compute(Input(new[]
            {
                Piece(1, 2d, 0d, 1d, 3d),
                Piece(0, 0d, 0d, 2d, 3d)
            }));

            Equal(ordered, reversed, "piece input order must remain canonicalized by semantic piece coordinates");
        }

        private static void CountDriftStillFailsClosed()
        {
            var pieces = new ChangingCountList<CurtainWallPanelPiece>(
                new[] { Piece(0, 0d, 0d, 2d, 3d) },
                firstCount: 1,
                laterCount: 2);

            Expect<InvalidOperationException>(() => CurtainWallPanelFingerprint.Compute(Input(pieces)), "piece Count drift");
            Equal(1, pieces.IndexReads, "Count-drift validation must not expand the snapshotted input range");
        }

        private static CurtainWallPanelFingerprintInput Input(IReadOnlyList<CurtainWallPanelPiece> pieces)
            => new CurtainWallPanelFingerprintInput
            {
                SourceLengthM = 3d,
                HeightM = 3d,
                BottomOffsetM = 0d,
                PanelDepthM = 0.05d,
                SourceKind = "Line",
                PathSegmentCount = 0,
                Pieces = pieces
            };

        private static CurtainWallPanelPiece Piece(int sourcePanelIndex, double x, double z, double width, double height)
            => new CurtainWallPanelPiece
            {
                SourcePanelIndex = sourcePanelIndex,
                X_M = x,
                Z_M = z,
                WidthM = width,
                HeightM = height
            };

        private static void Expect<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException(label + " must fail with " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private sealed class MutatingPieceList : IReadOnlyList<CurtainWallPanelPiece>
        {
            private readonly CurtainWallPanelPiece _first;
            private readonly CurtainWallPanelPiece _second;
            private readonly Action _mutateFirst;

            internal MutatingPieceList(CurtainWallPanelPiece first, CurtainWallPanelPiece second, Action mutateFirst)
            {
                _first = first;
                _second = second;
                _mutateFirst = mutateFirst;
            }

            internal int IndexReads { get; private set; }
            public int Count => 2;
            public CurtainWallPanelPiece this[int index]
            {
                get
                {
                    IndexReads++;
                    if (index == 0) return _first;
                    if (index == 1)
                    {
                        _mutateFirst();
                        return _second;
                    }
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            public IEnumerator<CurtainWallPanelPiece> GetEnumerator()
            {
                yield return _first;
                yield return _second;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ChangingCountList<T> : IReadOnlyList<T>
        {
            private readonly IReadOnlyList<T> _items;
            private readonly int _firstCount;
            private readonly int _laterCount;

            internal ChangingCountList(IReadOnlyList<T> items, int firstCount, int laterCount)
            {
                _items = items;
                _firstCount = firstCount;
                _laterCount = laterCount;
            }

            internal int CountReads { get; private set; }
            internal int IndexReads { get; private set; }
            public int Count => ++CountReads == 1 ? _firstCount : _laterCount;
            public T this[int index]
            {
                get
                {
                    IndexReads++;
                    return _items[index];
                }
            }

            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
