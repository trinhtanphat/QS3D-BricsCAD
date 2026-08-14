using System;
using System.Collections.Generic;

namespace QS3D.Core.Rebar
{
    public sealed class RebarPlannedCut
    {
        internal RebarPlannedCut(string cutId, int instanceIndex, double requiredLengthM, double allowanceLengthM, double effectiveLengthM)
        {
            CutId = cutId;
            InstanceIndex = instanceIndex;
            RequiredLengthM = requiredLengthM;
            AllowanceLengthM = allowanceLengthM;
            EffectiveLengthM = effectiveLengthM;
        }

        public string CutId { get; }
        public int InstanceIndex { get; }
        public double RequiredLengthM { get; }
        public double AllowanceLengthM { get; }
        public double EffectiveLengthM { get; }
    }

    public sealed class RebarStockCutPlan
    {
        internal RebarStockCutPlan(
            int stockBarIndex,
            double stockLengthM,
            IReadOnlyList<RebarPlannedCut> cuts,
            double allocatedLengthBeforeKerfM,
            int cutOperationCount,
            double kerfLengthM,
            double offCutLengthM)
        {
            StockBarIndex = stockBarIndex;
            StockLengthM = stockLengthM;
            Cuts = cuts;
            AllocatedLengthBeforeKerfM = allocatedLengthBeforeKerfM;
            CutOperationCount = cutOperationCount;
            KerfLengthM = kerfLengthM;
            OffCutLengthM = offCutLengthM;
        }

        public int StockBarIndex { get; }
        public double StockLengthM { get; }
        public IReadOnlyList<RebarPlannedCut> Cuts { get; }
        public double AllocatedLengthBeforeKerfM { get; }
        public int CutOperationCount { get; }
        public double KerfLengthM { get; }
        public double OffCutLengthM { get; }
    }

    public sealed class RebarCuttingOptimizationResult
    {
        internal RebarCuttingOptimizationResult(
            RebarStockDemand demand,
            IReadOnlyList<RebarStockCutPlan> stockBars,
            RebarStockProcurementQuantities procurementQuantities)
        {
            Demand = demand;
            StockBars = stockBars;
            ProcurementQuantities = procurementQuantities;
        }

        public const string AlgorithmId = "BestFitDecreasingV1";
        public RebarStockDemand Demand { get; }
        public IReadOnlyList<RebarStockCutPlan> StockBars { get; }
        public RebarStockProcurementQuantities ProcurementQuantities { get; }
    }

    public static class RebarCuttingOptimizer
    {
        private const int MaxExpandedPieces = 10000;
        private const double FitToleranceM = 1e-12d;

        public static RebarCuttingOptimizationResult Plan(RebarStockDemand demand)
        {
            if (demand == null) throw new ArgumentNullException(nameof(demand));
            if (demand.RequiredCutCount > MaxExpandedPieces)
                throw new ArgumentOutOfRangeException(nameof(demand), "Rebar cutting optimisation exceeds the supported expanded-piece bound of " + MaxExpandedPieces + ".");

            var pieces = ExpandAndSort(demand);
            var bars = new List<BarState>();
            foreach (var piece in pieces)
            {
                var selectedIndex = -1;
                var selectedMetrics = default(BarMetrics);
                var bestOffCut = double.PositiveInfinity;

                for (var index = 0; index < bars.Count; index++)
                {
                    var metrics = Evaluate(
                        demand.StockLengthM,
                        demand.AllowancePolicy.KerfPerCutM,
                        bars[index].AllocatedLengthBeforeKerfM,
                        bars[index].Pieces.Count,
                        piece.EffectiveLengthM);
                    if (!metrics.Fits) continue;
                    if (metrics.OffCutLengthM < bestOffCut)
                    {
                        selectedIndex = index;
                        selectedMetrics = metrics;
                        bestOffCut = metrics.OffCutLengthM;
                    }
                }

                if (selectedIndex < 0)
                {
                    var metrics = Evaluate(
                        demand.StockLengthM,
                        demand.AllowancePolicy.KerfPerCutM,
                        0d,
                        0,
                        piece.EffectiveLengthM);
                    if (!metrics.Fits)
                        throw new InvalidOperationException(
                            "Rebar cut " + piece.CutId + "#" + piece.InstanceIndex +
                            " cannot be produced from stock length " + demand.StockLengthM + " m under the current allowance/kerf policy.");
                    var bar = new BarState();
                    bar.Add(piece, metrics);
                    bars.Add(bar);
                }
                else
                {
                    bars[selectedIndex].Add(piece, selectedMetrics);
                }
            }

            var plans = new List<RebarStockCutPlan>(bars.Count);
            var totalKerfLengthM = 0d;
            var totalOffCutLengthM = 0d;
            for (var index = 0; index < bars.Count; index++)
            {
                var state = bars[index];
                var cuts = new List<RebarPlannedCut>(state.Pieces.Count);
                foreach (var piece in state.Pieces)
                    cuts.Add(new RebarPlannedCut(piece.CutId, piece.InstanceIndex, piece.RequiredLengthM, piece.AllowanceLengthM, piece.EffectiveLengthM));

                totalKerfLengthM = RebarMath.Add(totalKerfLengthM, state.Metrics.KerfLengthM, "cutting optimisation total kerf");
                totalOffCutLengthM = RebarMath.Add(totalOffCutLengthM, state.Metrics.OffCutLengthM, "cutting optimisation total off-cut");
                plans.Add(new RebarStockCutPlan(
                    index + 1,
                    demand.StockLengthM,
                    cuts.AsReadOnly(),
                    state.AllocatedLengthBeforeKerfM,
                    state.Metrics.CutOperationCount,
                    state.Metrics.KerfLengthM,
                    state.Metrics.OffCutLengthM));
            }

            var procurement = new RebarStockProcurementQuantities(
                demand.StockLengthM,
                plans.Count,
                totalKerfLengthM,
                totalOffCutLengthM);
            return new RebarCuttingOptimizationResult(demand, plans.AsReadOnly(), procurement);
        }

        private static List<Piece> ExpandAndSort(RebarStockDemand demand)
        {
            var pieces = new List<Piece>((int)demand.RequiredCutCount);
            foreach (var requirement in demand.RequiredCuts)
            {
                var effectiveLengthM = RebarMath.Add(
                    requirement.LengthM,
                    demand.AllowancePolicy.AllowancePerRequiredCutM,
                    "rebar cutting effective piece length");
                for (var instanceIndex = 1; instanceIndex <= requirement.Quantity; instanceIndex++)
                    pieces.Add(new Piece(requirement.CutId, instanceIndex, requirement.LengthM, demand.AllowancePolicy.AllowancePerRequiredCutM, effectiveLengthM));
            }

            pieces.Sort(ComparePieces);
            return pieces;
        }

        private static int ComparePieces(Piece left, Piece right)
        {
            var byLength = right.EffectiveLengthM.CompareTo(left.EffectiveLengthM);
            if (byLength != 0) return byLength;
            var byIdIgnoreCase = StringComparer.OrdinalIgnoreCase.Compare(left.CutId, right.CutId);
            if (byIdIgnoreCase != 0) return byIdIgnoreCase;
            var byId = StringComparer.Ordinal.Compare(left.CutId, right.CutId);
            if (byId != 0) return byId;
            return left.InstanceIndex.CompareTo(right.InstanceIndex);
        }

        private static BarMetrics Evaluate(
            double stockLengthM,
            double kerfPerCutM,
            double existingAllocatedLengthM,
            int existingPieceCount,
            double nextPieceLengthM)
        {
            var allocatedLengthM = RebarMath.Add(existingAllocatedLengthM, nextPieceLengthM, "cutting optimisation stock allocation");
            var pieceCount = checked(existingPieceCount + 1);

            var exactFillCutCount = Math.Max(0, pieceCount - 1);
            var exactFillKerfM = RebarMath.Multiply(kerfPerCutM, exactFillCutCount, "cutting optimisation exact-fill kerf");
            var exactFillConsumedM = RebarMath.Add(allocatedLengthM, exactFillKerfM, "cutting optimisation exact-fill consumption");
            if (NearlyEqual(exactFillConsumedM, stockLengthM))
                return new BarMetrics(true, allocatedLengthM, exactFillCutCount, exactFillKerfM, 0d);
            if (exactFillConsumedM > stockLengthM + FitToleranceM)
                return BarMetrics.DoesNotFit;

            var tailCutCount = pieceCount;
            var tailKerfM = RebarMath.Multiply(kerfPerCutM, tailCutCount, "cutting optimisation tail kerf");
            var consumedM = RebarMath.Add(allocatedLengthM, tailKerfM, "cutting optimisation stock consumption");
            if (consumedM > stockLengthM + FitToleranceM)
                return BarMetrics.DoesNotFit;

            var offCutLengthM = stockLengthM - consumedM;
            if (offCutLengthM < 0d && offCutLengthM >= -FitToleranceM) offCutLengthM = 0d;
            if (offCutLengthM < 0d) return BarMetrics.DoesNotFit;
            return new BarMetrics(true, allocatedLengthM, tailCutCount, tailKerfM, offCutLengthM);
        }

        private static bool NearlyEqual(double left, double right) => Math.Abs(left - right) <= FitToleranceM;

        private sealed class Piece
        {
            public Piece(string cutId, int instanceIndex, double requiredLengthM, double allowanceLengthM, double effectiveLengthM)
            {
                CutId = cutId;
                InstanceIndex = instanceIndex;
                RequiredLengthM = requiredLengthM;
                AllowanceLengthM = allowanceLengthM;
                EffectiveLengthM = effectiveLengthM;
            }

            public string CutId { get; }
            public int InstanceIndex { get; }
            public double RequiredLengthM { get; }
            public double AllowanceLengthM { get; }
            public double EffectiveLengthM { get; }
        }

        private sealed class BarState
        {
            public List<Piece> Pieces { get; } = new List<Piece>();
            public double AllocatedLengthBeforeKerfM { get; private set; }
            public BarMetrics Metrics { get; private set; }

            public void Add(Piece piece, BarMetrics metrics)
            {
                Pieces.Add(piece);
                AllocatedLengthBeforeKerfM = metrics.AllocatedLengthBeforeKerfM;
                Metrics = metrics;
            }
        }

        private struct BarMetrics
        {
            public static readonly BarMetrics DoesNotFit = new BarMetrics(false, 0d, 0, 0d, 0d);

            public BarMetrics(bool fits, double allocatedLengthBeforeKerfM, int cutOperationCount, double kerfLengthM, double offCutLengthM)
            {
                Fits = fits;
                AllocatedLengthBeforeKerfM = allocatedLengthBeforeKerfM;
                CutOperationCount = cutOperationCount;
                KerfLengthM = kerfLengthM;
                OffCutLengthM = offCutLengthM;
            }

            public bool Fits { get; }
            public double AllocatedLengthBeforeKerfM { get; }
            public int CutOperationCount { get; }
            public double KerfLengthM { get; }
            public double OffCutLengthM { get; }
        }
    }
}
