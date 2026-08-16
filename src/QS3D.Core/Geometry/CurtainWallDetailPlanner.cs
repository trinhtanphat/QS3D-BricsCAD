using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class CurtainWallRect
    {
        public CurtainWallRect(double xM, double zM, double widthM, double heightM)
        {
            X_M = xM;
            Z_M = zM;
            WidthM = widthM;
            HeightM = heightM;
        }

        public double X_M { get; }
        public double Z_M { get; }
        public double WidthM { get; }
        public double HeightM { get; }
        public double AreaM2
        {
            get
            {
                var area = WidthM * HeightM;
                if (double.IsNaN(area) || double.IsInfinity(area))
                    throw new OverflowException("Curtain rectangle area must remain finite.");
                if (area == 0d && WidthM != 0d && HeightM != 0d)
                    throw new OverflowException("Curtain rectangle area underflowed to zero.");
                return area == 0d ? 0d : area;
            }
        }
    }

    public sealed class CurtainWallDetail
    {
        internal CurtainWallDetail(
            CurtainWallLayout layout,
            IReadOnlyList<CurtainWallRect> panels,
            IReadOnlyList<CurtainWallRect> verticalFrames,
            IReadOnlyList<CurtainWallRect> horizontalFrames)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Panels = panels ?? throw new ArgumentNullException(nameof(panels));
            VerticalFrames = verticalFrames ?? throw new ArgumentNullException(nameof(verticalFrames));
            HorizontalFrames = horizontalFrames ?? throw new ArgumentNullException(nameof(horizontalFrames));
        }

        public CurtainWallLayout Layout { get; }
        public IReadOnlyList<CurtainWallRect> Panels { get; }
        public IReadOnlyList<CurtainWallRect> VerticalFrames { get; }
        public IReadOnlyList<CurtainWallRect> HorizontalFrames { get; }
        public double PanelAreaM2 => Layout.ClearGlassAreaM2;
        public int DetailSolidCount => checked(Panels.Count + VerticalFrames.Count + HorizontalFrames.Count);
    }

    public static class CurtainWallDetailPlanner
    {
        private const int MaxDetailSolids = 20000;

        public static CurtainWallDetail Plan(CurtainWallLayoutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var layout = CurtainWallLayoutPlanner.Plan(input);
            var projectedDetailSolids = checked((long)layout.PanelCount + layout.VerticalFrameCount + layout.HorizontalFrameCount);
            if (projectedDetailSolids > MaxDetailSolids)
                throw new InvalidOperationException("Curtain wall native detail requires too many panel/frame solids: " + projectedDetailSolids + ".");

            var verticalFrames = BuildVerticalFrames(input, layout);
            var horizontalFrames = BuildHorizontalFrames(input, layout);
            var panels = BuildPanelCells(verticalFrames, horizontalFrames);
            var solidCount = checked(panels.Count + verticalFrames.Count + horizontalFrames.Count);
            if (solidCount != projectedDetailSolids)
                throw new InvalidOperationException("Curtain wall native detail count does not match the projected grid count.");

            var panelArea = 0d;
            foreach (var panel in panels) panelArea = Add(panelArea, Multiply(panel.WidthM, panel.HeightM, "curtain detail panel area"), "curtain detail total panel area");
            var tolerance = Math.Max(1e-10d, layout.ClearGlassAreaM2 * 1e-10d);
            if (Math.Abs(panelArea - layout.ClearGlassAreaM2) > tolerance)
                throw new InvalidOperationException("Curtain wall detail panel area does not match the layout clear-glass area.");

            return new CurtainWallDetail(layout, panels.AsReadOnly(), verticalFrames.AsReadOnly(), horizontalFrames.AsReadOnly());
        }

        private static List<CurtainWallRect> BuildVerticalFrames(CurtainWallLayoutInput input, CurtainWallLayout layout)
        {
            var frames = new List<CurtainWallRect>(layout.VerticalFrameCount);
            for (var index = 0; index <= layout.Columns; index++)
            {
                double left;
                double width;
                if (index == 0)
                {
                    left = 0d;
                    width = input.PerimeterFrameWidthM;
                }
                else if (index == layout.Columns)
                {
                    width = input.PerimeterFrameWidthM;
                    left = Subtract(input.LengthM, width, "curtain vertical frame right perimeter placement");
                }
                else
                {
                    width = input.MullionWidthM;
                    var center = Multiply(index, layout.BayWidthM, "curtain vertical frame center");
                    var halfWidth = Multiply(width, .5d, "curtain vertical frame half width");
                    left = Subtract(center, halfWidth, "curtain vertical frame half-width placement");
                }
                frames.Add(Rect(left, 0d, width, input.HeightM, "curtain vertical frame"));
            }
            return frames;
        }

        private static List<CurtainWallRect> BuildHorizontalFrames(CurtainWallLayoutInput input, CurtainWallLayout layout)
        {
            var frames = new List<CurtainWallRect>(layout.HorizontalFrameCount);
            for (var index = 0; index <= layout.Rows; index++)
            {
                double bottom;
                double height;
                if (index == 0)
                {
                    bottom = 0d;
                    height = input.PerimeterFrameWidthM;
                }
                else if (index == layout.Rows)
                {
                    height = input.PerimeterFrameWidthM;
                    bottom = Subtract(input.HeightM, height, "curtain horizontal frame top perimeter placement");
                }
                else
                {
                    height = input.TransomWidthM;
                    var center = Multiply(index, layout.BayHeightM, "curtain horizontal frame center");
                    var halfHeight = Multiply(height, .5d, "curtain horizontal frame half height");
                    bottom = Subtract(center, halfHeight, "curtain horizontal frame half-height placement");
                }
                frames.Add(Rect(0d, bottom, input.LengthM, height, "curtain horizontal frame"));
            }
            return frames;
        }

        private static List<CurtainWallRect> BuildPanelCells(IReadOnlyList<CurtainWallRect> verticalFrames, IReadOnlyList<CurtainWallRect> horizontalFrames)
        {
            var columns = verticalFrames.Count - 1;
            var rows = horizontalFrames.Count - 1;
            var panels = new List<CurtainWallRect>(checked(columns * rows));
            for (var row = 0; row < rows; row++)
            {
                var bottom = Add(horizontalFrames[row].Z_M, horizontalFrames[row].HeightM, "curtain panel bottom");
                var top = horizontalFrames[row + 1].Z_M;
                var height = SubtractPositive(top, bottom, "curtain panel height");
                for (var column = 0; column < columns; column++)
                {
                    var left = Add(verticalFrames[column].X_M, verticalFrames[column].WidthM, "curtain panel left");
                    var right = verticalFrames[column + 1].X_M;
                    var width = SubtractPositive(right, left, "curtain panel width");
                    panels.Add(Rect(left, bottom, width, height, "curtain panel"));
                }
            }
            return panels;
        }

        private static CurtainWallRect Rect(double x, double z, double width, double height, string label)
        {
            x = Finite(x, label + " X");
            z = Finite(z, label + " Z");
            width = Positive(width, label + " width");
            height = Positive(height, label + " height");
            if (x < -1e-12d || z < -1e-12d) throw new InvalidOperationException(label + " starts outside the curtain wall extent.");

            var normalizedX = Math.Max(0d, x);
            var normalizedZ = Math.Max(0d, z);
            var right = Finite(normalizedX + width, label + " right");
            var top = Finite(normalizedZ + height, label + " top");
            if (!(right > normalizedX))
                throw new OverflowException(label + " width is below the representable coordinate resolution.");
            if (!(top > normalizedZ))
                throw new OverflowException(label + " height is below the representable coordinate resolution.");

            return new CurtainWallRect(normalizedX, normalizedZ, width, height);
        }

        private static double Positive(double value, string label)
        {
            value = Finite(value, label);
            if (!(value > 0d)) throw new InvalidOperationException(label + " must be greater than zero.");
            return value;
        }

        private static double Add(double left, double right, string label)
        {
            left = Finite(left, label + " left");
            right = Finite(right, label + " right");
            var result = Finite(left + right, label);
            if (left > 0d && right > 0d && (result == left || result == right))
                throw new OverflowException(label + " lost a positive contribution at floating-point precision.");
            return result == 0d ? 0d : result;
        }

        private static double Subtract(double left, double right, string label)
        {
            left = Finite(left, label + " left");
            right = Finite(right, label + " right");
            var result = Finite(left - right, label);
            if (right > 0d && result == left)
                throw new OverflowException(label + " lost a positive deduction at floating-point precision.");
            return result == 0d ? 0d : result;
        }

        private static double Multiply(double left, double right, string label)
        {
            left = Finite(left, label + " left");
            right = Finite(right, label + " right");
            var result = Finite(left * right, label);
            if (result == 0d && left != 0d && right != 0d)
                throw new OverflowException(label + " underflowed to zero.");
            return result == 0d ? 0d : result;
        }

        private static double SubtractPositive(double left, double right, string label)
        {
            var result = Subtract(left, right, label);
            if (!(result > 0d)) throw new InvalidOperationException(label + " must be positive.");
            return result;
        }

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " must be finite.");
            return value;
        }
    }
}
