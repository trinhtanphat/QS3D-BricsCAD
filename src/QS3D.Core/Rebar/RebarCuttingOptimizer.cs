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
                        bars[index].AllocatedLengthBeforeKerf,
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
                        CompensatedLength.Zero,
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
                    state.AllocatedLengthBeforeKerf.Value("cutting optimisation allocated length"),
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
            CompensatedLength existingAllocatedLength,
            int existingPieceCount,
            double nextPieceLengthM)
        {
            var allocatedLength = existingAllocatedLength.Add(nextPieceLengthM, "cutting optimisation stock allocation");
            var pieceCount = checked(existingPieceCount + 1);

            var exactFillCutCount = Math.Max(0, pieceCount - 1);
            var exactFillKerfM = RebarMath.Multiply(kerfPerCutM, exactFillCutCount, "cutting optimisation exact-fill kerf");
            var exactFillConsumed = allocatedLength.Add(exactFillKerfM, "cutting optimisation exact-fill consumption");
            var exactFillComparison = exactFillConsumed.CompareTo(stockLengthM);
            if (exactFillComparison <= 0)
            {
                var exactFillRemainder = exactFillConsumed.RemainingIn(stockLengthM, "cutting optimisation exact-fill remainder");
                if (exactFillRemainder <= FitToleranceM)
                    return new BarMetrics(true, allocatedLength, exactFillCutCount, exactFillKerfM, 0d);
            }
            else
            {
                return BarMetrics.DoesNotFit;
            }

            var tailCutCount = pieceCount;
            var tailKerfM = RebarMath.Multiply(kerfPerCutM, tailCutCount, "cutting optimisation tail kerf");
            var consumed = allocatedLength.Add(tailKerfM, "cutting optimisation stock consumption");
            if (consumed.CompareTo(stockLengthM) > 0)
                return BarMetrics.DoesNotFit;

            var offCutLengthM = consumed.RemainingIn(stockLengthM, "cutting optimisation off-cut");
            if (offCutLengthM <= FitToleranceM) offCutLengthM = 0d;
            return new BarMetrics(true, allocatedLength, tailCutCount, tailKerfM, offCutLengthM);
        }

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
            public CompensatedLength AllocatedLengthBeforeKerf { get; private set; }
            public BarMetrics Metrics { get; private set; }

            public void Add(Piece piece, BarMetrics metrics)
            {
                Pieces.Add(piece);
                AllocatedLengthBeforeKerf = metrics.AllocatedLengthBeforeKerf;
                Metrics = metrics;
            }
        }

        private struct BarMetrics
        {
            public static readonly BarMetrics DoesNotFit = new BarMetrics(false, CompensatedLength.Zero, 0, 0d, 0d);

            public BarMetrics(bool fits, CompensatedLength allocatedLengthBeforeKerf, int cutOperationCount, double kerfLengthM, double offCutLengthM)
            {
                Fits = fits;
                AllocatedLengthBeforeKerf = allocatedLengthBeforeKerf;
                CutOperationCount = cutOperationCount;
                KerfLengthM = kerfLengthM;
                OffCutLengthM = offCutLengthM;
            }

            public bool Fits { get; }
            public CompensatedLength AllocatedLengthBeforeKerf { get; }
            public int CutOperationCount { get; }
            public double KerfLengthM { get; }
            public double OffCutLengthM { get; }
        }

        private struct CompensatedLength
        {
            private double _sum;
            private double _compensation;

            public static CompensatedLength Zero => default(CompensatedLength);

            public CompensatedLength Add(double value, string label)
            {
                var result = this;
                var next = result._sum + value;
                EnsureFinite(next, label);

                var correction = Math.Abs(result._sum) >= Math.Abs(value)
                    ? (result._sum - next) + value
                    : (value - next) + result._sum;
                var compensation = result._compensation + correction;
                EnsureFinite(compensation, label);

                result._sum = next;
                result._compensation = compensation;
                return result;
            }

            public int CompareTo(double value)
            {
                EnsureFinite(value, "cutting optimisation comparison");
                var difference = _sum - value;
                EnsureFinite(difference, "cutting optimisation comparison");
                if (difference == 0d) return _compensation.CompareTo(0d);

                var correctionNeeded = -difference;
                if (_compensation < correctionNeeded) return -1;
                if (_compensation > correctionNeeded) return 1;
                return 0;
            }

            public double RemainingIn(double stockLengthM, string label)
            {
                if (CompareTo(stockLengthM) > 0)
                    throw new InvalidOperationException("Compensated rebar allocation exceeds stock length: " + label + ".");

                var difference = stockLengthM - _sum;
                EnsureFinite(difference, label);
                var remainder = difference - _compensation;
                EnsureFinite(remainder, label);
                if (remainder < 0d)
                {
                    if (remainder >= -FitToleranceM) return 0d;
                    throw new InvalidOperationException("Compensated rebar allocation produced a negative stock remainder: " + label + ".");
                }
                return remainder;
            }

            public double Value(string label)
            {
                var value = _sum + _compensation;
                EnsureFinite(value, label);
                return value;
            }

            private static void EnsureFinite(double value, string label)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    throw new OverflowException("Rebar addition overflow: " + label);
            }
        }
    }
}
