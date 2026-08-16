using System;

namespace QS3D.Core.Geometry
{
    public sealed class CurtainWallLayoutInput
    {
        public double LengthM { get; set; }
        public double HeightM { get; set; }
        public double MaxPanelWidthM { get; set; }
        public double MaxPanelHeightM { get; set; }
        public double PerimeterFrameWidthM { get; set; }
        public double MullionWidthM { get; set; }
        public double TransomWidthM { get; set; }
    }

    public sealed class CurtainWallLayout
    {
        internal CurtainWallLayout(
            int columns,
            int rows,
            double bayWidthM,
            double bayHeightM,
            double minimumClearPanelWidthM,
            double maximumClearPanelWidthM,
            double minimumClearPanelHeightM,
            double maximumClearPanelHeightM,
            double grossAreaM2,
            double clearGlassAreaM2,
            double frameFaceAreaM2,
            double verticalFrameLengthM,
            double horizontalFrameLengthM)
        {
            Columns = columns;
            Rows = rows;
            BayWidthM = bayWidthM;
            BayHeightM = bayHeightM;
            MinimumClearPanelWidthM = minimumClearPanelWidthM;
            MaximumClearPanelWidthM = maximumClearPanelWidthM;
            MinimumClearPanelHeightM = minimumClearPanelHeightM;
            MaximumClearPanelHeightM = maximumClearPanelHeightM;
            GrossAreaM2 = grossAreaM2;
            ClearGlassAreaM2 = clearGlassAreaM2;
            FrameFaceAreaM2 = frameFaceAreaM2;
            VerticalFrameLengthM = verticalFrameLengthM;
            HorizontalFrameLengthM = horizontalFrameLengthM;
        }

        public int Columns { get; }
        public int Rows { get; }
        public int PanelCount => checked(Columns * Rows);
        public int VerticalFrameCount => checked(Columns + 1);
        public int HorizontalFrameCount => checked(Rows + 1);
        public double BayWidthM { get; }
        public double BayHeightM { get; }
        public double MinimumClearPanelWidthM { get; }
        public double MaximumClearPanelWidthM { get; }
        public double MinimumClearPanelHeightM { get; }
        public double MaximumClearPanelHeightM { get; }
        public double GrossAreaM2 { get; }
        public double ClearGlassAreaM2 { get; }
        public double FrameFaceAreaM2 { get; }
        public double VerticalFrameLengthM { get; }
        public double HorizontalFrameLengthM { get; }
        public double TotalFrameLengthM
        {
            get
            {
                var result = VerticalFrameLengthM + HorizontalFrameLengthM;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Curtain total frame length overflowed.");
                if (VerticalFrameLengthM > 0d && HorizontalFrameLengthM > 0d &&
                    (result == VerticalFrameLengthM || result == HorizontalFrameLengthM))
                {
                    throw new OverflowException("Curtain total frame length lost a positive component at floating-point precision.");
                }
                return result;
            }
        }
    }

    public static class CurtainWallLayoutPlanner
    {
        private const int MaxGridDivisions = 10000;
        private const int MaxPanels = 250000;

        public static CurtainWallLayout Plan(CurtainWallLayoutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var lengthM = Positive(input.LengthM, nameof(input.LengthM));
            var heightM = Positive(input.HeightM, nameof(input.HeightM));
            var maxPanelWidthM = Positive(input.MaxPanelWidthM, nameof(input.MaxPanelWidthM));
            var maxPanelHeightM = Positive(input.MaxPanelHeightM, nameof(input.MaxPanelHeightM));
            var perimeterFrameWidthM = NonNegative(input.PerimeterFrameWidthM, nameof(input.PerimeterFrameWidthM));
            var mullionWidthM = NonNegative(input.MullionWidthM, nameof(input.MullionWidthM));
            var transomWidthM = NonNegative(input.TransomWidthM, nameof(input.TransomWidthM));

            var columns = DivisionCount(lengthM, maxPanelWidthM, "curtain panel columns");
            var rows = DivisionCount(heightM, maxPanelHeightM, "curtain panel rows");
            if (columns > MaxGridDivisions || rows > MaxGridDivisions)
                throw new InvalidOperationException("Curtain wall grid exceeds the supported division limit.");
            var panelCount = checked(columns * rows);
            if (panelCount > MaxPanels)
                throw new InvalidOperationException("Curtain wall grid exceeds the supported panel-count limit of " + MaxPanels + ".");

            var bayWidthM = Divide(lengthM, columns, "curtain bay width");
            var bayHeightM = Divide(heightM, rows, "curtain bay height");

            var widthMetrics = ClearDimensionMetrics(lengthM, bayWidthM, columns, perimeterFrameWidthM, mullionWidthM, "curtain panel width");
            var heightMetrics = ClearDimensionMetrics(heightM, bayHeightM, rows, perimeterFrameWidthM, transomWidthM, "curtain panel height");

            var grossAreaM2 = Multiply(lengthM, heightM, "curtain gross area");
            var clearGlassAreaM2 = Multiply(widthMetrics.TotalClearM, heightMetrics.TotalClearM, "curtain clear glass area");
            var frameFaceAreaM2 = SubtractFloorZero(grossAreaM2, clearGlassAreaM2, "curtain frame face area");
            var verticalFrameLengthM = Multiply(columns + 1d, heightM, "curtain vertical frame length");
            var horizontalFrameLengthM = Multiply(rows + 1d, lengthM, "curtain horizontal frame length");

            return new CurtainWallLayout(
                columns,
                rows,
                bayWidthM,
                bayHeightM,
                widthMetrics.MinimumClearM,
                widthMetrics.MaximumClearM,
                heightMetrics.MinimumClearM,
                heightMetrics.MaximumClearM,
                grossAreaM2,
                clearGlassAreaM2,
                frameFaceAreaM2,
                verticalFrameLengthM,
                horizontalFrameLengthM);
        }

        private sealed class ClearMetrics
        {
            public double TotalClearM { get; set; }
            public double MinimumClearM { get; set; }
            public double MaximumClearM { get; set; }
        }

        private static ClearMetrics ClearDimensionMetrics(
            double totalM,
            double bayM,
            int divisions,
            double perimeterFrameWidthM,
            double internalFrameWidthM,
            string label)
        {
            double edgeClearM;
            double interiorClearM;
            double totalClearM;
            if (divisions == 1)
            {
                edgeClearM = Subtract(totalM, Multiply(2d, perimeterFrameWidthM, label + " perimeter frames"), label + " single clear span");
                interiorClearM = edgeClearM;
                totalClearM = edgeClearM;
            }
            else
            {
                var halfInternalM = Divide(internalFrameWidthM, 2d, label + " half internal frame");
                edgeClearM = Subtract(Subtract(bayM, perimeterFrameWidthM, label + " edge frame"), halfInternalM, label + " edge clear span");
                interiorClearM = Subtract(bayM, internalFrameWidthM, label + " interior clear span");
                var internalCount = Math.Max(0, divisions - 2);
                totalClearM = Add(Multiply(2d, edgeClearM, label + " edge clear total"), Multiply(internalCount, interiorClearM, label + " interior clear total"), label + " clear total");
            }

            if (!(edgeClearM > 0d) || !(interiorClearM > 0d) || !(totalClearM > 0d))
                throw new InvalidOperationException(label + " is not positive after frame deductions.");
            return new ClearMetrics
            {
                TotalClearM = totalClearM,
                MinimumClearM = divisions <= 2 ? edgeClearM : Math.Min(edgeClearM, interiorClearM),
                MaximumClearM = divisions <= 2 ? edgeClearM : Math.Max(edgeClearM, interiorClearM)
            };
        }

        private static int DivisionCount(double spanM, double maximumM, string label)
        {
            var ratio = Divide(spanM, maximumM, label + " ratio");
            var ceiling = Math.Ceiling(ratio);
            if (double.IsNaN(ceiling) || double.IsInfinity(ceiling) || ceiling < 1d || ceiling > int.MaxValue)
                throw new InvalidOperationException(label + " is outside supported integer range.");
            return (int)ceiling;
        }

        private static double Positive(double value, string label)
        {
            value = Finite(value, label);
            if (!(value > 0d)) throw new ArgumentOutOfRangeException(label);
            return value;
        }

        private static double NonNegative(double value, string label)
        {
            value = Finite(value, label);
            if (value < 0d) throw new ArgumentOutOfRangeException(label);
            return value;
        }

        private static double Add(double left, double right, string label)
        {
            left = Finite(left, label + " left");
            right = Finite(right, label + " right");
            var result = left + right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " overflowed.");
            if (left > 0d && right > 0d && (result == left || result == right))
                throw new InvalidOperationException(label + " lost a positive contribution at floating-point precision.");
            return result;
        }

        private static double Subtract(double left, double right, string label)
        {
            left = Finite(left, label + " left");
            right = Finite(right, label + " right");
            var result = left - right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " overflowed.");
            if (right > 0d && result == left)
                throw new InvalidOperationException(label + " lost a positive deduction at floating-point precision.");
            return result;
        }

        private static double SubtractFloorZero(double left, double right, string label)
        {
            var result = Subtract(left, right, label);
            return result <= 0d ? 0d : result;
        }

        private static double Multiply(double left, double right, string label)
        {
            left = Finite(left, label + " left");
            right = Finite(right, label + " right");
            var result = left * right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " overflowed.");
            if (result == 0d && left != 0d && right != 0d)
                throw new InvalidOperationException(label + " underflowed to zero.");
            return result;
        }

        private static double Divide(double numerator, double denominator, string label)
        {
            numerator = Finite(numerator, label + " numerator");
            denominator = Finite(denominator, label + " denominator");
            if (!(denominator > 0d)) throw new InvalidOperationException(label + " denominator must be positive.");
            var result = numerator / denominator;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " overflowed.");
            if (result == 0d && numerator != 0d)
                throw new InvalidOperationException(label + " underflowed to zero.");
            return result;
        }

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(label, "Value must be finite.");
            return value;
        }
    }
}
