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
        public double AreaM2
        {
            get
            {
                var area = WidthM * HeightM;
                if (double.IsNaN(area) || double.IsInfinity(area))
                    throw new OverflowException("Curtain panel piece area overflowed.");
                if (area == 0d && WidthM != 0d && HeightM != 0d)
                    throw new OverflowException("Curtain panel piece area underflowed to zero.");
                return area == 0d ? 0d : area;
            }
        }
    }

    public sealed class CurtainWallOpeningPanelPlan
    {
        public IReadOnlyList<CurtainWallPanelPiece> Pieces { get; set; } = Array.Empty<CurtainWallPanelPiece>();
        public int SourcePanelCount { get; set; }
        public int InterruptedPanelCount { get; set; }
        public double OriginalPanelAreaM2 { get; set; }
        public double RemainingPanelAreaM2 { get; set; }
        public double RemovedPanelAreaM2
        {
            get
            {
                var removed = OriginalPanelAreaM2 - RemainingPanelAreaM2;
                if (double.IsNaN(removed) || double.IsInfinity(removed))
                    throw new OverflowException("Curtain removed panel area is not representable.");
                return removed <= 0d ? 0d : removed;
            }
        }
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

            var panelCount = panels.Count;
            var openingCount = openings.Count;
            if (panelCount < 0)
                throw new InvalidOperationException("Curtain panel input reports an invalid negative panel Count.");
            if (openingCount < 0)
                throw new InvalidOperationException("Curtain panel interruption input reports an invalid negative opening Count.");
            if (panelCount > MaxInputPanels)
                throw new InvalidOperationException("Curtain panel input exceeds " + MaxInputPanels + " panels.");
            if (openingCount > MaxOpenings)
                throw new InvalidOperationException("Curtain panel interruption input exceeds " + MaxOpenings + " openings.");

            var panelSnapshot = SnapshotStable(panels, panelCount, "panel");
            var openingSnapshot = SnapshotStable(openings, openingCount, "opening");
            var clipped = CurtainWallOpeningFramePlanner.Plan(panelSnapshot, openingSnapshot, clearanceM);
            if (clipped.Pieces.Count > MaxOutputPieces)
                throw new InvalidOperationException("Curtain panel interruption output exceeds " + MaxOutputPieces + " pieces.");

            return new CurtainWallOpeningPanelPlan
            {
                Pieces = Array.AsReadOnly(clipped.Pieces
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
                    .ToArray()),
                SourcePanelCount = panelCount,
                InterruptedPanelCount = clipped.InterruptedFrameCount,
                OriginalPanelAreaM2 = clipped.OriginalFrameAreaM2,
                RemainingPanelAreaM2 = clipped.RemainingFrameAreaM2
            };
        }

        private static T[] SnapshotStable<T>(IReadOnlyList<T> source, int expectedCount, string label)
        {
            var snapshot = new T[expectedCount];
            for (var index = 0; index < expectedCount; index++)
            {
                if (source.Count != expectedCount)
                    throw new InvalidOperationException("Curtain panel " + label + " input Count changed while it was being snapshotted.");
                snapshot[index] = source[index];
            }
            if (source.Count != expectedCount)
                throw new InvalidOperationException("Curtain panel " + label + " input Count changed while it was being snapshotted.");
            return snapshot;
        }
    }
}
