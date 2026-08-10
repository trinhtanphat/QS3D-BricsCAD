using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class GlassWallLayoutInput
    {
        public double LengthM { get; set; }
        public double HeightM { get; set; }
        public double MaxPanelWidthM { get; set; }
        public double MaxPanelHeightM { get; set; }
        public double MullionWidthM { get; set; }
        public double TransomHeightM { get; set; }
    }

    public sealed class GlassPanelCell
    {
        public GlassPanelCell(int column, int row, double leftM, double bottomM, double widthM, double heightM)
        {
            Column = column;
            Row = row;
            LeftM = leftM;
            BottomM = bottomM;
            WidthM = widthM;
            HeightM = heightM;
        }

        public int Column { get; }
        public int Row { get; }
        public double LeftM { get; }
        public double BottomM { get; }
        public double WidthM { get; }
        public double HeightM { get; }
        public double AreaM2 => WidthM * HeightM;
    }

    public sealed class GlassWallLayout
    {
        public GlassWallLayout(
            int columns,
            int rows,
            double panelWidthM,
            double panelHeightM,
            IReadOnlyList<double> verticalFrameStationsM,
            IReadOnlyList<double> horizontalFrameElevationsM,
            IReadOnlyList<GlassPanelCell> panels)
        {
            Columns = columns;
            Rows = rows;
            PanelWidthM = panelWidthM;
            PanelHeightM = panelHeightM;
            VerticalFrameStationsM = verticalFrameStationsM ?? throw new ArgumentNullException(nameof(verticalFrameStationsM));
            HorizontalFrameElevationsM = horizontalFrameElevationsM ?? throw new ArgumentNullException(nameof(horizontalFrameElevationsM));
            Panels = panels ?? throw new ArgumentNullException(nameof(panels));
        }

        public int Columns { get; }
        public int Rows { get; }
        public int PanelCount => Panels.Count;
        public double PanelWidthM { get; }
        public double PanelHeightM { get; }
        public IReadOnlyList<double> VerticalFrameStationsM { get; }
        public IReadOnlyList<double> HorizontalFrameElevationsM { get; }
        public IReadOnlyList<GlassPanelCell> Panels { get; }
        public double PanelAreaM2 { get; internal set; }
        public double VerticalFrameLengthM { get; internal set; }
        public double HorizontalFrameLengthM { get; internal set; }
        public double TotalFrameLengthM => VerticalFrameLengthM + HorizontalFrameLengthM;
    }

    public static class GlassWallLayoutPlanner
    {
        private const int MaxPanelsPerAxis = 500;
        private const int MaxPanels = 10000;

        public static GlassWallLayout Plan(GlassWallLayoutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var length = Positive(input.LengthM, nameof(input.LengthM));
            var height = Positive(input.HeightM, nameof(input.HeightM));
            var maxPanelWidth = Positive(input.MaxPanelWidthM, nameof(input.MaxPanelWidthM));
            var maxPanelHeight = Positive(input.MaxPanelHeightM, nameof(input.MaxPanelHeightM));
            var mullionWidth = Positive(input.MullionWidthM, nameof(input.MullionWidthM));
            var transomHeight = Positive(input.TransomHeightM, nameof(input.TransomHeightM));

            if (length <= 2d * mullionWidth)
                throw new InvalidOperationException("Glass wall length must exceed two edge mullion widths.");
            if (height <= 2d * transomHeight)
                throw new InvalidOperationException("Glass wall height must exceed two edge transom heights.");

            var columns = ResolvePanelCount(length, maxPanelWidth, mullionWidth, "horizontal");
            var rows = ResolvePanelCount(height, maxPanelHeight, transomHeight, "vertical");
            var panelCount = checked(columns * rows);
            if (panelCount > MaxPanels) throw new InvalidOperationException("Glass wall layout requires too many panels.");

            var panelWidth = ResolveClearPanelSize(length, columns, mullionWidth, "panel width");
            var panelHeight = ResolveClearPanelSize(height, rows, transomHeight, "panel height");
            if (panelWidth > maxPanelWidth + 1e-12d || panelHeight > maxPanelHeight + 1e-12d)
                throw new InvalidOperationException("Glass wall panel sizing exceeded the configured maximum panel dimensions.");

            var verticalStations = BuildFrameCenters(length, columns, panelWidth, mullionWidth, "vertical frame station");
            var horizontalElevations = BuildFrameCenters(height, rows, panelHeight, transomHeight, "horizontal frame elevation");
            var panels = new List<GlassPanelCell>(panelCount);
            var panelArea = 0d;
            for (var row = 0; row < rows; row++)
            {
                var bottom = transomHeight + row * (panelHeight + transomHeight);
                bottom = Finite(bottom, "panel bottom");
                for (var column = 0; column < columns; column++)
                {
                    var left = mullionWidth + column * (panelWidth + mullionWidth);
                    left = Finite(left, "panel left");
                    panels.Add(new GlassPanelCell(column, row, left, bottom, panelWidth, panelHeight));
                    panelArea = Add(panelArea, Multiply(panelWidth, panelHeight, "panel cell area"), "glass panel area");
                }
            }

            var layout = new GlassWallLayout(
                columns,
                rows,
                panelWidth,
                panelHeight,
                verticalStations.AsReadOnly(),
                horizontalElevations.AsReadOnly(),
                panels.AsReadOnly())
            {
                PanelAreaM2 = panelArea,
                VerticalFrameLengthM = Multiply(verticalStations.Count, height, "vertical frame length"),
                HorizontalFrameLengthM = Multiply(horizontalElevations.Count, length, "horizontal frame length")
            };
            return layout;
        }

        private static int ResolvePanelCount(double span, double maxPanel, double frameWidth, string axis)
        {
            var numerator = Finite(span - frameWidth, axis + " panel-count numerator");
            var denominator = Add(maxPanel, frameWidth, axis + " panel-count denominator");
            var raw = numerator / denominator;
            raw = Finite(raw, axis + " panel count");
            var countDouble = Math.Ceiling(raw);
            if (countDouble < 1d) countDouble = 1d;
            if (countDouble > MaxPanelsPerAxis) throw new InvalidOperationException("Glass wall " + axis + " layout exceeds the panel-per-axis safety limit.");
            return checked((int)countDouble);
        }

        private static double ResolveClearPanelSize(double span, int count, double frameWidth, string label)
        {
            var frameCount = checked(count + 1);
            var occupiedByFrames = Multiply(frameCount, frameWidth, label + " frame occupancy");
            var clear = Finite(span - occupiedByFrames, label + " clear span");
            if (!(clear > 0d)) throw new InvalidOperationException("Glass wall frames leave no positive clear " + label + ".");
            return Finite(clear / count, label);
        }

        private static List<double> BuildFrameCenters(double span, int panelCount, double panelSize, double frameWidth, string label)
        {
            var result = new List<double>(checked(panelCount + 1));
            var pitch = Add(panelSize, frameWidth, label + " pitch");
            for (var index = 0; index <= panelCount; index++)
            {
                var center = Add(frameWidth / 2d, Multiply(index, pitch, label + " offset"), label);
                if (index == panelCount) center = span - frameWidth / 2d;
                result.Add(Finite(center, label));
            }
            return result;
        }

        private static double Positive(double value, string label)
        {
            value = Finite(value, label);
            if (!(value > 0d)) throw new ArgumentOutOfRangeException(label, "Value must be greater than zero.");
            return value;
        }

        private static double Add(double left, double right, string label) => Finite(left + right, label);
        private static double Multiply(double left, double right, string label) => Finite(left * right, label);

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " must be finite.");
            return value;
        }
    }
}
