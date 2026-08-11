using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public sealed class CurtainWallPanelPiece
    {
        public int SourcePanelIndex { get; set; }
        public double X_M { get; set; }
        public double Z_M { get; set; }
        public double WidthM { get; set; }
        public double HeightM { get; set; }
        public double AreaM2 => WidthM * HeightM;
    }

    public sealed class CurtainWallOpeningPanelPlan
    {
        public IReadOnlyList<CurtainWallPanelPiece> Pieces { get; set; } = Array.Empty<CurtainWallPanelPiece>();
        public int SourcePanelCount { get; set; }
        public int InterruptedPanelCount { get; set; }
        public double OriginalPanelAreaM2 { get; set; }
        public double RemainingPanelAreaM2 { get; set; }
        public double RemovedPanelAreaM2 => Math.Max(0d, OriginalPanelAreaM2 - RemainingPanelAreaM2);
    }

    public static class CurtainWallOpeningPanelPlanner
    {
        public const int MaxInputPanels = 20000;
        public const int MaxOpenings = CurtainWallOpeningFramePlanner.MaxOpenings;
        public const int MaxOutputPieces = CurtainWallOpeningFramePlanner.MaxOutputPieces;

        public static CurtainWallOpeningPanelPlan Plan(
            IReadOnlyList<CurtainWallRect> panels,
            IReadOnlyList<CurtainWallOpeningRect> openings,
            double clearanceM = 0d)
        {
            if (panels == null) throw new ArgumentNullException(nameof(panels));
            if (openings == null) throw new ArgumentNullException(nameof(openings));
            if (panels.Count > MaxInputPanels)
                throw new InvalidOperationException("Curtain panel input exceeds " + MaxInputPanels + " panels.");
            if (openings.Count > MaxOpenings)
                throw new InvalidOperationException("Curtain panel interruption input exceeds " + MaxOpenings + " openings.");

            var clipped = CurtainWallOpeningFramePlanner.Plan(panels, openings, clearanceM);
            if (clipped.Pieces.Count > MaxOutputPieces)
                throw new InvalidOperationException("Curtain panel interruption output exceeds " + MaxOutputPieces + " pieces.");

            return new CurtainWallOpeningPanelPlan
            {
                Pieces = clipped.Pieces
                    .Select(x => new CurtainWallPanelPiece
                    {
                        SourcePanelIndex = x.SourceFrameIndex,
                        X_M = x.X_M,
                        Z_M = x.Z_M,
                        WidthM = x.WidthM,
                        HeightM = x.HeightM
                    })
                    .OrderBy(x => x.SourcePanelIndex)
                    .ThenBy(x => x.Z_M)
                    .ThenBy(x => x.X_M)
                    .ThenBy(x => x.HeightM)
                    .ThenBy(x => x.WidthM)
                    .ToArray(),
                SourcePanelCount = panels.Count,
                InterruptedPanelCount = clipped.InterruptedFrameCount,
                OriginalPanelAreaM2 = clipped.OriginalFrameAreaM2,
                RemainingPanelAreaM2 = clipped.RemainingFrameAreaM2
            };
        }
    }
}
