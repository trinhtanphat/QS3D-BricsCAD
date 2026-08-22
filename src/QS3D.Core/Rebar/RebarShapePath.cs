using System;
using System.Collections.Generic;
using System.Globalization;

namespace QS3D.Core.Rebar
{
    public readonly struct RebarShapePoint
    {
        public RebarShapePoint(double x, double y, double z = 0d)
        {
            X = Finite(x, nameof(x)); Y = Finite(y, nameof(y)); Z = Finite(z, nameof(z));
        }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        private static double Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }

    public sealed class RebarShapePath
    {
        public RebarShapePath(string shapeCode, IReadOnlyList<RebarShapePoint> points)
        {
            ShapeCode = string.IsNullOrWhiteSpace(shapeCode) ? "00" : shapeCode.Trim();
            if (points == null) throw new ArgumentNullException(nameof(points));
            var snapshot = new List<RebarShapePoint>(points);
            if (snapshot.Count < 2) throw new ArgumentException("Rebar shape path requires at least two points.", nameof(points));
            Points = snapshot.AsReadOnly();
        }
        public string ShapeCode { get; }
        public IReadOnlyList<RebarShapePoint> Points { get; }
    }

    public static class RebarShapePathBuilder
    {
        private const int MaxLegs = 32;
        private const int MaxListTextLength = 4096;
        public static RebarShapePath Build(string? shapeCode, double cuttingLengthM, string? legsText = null, string? turnsText = null)
        {
            if (double.IsNaN(cuttingLengthM) || double.IsInfinity(cuttingLengthM) || cuttingLengthM <= 0d) throw new ArgumentOutOfRangeException(nameof(cuttingLengthM));
            var code = Normalize(shapeCode);
            if (IsStraight(code) && string.IsNullOrWhiteSpace(legsText)) return new RebarShapePath(code, new[] { new RebarShapePoint(0d, 0d), new RebarShapePoint(cuttingLengthM, 0d) });
            var legs = ParsePositiveList(legsText, "RebarShapeLegsM", MaxLegs);
            if (legs.Count == 0)
            {
                if (IsStraight(code)) return new RebarShapePath(code, new[] { new RebarShapePoint(0d, 0d), new RebarShapePoint(cuttingLengthM, 0d) });
                throw new InvalidOperationException("Rebar shape " + code + " requires RebarShapeLegsM so geometry is not guessed from cutting length alone.");
            }
            ValidateTotal(legs, cuttingLengthM);
            IReadOnlyList<double> turns = !string.IsNullOrWhiteSpace(turnsText) ? ParseTurns(turnsText, legs.Count - 1) : PresetTurns(code, legs.Count);
            if (turns.Count != legs.Count - 1) throw new InvalidOperationException("RebarShapeTurnsDeg must contain exactly legs-1 values.");
            var points = new List<RebarShapePoint>(legs.Count + 1) { new RebarShapePoint(0d, 0d) };
            var x = 0d; var y = 0d; var angle = 0d;
            for (var index = 0; index < legs.Count; index++)
            {
                var nextX = AddFinite(x, legs[index] * Math.Cos(angle), "rebar shape X");
                var nextY = AddFinite(y, legs[index] * Math.Sin(angle), "rebar shape Y");
                if (nextX == x && nextY == y)
                    throw new OverflowException("Rebar shape positive leg at index " + index + " collapsed at the current coordinate scale.");
                x = nextX;
                y = nextY;
                points.Add(new RebarShapePoint(x, y));
                if (index < turns.Count) angle = AddFinite(angle, TurnRadians(turns[index]), "rebar shape angle");
            }
            return new RebarShapePath(code, points.AsReadOnly());
        }
        private static IReadOnlyList<double> PresetTurns(string code, int legCount)
        {
            if (IsStraight(code)) { if (legCount != 1) throw new InvalidOperationException("Straight rebar shape 00 must contain exactly one leg."); return Array.Empty<double>(); }
            if (code == "11" || code == "L") { if (legCount != 2) throw new InvalidOperationException("L/11 rebar shape requires exactly two legs."); return new[] { 90d }; }
            if (code == "21" || code == "U") { if (legCount != 3) throw new InvalidOperationException("U/21 rebar shape requires exactly three legs."); return new[] { 90d, 90d }; }
            if (code == "31" || code == "Z") { if (legCount != 3) throw new InvalidOperationException("Z/31 rebar shape requires exactly three legs."); return new[] { 90d, -90d }; }
            throw new InvalidOperationException("Unsupported RebarShapeCode " + code + ". Provide RebarShapeTurnsDeg for an explicit custom segmented path.");
        }
        private static List<double> ParsePositiveList(string? text, string label, int maxCount)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<double>();
            var raw = text!;
            if (raw.Length > MaxListTextLength) throw new FormatException(label + " exceeds the supported " + MaxListTextLength + "-character limit.");
            var values = new List<double>();
            foreach (var token in Split(raw))
            {
                if (values.Count >= maxCount) throw new InvalidOperationException("Rebar shape exceeds the supported leg limit of " + MaxLegs + ".");
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0d) throw new FormatException(label + " contains an invalid positive number: " + token);
                values.Add(value);
            }
            return values;
        }
        private static IReadOnlyList<double> ParseTurns(string? text, int maxCount)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<double>();
            var raw = text!;
            if (raw.Length > MaxListTextLength) throw new FormatException("RebarShapeTurnsDeg exceeds the supported " + MaxListTextLength + "-character limit.");
            var values = new List<double>();
            foreach (var token in Split(raw))
            {
                if (values.Count >= maxCount) throw new InvalidOperationException("RebarShapeTurnsDeg must contain exactly legs-1 values.");
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > 180d) throw new FormatException("RebarShapeTurnsDeg contains an invalid turn angle: " + token);
                values.Add(value);
            }
            return values;
        }
        private static IEnumerable<string> Split(string text) => text.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        private static void ValidateTotal(IReadOnlyList<double> legs, double cuttingLengthM)
        {
            var total = 0d; foreach (var leg in legs) total = AddFinite(total, leg, "rebar shape total length");
            var tolerance = Math.Max(1e-6d, cuttingLengthM * 1e-6d);
            if (Math.Abs(total - cuttingLengthM) > tolerance) throw new InvalidOperationException("RebarShapeLegsM total " + total.ToString("R", CultureInfo.InvariantCulture) + " m does not match BBS cutting length " + cuttingLengthM.ToString("R", CultureInfo.InvariantCulture) + " m.");
        }
        private static double TurnRadians(double degrees)
        {
            if (double.IsNaN(degrees) || double.IsInfinity(degrees) || Math.Abs(degrees) > 180d)
                throw new ArgumentOutOfRangeException(nameof(degrees));
            var radians = degrees * Math.PI / 180d;
            if (double.IsNaN(radians) || double.IsInfinity(radians)) throw new OverflowException("Rebar shape turn angle scaling overflowed.");
            if (degrees != 0d && radians == 0d) throw new OverflowException("Rebar shape nonzero turn angle underflowed to zero radians.");
            return radians;
        }
        private static string Normalize(string? code) => string.IsNullOrWhiteSpace(code) ? "00" : code!.Trim().ToUpperInvariant();
        private static bool IsStraight(string code) => code == "00" || code == "0" || code == "STRAIGHT";
        private static double AddFinite(double left, double right, string label) { var result = left + right; if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " overflowed."); return result; }
    }
}
