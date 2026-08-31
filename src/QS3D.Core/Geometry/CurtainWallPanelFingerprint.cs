using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.Geometry
{
    public sealed class CurtainWallPanelFingerprintInput
    {
        public double SourceLengthM { get; set; }
        public double HeightM { get; set; }
        public double BottomOffsetM { get; set; }
        public double PanelDepthM { get; set; }
        public string SourceKind { get; set; } = "Line";
        public int PathSegmentCount { get; set; }
        public IReadOnlyList<CurtainWallPanelPiece> Pieces { get; set; } = Array.Empty<CurtainWallPanelPiece>();
    }

    public static class CurtainWallPanelFingerprint
    {
        public const int MaxPieces = CurtainWallOpeningPanelPlanner.MaxOutputPieces;

        public static string Compute(CurtainWallPanelFingerprintInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var sourceLengthM = input.SourceLengthM;
            var heightM = input.HeightM;
            var bottomOffsetM = input.BottomOffsetM;
            var panelDepthM = input.PanelDepthM;
            var sourceKind = input.SourceKind ?? string.Empty;
            var pathSegmentCount = input.PathSegmentCount;
            var inputPieces = input.Pieces;

            Positive(sourceLengthM, nameof(input.SourceLengthM));
            Positive(heightM, nameof(input.HeightM));
            Finite(bottomOffsetM, nameof(input.BottomOffsetM));
            Positive(panelDepthM, nameof(input.PanelDepthM));
            if (!string.Equals(sourceKind, sourceKind.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Curtain panel source kind must not contain leading or trailing whitespace.", nameof(input.SourceKind));
            if (!string.Equals(sourceKind, "Line", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(sourceKind, "OpenPolyline", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Curtain panel source kind must be Line or OpenPolyline.", nameof(input.SourceKind));
            if (pathSegmentCount < 0 ||
                (string.Equals(sourceKind, "Line", StringComparison.OrdinalIgnoreCase) && pathSegmentCount != 0) ||
                (string.Equals(sourceKind, "OpenPolyline", StringComparison.OrdinalIgnoreCase) && pathSegmentCount < 1))
                throw new ArgumentOutOfRangeException(nameof(input.PathSegmentCount));
            if (inputPieces == null) throw new ArgumentNullException(nameof(input.Pieces));
            var pieceCount = inputPieces.Count;
            if (pieceCount < 0)
                throw new InvalidOperationException("Curtain panel fingerprint Pieces Count must not be negative.");
            if (pieceCount > MaxPieces)
                throw new InvalidOperationException("Curtain panel fingerprint exceeds " + MaxPieces + " pieces.");

            var pieces = new List<CurtainWallPanelPiece>(pieceCount);
            for (var index = 0; index < pieceCount; index++)
            {
                RequireStablePieceCount(inputPieces, pieceCount);
                var sourcePiece = inputPieces[index];
                RequireStablePieceCount(inputPieces, pieceCount);
                pieces.Add(SnapshotAndValidate(sourcePiece));
            }
            RequireStablePieceCount(inputPieces, pieceCount);

            var canonical = new StringBuilder("CURTAIN_PANEL_V1")
                .Append('|').Append(R(sourceLengthM))
                .Append('|').Append(R(heightM))
                .Append('|').Append(R(bottomOffsetM))
                .Append('|').Append(R(panelDepthM))
                .Append('|').Append(sourceKind.ToUpperInvariant())
                .Append('|').Append(pathSegmentCount.ToString(CultureInfo.InvariantCulture));

            foreach (var piece in pieces
                .OrderBy(x => x.SourcePanelIndex)
                .ThenBy(x => x.Z_M)
                .ThenBy(x => x.X_M)
                .ThenBy(x => x.HeightM)
                .ThenBy(x => x.WidthM))
            {
                canonical.Append('|').Append(piece.SourcePanelIndex.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(R(piece.X_M))
                    .Append(',').Append(R(piece.Z_M))
                    .Append(',').Append(R(piece.WidthM))
                    .Append(',').Append(R(piece.HeightM));
            }

            using (var sha = SHA256.Create())
            {
                var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
                return string.Concat(digest.Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static void RequireStablePieceCount(IReadOnlyList<CurtainWallPanelPiece> pieces, int admittedCount)
        {
            var currentCount = pieces.Count;
            if (currentCount < 0)
                throw new InvalidOperationException("Curtain panel fingerprint Pieces Count must not be negative.");
            if (currentCount > MaxPieces)
                throw new InvalidOperationException("Curtain panel fingerprint exceeds " + MaxPieces + " pieces.");
            if (currentCount != admittedCount)
                throw new InvalidOperationException("Curtain panel fingerprint Pieces Count changed while being validated.");
        }

        private static CurtainWallPanelPiece SnapshotAndValidate(CurtainWallPanelPiece piece)
        {
            if (piece == null) throw new InvalidOperationException("Curtain panel fingerprint piece cannot be null.");

            // Piece DTOs are caller-mutable. Read each scalar exactly once, then validate and
            // hash only the detached snapshot so one digest cannot mix multiple observations.
            var sourcePanelIndex = piece.SourcePanelIndex;
            var xM = piece.X_M;
            var zM = piece.Z_M;
            var widthM = piece.WidthM;
            var heightM = piece.HeightM;

            if (sourcePanelIndex < 0) throw new ArgumentOutOfRangeException(nameof(piece.SourcePanelIndex));
            Finite(xM, nameof(piece.X_M));
            Finite(zM, nameof(piece.Z_M));
            Positive(widthM, nameof(piece.WidthM));
            Positive(heightM, nameof(piece.HeightM));
            var right = xM + widthM;
            var top = zM + heightM;
            Finite(right, "panel right");
            Finite(top, "panel top");
            if (!(right > xM))
                throw new OverflowException("Curtain panel fingerprint piece width is below the representable coordinate resolution.");
            if (!(top > zM))
                throw new OverflowException("Curtain panel fingerprint piece height is below the representable coordinate resolution.");
            var area = widthM * heightM;
            if (double.IsNaN(area) || double.IsInfinity(area))
                throw new OverflowException("Curtain panel fingerprint piece area must remain finite.");
            if (area == 0d && widthM != 0d && heightM != 0d)
                throw new OverflowException("Curtain panel fingerprint piece area underflowed to zero.");

            return new CurtainWallPanelPiece
            {
                SourcePanelIndex = sourcePanelIndex,
                X_M = xM,
                Z_M = zM,
                WidthM = widthM,
                HeightM = heightM
            };
        }

        private static double Positive(double value, string label)
        {
            Finite(value, label);
            if (value <= 0d) throw new ArgumentOutOfRangeException(label);
            return value;
        }

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(label, "Value must be finite.");
            return value;
        }

        private static string R(double value) => (value == 0d ? 0d : value).ToString("R", CultureInfo.InvariantCulture);
    }
}
