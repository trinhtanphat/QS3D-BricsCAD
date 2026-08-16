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
            var verticalFrameCount = PhysicalFrameCount(layout.Columns, input.PerimeterFrameWidthM, input.MullionWidthM);
            var horizontalFrameCount = PhysicalFrameCount(layout.Rows, input.PerimeterFrameWidthM, input.TransomWidthM);
            var projectedDetailSolids = checked((long)layout.PanelCount + verticalFrameCount + horizontalFrameCount);
            if (projectedDetailSolids > MaxDetailSolids)
                throw new InvalidOperationException("Curtain wall native detail requires too many panel/frame solids: " + projectedDetailSolids + ".");

            var verticalFrames = BuildVerticalFrames(input, layout);
            var horizontalFrames = BuildHorizontalFrames(input, layout);
            var panels = BuildPanelCells(input, layout);
            var solidCount = checked(panels.Count + verticalFrames.Count + horizontalFrames.Count);
            if (solidCount != projectedDetailSolids)
                throw new InvalidOperationException("Curtain wall native detail count does not match the projected grid count.");

            var panelArea = 0d;
            foreach (var panel in panels)
                panelArea = Add(panelArea, Multiply(panel.WidthM, panel.HeightM, "curtain detail panel area"), "curtain detail total panel area");
            var tolerance = Math.Max(1e-10d, layout.ClearGlassAreaM2 * 1e-10d);
            if (Math.Abs(panelArea - layout.ClearGlassAreaM2) > tolerance)
                throw new InvalidOperationException("Curtain wall detail panel area does not match the layout clear-glass area.");

            return new CurtainWallDetail(layout, panels.AsReadOnly(), verticalFrames.AsReadOnly(), horizontalFrames.AsReadOnly());
        }

        private static int PhysicalFrameCount(int divisions, double perimeterFrameWidthM, double internalFrameWidthM)
        {
            var perimeterCount = perimeterFrameWidthM > 0d ? 2 : 0;
            var internalCount = internalFrameWidthM > 0d && divisions > 1 ? divisions - 1 : 0;
            return checked(perimeterCount + internalCount);
        }

        private static List<CurtainWallRect> BuildVerticalFrames(CurtainWallLayoutInput input, CurtainWallLayout layout)
        {
            var frames = new List<CurtainWallRect>(PhysicalFrameCount(layout.Columns, input.PerimeterFrameWidthM, input.MullionWidthM));
            for (var index = 0; index <= layout.Columns; index++)
            {
                double left;
                double width;
                if (index == 0)
                {
                    width = input.PerimeterFrameWidthM;
                    if (width == 0d) continue;
                    left = 0d;
                }
                else if (index == layout.Columns)
                {
                    width = input.PerimeterFrameWidthM;
                    if (width == 0d) continue;
                    left = Subtract(input.LengthM, width, "curtain vertical frame right perimeter placement");
                }
                else
                {
                    width = input.MullionWidthM;
                    if (width == 0d) continue;
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
            var frames = new List<CurtainWallRect>(PhysicalFrameCount(layout.Rows, input.PerimeterFrameWidthM, input.TransomWidthM));
            for (var index = 0; index <= layout.Rows; index++)
            {
                double bottom;
                double height;
                if (index == 0)
                {
                    height = input.PerimeterFrameWidthM;
                    if (height == 0d) continue;
                    bottom = 0d;
                }
                else if (index == layout.Rows)
                {
                    height = input.PerimeterFrameWidthM;
                    if (height == 0d) continue;
                    bottom = Subtract(input.HeightM, height, "curtain horizontal frame top perimeter placement");
                }
                else
                {
                    height = input.TransomWidthM;
                    if (height == 0d) continue;
                    var center = Multiply(index, layout.BayHeightM, "curtain horizontal frame center");
                    var halfHeight = Multiply(height, .5d, "curtain horizontal frame half height");
                    bottom = Subtract(center, halfHeight, "curtain horizontal frame half-height placement");
                }
                frames.Add(Rect(0d, bottom, input.LengthM, height, "curtain horizontal frame"));
            }
            return frames;
        }

        private static List<CurtainWallRect> BuildPanelCells(CurtainWallLayoutInput input, CurtainWallLayout layout)
        {
            var panels = new List<CurtainWallRect>(layout.PanelCount);
            var halfMullion = Multiply(input.MullionWidthM, .5d, "curtain panel half mullion width");
            var halfTransom = Multiply(input.TransomWidthM, .5d, "curtain panel half transom height");

            for (var row = 0; row < layout.Rows; row++)
            {
                var bottom = row == 0
                    ? input.PerimeterFrameWidthM
                    : Add(Multiply(row, layout.BayHeightM, "curtain panel bottom grid"), halfTransom, "curtain panel bottom");
                var top = row + 1 == layout.Rows
                    ? Subtract(input.HeightM, input.PerimeterFrameWidthM, "curtain panel top perimeter")
                    : Subtract(Multiply(row + 1d, layout.BayHeightM, "curtain panel top grid"), halfTransom, "curtain panel top");
                var height = SubtractPositive(top, bottom, "curtain panel height");

                for (var column = 0; column < layout.Columns; column++)
                {
                    var left = column == 0
                        ? input.PerimeterFrameWidthM
                        : Add(Multiply(column, layout.BayWidthM, "curtain panel left grid"), halfMullion, "curtain panel left");
                    var right = column + 1 == layout.Columns
                        ? Subtract(input.LengthM, input.PerimeterFrameWidthM, "curtain panel right perimeter")
                        : Subtract(Multiply(column + 1d, layout.BayWidthM, "curtain panel right grid"), halfMullion, "curtain panel right");
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
