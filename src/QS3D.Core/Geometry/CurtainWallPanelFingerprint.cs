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
            Positive(input.SourceLengthM, nameof(input.SourceLengthM));
            Positive(input.HeightM, nameof(input.HeightM));
            Finite(input.BottomOffsetM, nameof(input.BottomOffsetM));
            Positive(input.PanelDepthM, nameof(input.PanelDepthM));
            var sourceKind = input.SourceKind ?? string.Empty;
            if (!string.Equals(sourceKind, sourceKind.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Curtain panel source kind must not contain leading or trailing whitespace.", nameof(input.SourceKind));
            if (!string.Equals(sourceKind, "Line", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(sourceKind, "OpenPolyline", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Curtain panel source kind must be Line or OpenPolyline.", nameof(input.SourceKind));
            if (input.PathSegmentCount < 0 ||
                (string.Equals(sourceKind, "Line", StringComparison.OrdinalIgnoreCase) && input.PathSegmentCount != 0) ||
                (string.Equals(sourceKind, "OpenPolyline", StringComparison.OrdinalIgnoreCase) && input.PathSegmentCount < 1))
                throw new ArgumentOutOfRangeException(nameof(input.PathSegmentCount));
            if (input.Pieces == null) throw new ArgumentNullException(nameof(input.Pieces));
            var pieceCount = input.Pieces.Count;
            if (pieceCount > MaxPieces)
                throw new InvalidOperationException("Curtain panel fingerprint exceeds " + MaxPieces + " pieces.");

            var pieces = new List<CurtainWallPanelPiece>(pieceCount);
            for (var index = 0; index < pieceCount; index++)
                pieces.Add(Validate(input.Pieces[index]));

            var canonical = new StringBuilder("CURTAIN_PANEL_V1")
                .Append('|').Append(R(input.SourceLengthM))
                .Append('|').Append(R(input.HeightM))
                .Append('|').Append(R(input.BottomOffsetM))
                .Append('|').Append(R(input.PanelDepthM))
                .Append('|').Append(sourceKind.ToUpperInvariant())
                .Append('|').Append(input.PathSegmentCount.ToString(CultureInfo.InvariantCulture));

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

        private static CurtainWallPanelPiece Validate(CurtainWallPanelPiece piece)
        {
            if (piece == null) throw new InvalidOperationException("Curtain panel fingerprint piece cannot be null.");
            if (piece.SourcePanelIndex < 0) throw new ArgumentOutOfRangeException(nameof(piece.SourcePanelIndex));
            Finite(piece.X_M, nameof(piece.X_M));
            Finite(piece.Z_M, nameof(piece.Z_M));
            Positive(piece.WidthM, nameof(piece.WidthM));
            Positive(piece.HeightM, nameof(piece.HeightM));
            Finite(piece.X_M + piece.WidthM, "panel right");
            Finite(piece.Z_M + piece.HeightM, "panel top");
            var area = piece.WidthM * piece.HeightM;
            if (double.IsNaN(area) || double.IsInfinity(area))
                throw new OverflowException("Curtain panel fingerprint piece area must remain finite.");
            return piece;
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