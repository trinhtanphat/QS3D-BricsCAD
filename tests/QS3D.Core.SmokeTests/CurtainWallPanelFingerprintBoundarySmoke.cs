using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallPanelFingerprintBoundarySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CanonicalInputsProduceStableDigest();
            InputOrderSourceKindCaseAndSignedZeroAreCanonical();
            ScalarAndPieceContractsFailClosed();
            NonRepresentablePieceBoundsFailClosed();
            PieceAreaUnderflowFailsClosed();
            CountContractsFailClosedWithoutUnsafeAccess();
        }

        private static void CanonicalInputsProduceStableDigest()
        {
            var digest = CurtainWallPanelFingerprint.Compute(Input(
                Piece(1, 4d, 0d, 2d, 3d),
                Piece(0, 0d, 0d, 4d, 3d)));
            if (digest.Length != 64)
                throw new InvalidOperationException("Curtain panel fingerprint must be a 64-character SHA-256 hex digest.");
            for (var i = 0; i < digest.Length; i++)
            {
                var c = digest[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    throw new InvalidOperationException("Curtain panel fingerprint must use canonical lowercase hex.");
            }
        }

        private static void InputOrderSourceKindCaseAndSignedZeroAreCanonical()
        {
            var first = Piece(0, 0d, 0d, 4d, 3d);
            var second = Piece(1, 4d, 0d, 2d, 3d);
            var forward = Input(first, second);
            var reverse = Input(second, first);
            var upper = Input(first, second);
            upper.SourceKind = "LINE";
            var negativeZero = Input(first, second);
            negativeZero.BottomOffsetM = -0d;

            var expected = CurtainWallPanelFingerprint.Compute(forward);
            Equal(expected, CurtainWallPanelFingerprint.Compute(reverse), "piece ordering must not change fingerprint");
            Equal(expected, CurtainWallPanelFingerprint.Compute(upper), "accepted SourceKind casing must canonicalize");
            Equal(expected, CurtainWallPanelFingerprint.Compute(negativeZero), "signed zero must canonicalize");

            var polyline = Input(first, second);
            polyline.SourceKind = "OpenPolyline";
            polyline.PathSegmentCount = 2;
            var polylineDigest = CurtainWallPanelFingerprint.Compute(polyline);
            if (string.Equals(expected, polylineDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("Different source-kind/path semantics must not alias the Line fingerprint.");
        }

        private static void ScalarAndPieceContractsFailClosed()
        {
            Expect<ArgumentNullException>(() => CurtainWallPanelFingerprint.Compute(null!), "null input");

            var invalidSourceKind = Input(Piece(0, 0d, 0d, 1d, 1d));
            invalidSourceKind.SourceKind = " Line ";
            Expect<ArgumentException>(() => CurtainWallPanelFingerprint.Compute(invalidSourceKind), "padded source kind");

            var invalidLineSegments = Input(Piece(0, 0d, 0d, 1d, 1d));
            invalidLineSegments.PathSegmentCount = 1;
            Expect<ArgumentOutOfRangeException>(() => CurtainWallPanelFingerprint.Compute(invalidLineSegments), "Line path segment count");

            var invalidPolylineSegments = Input(Piece(0, 0d, 0d, 1d, 1d));
            invalidPolylineSegments.SourceKind = "OpenPolyline";
            invalidPolylineSegments.PathSegmentCount = 0;
            Expect<ArgumentOutOfRangeException>(() => CurtainWallPanelFingerprint.Compute(invalidPolylineSegments), "OpenPolyline path segment count");

            var negativeIndex = Input(Piece(-1, 0d, 0d, 1d, 1d));
            Expect<ArgumentOutOfRangeException>(() => CurtainWallPanelFingerprint.Compute(negativeIndex), "negative source panel index");

            var nullPiece = Input(Piece(0, 0d, 0d, 1d, 1d));
            nullPiece.Pieces = new CurtainWallPanelPiece[] { null! };
            Expect<InvalidOperationException>(() => CurtainWallPanelFingerprint.Compute(nullPiece), "null piece");

            var invalidWidth = Input(Piece(0, 0d, 0d, 0d, 1d));
            Expect<ArgumentOutOfRangeException>(() => CurtainWallPanelFingerprint.Compute(invalidWidth), "zero piece width");
        }

        private static void NonRepresentablePieceBoundsFailClosed()
        {
            var lostWidth = Input(Piece(0, 1e308d, 0d, 1d, 1d));
            Expect<OverflowException>(() => CurtainWallPanelFingerprint.Compute(lostWidth), "non-representable right bound");

            var lostHeight = Input(Piece(0, 0d, 1e308d, 1d, 1d));
            Expect<OverflowException>(() => CurtainWallPanelFingerprint.Compute(lostHeight), "non-representable top bound");

            var overflowingWidth = Input(Piece(0, double.MaxValue, 0d, double.MaxValue, 1d));
            Expect<ArgumentOutOfRangeException>(() => CurtainWallPanelFingerprint.Compute(overflowingWidth), "overflowing right bound");
        }

        private static void PieceAreaUnderflowFailsClosed()
        {
            var underflow = Input(Piece(0, 0d, 0d, 1e-200d, 1e-200d));
            Expect<OverflowException>(() => CurtainWallPanelFingerprint.Compute(underflow), "piece area underflow");

            var normalSmall = Input(Piece(0, 0d, 0d, 1e-100d, 1e-100d));
            var digest = CurtainWallPanelFingerprint.Compute(normalSmall);
            if (digest.Length != 64)
                throw new InvalidOperationException("Representable small panel piece must remain fingerprintable.");
        }

        private static void CountContractsFailClosedWithoutUnsafeAccess()
        {
            var negative = new CountProbeList(-1, Array.Empty<CurtainWallPanelPiece>());
            var negativeInput = Input();
            negativeInput.Pieces = negative;
            Expect<InvalidOperationException>(() => CurtainWallPanelFingerprint.Compute(negativeInput), "negative Pieces Count");
            if (negative.IndexReads != 0)
                throw new InvalidOperationException("Negative Pieces Count must fail before index access.");

            var oversized = new CountProbeList(CurtainWallPanelFingerprint.MaxPieces + 1, Array.Empty<CurtainWallPanelPiece>());
            var oversizedInput = Input();
            oversizedInput.Pieces = oversized;
            Expect<InvalidOperationException>(() => CurtainWallPanelFingerprint.Compute(oversizedInput), "oversized Pieces Count");
            if (oversized.IndexReads != 0)
                throw new InvalidOperationException("Oversized Pieces Count must fail before index access.");

            var changing = new ChangingCountList(new[] { Piece(0, 0d, 0d, 1d, 1d) }, firstCount: 1, laterCount: 0);
            var changingInput = Input();
            changingInput.Pieces = changing;
            Expect<InvalidOperationException>(() => CurtainWallPanelFingerprint.Compute(changingInput), "Pieces Count drift");
            if (changing.IndexReads != 0)
                throw new InvalidOperationException("Count-drift control must fail before indexing once the admitted Count changes.");
        }

        private static CurtainWallPanelFingerprintInput Input(params CurtainWallPanelPiece[] pieces)
            => new CurtainWallPanelFingerprintInput
            {
                SourceLengthM = 6d,
                HeightM = 3d,
                BottomOffsetM = 0d,
                PanelDepthM = 0.1d,
                SourceKind = "Line",
                PathSegmentCount = 0,
                Pieces = pieces
            };

        private static CurtainWallPanelPiece Piece(int sourceIndex, double x, double z, double width, double height)
            => new CurtainWallPanelPiece
            {
                SourcePanelIndex = sourceIndex,
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

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + ".");
        }

        private sealed class CountProbeList : IReadOnlyList<CurtainWallPanelPiece>
        {
            private readonly int _count;
            private readonly IReadOnlyList<CurtainWallPanelPiece> _items;

            internal CountProbeList(int count, IReadOnlyList<CurtainWallPanelPiece> items)
            {
                _count = count;
                _items = items;
            }

            internal int IndexReads { get; private set; }
            public int Count => _count;
            public CurtainWallPanelPiece this[int index]
            {
                get
                {
                    IndexReads++;
                    return _items[index];
                }
            }
            public IEnumerator<CurtainWallPanelPiece> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ChangingCountList : IReadOnlyList<CurtainWallPanelPiece>
        {
            private readonly IReadOnlyList<CurtainWallPanelPiece> _items;
            private readonly int _firstCount;
            private readonly int _laterCount;
            private int _countReads;

            internal ChangingCountList(IReadOnlyList<CurtainWallPanelPiece> items, int firstCount, int laterCount)
            {
                _items = items;
                _firstCount = firstCount;
                _laterCount = laterCount;
            }

            internal int IndexReads { get; private set; }
            public int Count => ++_countReads == 1 ? _firstCount : _laterCount;
            public CurtainWallPanelPiece this[int index]
            {
                get
                {
                    IndexReads++;
                    return _items[index];
                }
            }
            public IEnumerator<CurtainWallPanelPiece> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
