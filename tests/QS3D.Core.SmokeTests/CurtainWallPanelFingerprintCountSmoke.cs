using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallPanelFingerprintCountSmoke
    {
        internal static void Run()
        {
            TransientCountDriftFailsBeforeSecondIndexRead();
            StableInputPreservesFingerprintAndSingleIndexerReads();
        }

        private static void TransientCountDriftFailsBeforeSecondIndexRead()
        {
            var pieces = new TransientCountList(
                Piece(0, 0d),
                Piece(1, 1d));

            ExpectInvalidOperation(
                () => CurtainWallPanelFingerprint.Compute(Input(pieces)),
                "Count changed while being validated",
                "Transient fingerprint Pieces Count drift must fail closed.");

            Equal(1, pieces.IndexReads, "Transient Count drift must be rejected before the second indexed read.");
            Equal(3, pieces.CountReads, "Transient Count drift must be detected by the immediate post-index Count rebound.");
        }

        private static void StableInputPreservesFingerprintAndSingleIndexerReads()
        {
            var first = Piece(0, 0d);
            var second = Piece(1, 1d);
            var stable = new StableCountList(first, second);

            var actual = CurtainWallPanelFingerprint.Compute(Input(stable));
            var expected = CurtainWallPanelFingerprint.Compute(Input(new[] { first, second }));

            Equal(expected, actual, "Stable counted input must preserve canonical fingerprint semantics.");
            Equal(2, stable.IndexReads, "Stable input must read each source index exactly once.");
            Equal(6, stable.CountReads, "Stable two-piece input must revalidate Count before/after each index plus final rebound.");
        }

        private static CurtainWallPanelFingerprintInput Input(IReadOnlyList<CurtainWallPanelPiece> pieces) =>
            new CurtainWallPanelFingerprintInput
            {
                SourceLengthM = 2d,
                HeightM = 1d,
                BottomOffsetM = 0d,
                PanelDepthM = 0.1d,
                SourceKind = "Line",
                PathSegmentCount = 0,
                Pieces = pieces
            };

        private static CurtainWallPanelPiece Piece(int sourcePanelIndex, double x) =>
            new CurtainWallPanelPiece
            {
                SourcePanelIndex = sourcePanelIndex,
                X_M = x,
                Z_M = 0d,
                WidthM = 1d,
                HeightM = 1d
            };

        private static void ExpectInvalidOperation(Action action, string diagnosticFragment, string failureMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(diagnosticFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception(failureMessage + " Actual diagnostic: " + ex.Message);
                return;
            }

            throw new Exception(failureMessage);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class TransientCountList : IReadOnlyList<CurtainWallPanelPiece>
        {
            private readonly CurtainWallPanelPiece[] _items;
            private int _count = 2;

            public TransientCountList(params CurtainWallPanelPiece[] items) => _items = items;

            public int CountReads { get; private set; }
            public int IndexReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _count;
                }
            }

            public CurtainWallPanelPiece this[int index]
            {
                get
                {
                    IndexReads++;
                    if (index == 0)
                        _count = 3;
                    else
                        _count = 2;
                    return _items[index];
                }
            }

            public IEnumerator<CurtainWallPanelPiece> GetEnumerator() => ((IEnumerable<CurtainWallPanelPiece>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StableCountList : IReadOnlyList<CurtainWallPanelPiece>
        {
            private readonly CurtainWallPanelPiece[] _items;

            public StableCountList(params CurtainWallPanelPiece[] items) => _items = items;

            public int CountReads { get; private set; }
            public int IndexReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _items.Length;
                }
            }

            public CurtainWallPanelPiece this[int index]
            {
                get
                {
                    IndexReads++;
                    return _items[index];
                }
            }

            public IEnumerator<CurtainWallPanelPiece> GetEnumerator() => ((IEnumerable<CurtainWallPanelPiece>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class CurtainWallPanelFingerprintCountSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainWallPanelFingerprintCountSmoke.Run();
    }
}
