using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class BulgeArcTessellatorBoundarySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            StraightToleranceBoundaryIsStable();
            SignedSemicirclesAreDeterministic();
            TighterSagittaNeverReducesResolution();
            InvalidContractsFailClosed();
            ExtremeRepresentabilityFailsClosed();
            SegmentCeilingIsEnforced();
        }

        private static void StraightToleranceBoundaryIsStable()
        {
            var start = new Point2(0d, 0d);
            var end = new Point2(10d, 0d);
            AssertStraight(BulgeArcTessellator.Tessellate(start, end, 0d), start, end, "zero bulge");
            AssertStraight(BulgeArcTessellator.Tessellate(start, end, 1e-12d), start, end, "positive tolerance bulge");
            AssertStraight(BulgeArcTessellator.Tessellate(start, end, -1e-12d), start, end, "negative tolerance bulge");

            // Just above the straight threshold the geometric arc may still require one chord segment
            // for a loose sagitta. Pin acceptance and exact endpoint preservation rather than forcing
            // an artificial interior vertex.
            AssertEndpoints(BulgeArcTessellator.Tessellate(start, end, 1.0001e-12d), start, end, "positive just-above-tolerance bulge");
            AssertEndpoints(BulgeArcTessellator.Tessellate(start, end, -1.0001e-12d), start, end, "negative just-above-tolerance bulge");
        }

        private static void SignedSemicirclesAreDeterministic()
        {
            var start = new Point2(0d, 0d);
            var end = new Point2(2d, 0d);
            var positive = BulgeArcTessellator.Tessellate(start, end, 1d, 0.01d);
            var positiveAgain = BulgeArcTessellator.Tessellate(start, end, 1d, 0.01d);
            var negative = BulgeArcTessellator.Tessellate(start, end, -1d, 0.01d);

            AssertArc(positive, start, end, "positive semicircle");
            AssertArc(negative, start, end, "negative semicircle");
            if (positive.Count != positiveAgain.Count || positive.Count != negative.Count)
                throw new InvalidOperationException("Signed/repeated semicircle tessellation must retain deterministic vertex count.");

            for (var i = 0; i < positive.Count; i++)
            {
                AssertSamePoint(positive[i], positiveAgain[i], "repeated vertex " + i);
                AssertFinite(positive[i], "positive vertex " + i);
                AssertFinite(negative[i], "negative vertex " + i);
                if (i > 0)
                {
                    AssertDistinct(positive[i - 1], positive[i], "positive adjacent vertices");
                    AssertDistinct(negative[i - 1], negative[i], "negative adjacent vertices");
                }
            }

            if (!(positive[positive.Count / 2].Y < 0d && negative[negative.Count / 2].Y > 0d))
                throw new InvalidOperationException("Bulge sign must deterministically select opposite arc orientations.");
        }

        private static void TighterSagittaNeverReducesResolution()
        {
            var start = new Point2(-3d, 2d);
            var end = new Point2(5d, 7d);
            var coarse = BulgeArcTessellator.Tessellate(start, end, 0.75d, 0.05d);
            var medium = BulgeArcTessellator.Tessellate(start, end, 0.75d, 0.01d);
            var fine = BulgeArcTessellator.Tessellate(start, end, 0.75d, 0.001d);
            if (medium.Count < coarse.Count || fine.Count < medium.Count)
                throw new InvalidOperationException("Tightening maximum sagitta must not reduce tessellation resolution.");
            AssertArc(coarse, start, end, "coarse sagitta");
            AssertArc(medium, start, end, "medium sagitta");
            AssertArc(fine, start, end, "fine sagitta");
        }

        private static void InvalidContractsFailClosed()
        {
            var origin = new Point2(0d, 0d);
            var unit = new Point2(1d, 0d);
            Expect<ArgumentOutOfRangeException>(() => BulgeArcTessellator.Tessellate(new Point2(double.NaN, 0d), unit, 1d), "NaN start coordinate");
            Expect<ArgumentOutOfRangeException>(() => BulgeArcTessellator.Tessellate(origin, new Point2(double.PositiveInfinity, 0d), 1d), "infinite end coordinate");
            Expect<ArgumentOutOfRangeException>(() => BulgeArcTessellator.Tessellate(origin, unit, double.NaN), "NaN bulge");
            Expect<ArgumentOutOfRangeException>(() => BulgeArcTessellator.Tessellate(origin, unit, double.NegativeInfinity), "infinite bulge");
            Expect<ArgumentOutOfRangeException>(() => BulgeArcTessellator.Tessellate(origin, unit, 1d, double.NaN), "NaN sagitta");
            Expect<ArgumentOutOfRangeException>(() => BulgeArcTessellator.Tessellate(origin, unit, 1d, double.PositiveInfinity), "infinite sagitta");
            Expect<ArgumentOutOfRangeException>(() => BulgeArcTessellator.Tessellate(origin, unit, 1d, 0d), "zero sagitta");
            Expect<ArgumentOutOfRangeException>(() => BulgeArcTessellator.Tessellate(origin, unit, 1d, -0.1d), "negative sagitta");
            Expect<ArgumentException>(() => BulgeArcTessellator.Tessellate(origin, origin, 1d), "degenerate chord");
        }

        private static void ExtremeRepresentabilityFailsClosed()
        {
            Expect<OverflowException>(
                () => BulgeArcTessellator.Tessellate(new Point2(0d, 0d), new Point2(1e300d, 0d), 2e-12d),
                "finite inputs producing unrepresentable radius");
            Expect<InvalidOperationException>(
                () => BulgeArcTessellator.Tessellate(new Point2(1e16d, 0d), new Point2(1e16d + 2d, 0d), 1d),
                "midpoint below representable coordinate resolution");
        }

        private static void SegmentCeilingIsEnforced()
        {
            var start = new Point2(0d, 0d);
            var end = new Point2(1d, 0d);
            const double targetSegments = 4000d;
            var acceptedSagitta = Math.Pow(Math.Sin(Math.PI / (4d * targetSegments)), 2d);
            var accepted = BulgeArcTessellator.Tessellate(start, end, 1d, acceptedSagitta);
            if (accepted.Count < 3900 || accepted.Count > 4097)
                throw new InvalidOperationException("Near-ceiling tessellation produced unexpected vertex count: " + accepted.Count + ".");
            AssertArc(accepted, start, end, "near-ceiling tessellation");
            Expect<InvalidOperationException>(() => BulgeArcTessellator.Tessellate(start, end, 1d, 1e-12d), "segment-limit overflow");
        }

        private static void AssertStraight(IReadOnlyList<Point2> points, Point2 start, Point2 end, string label)
        {
            if (points.Count != 2) throw new InvalidOperationException(label + " must return exactly two endpoints.");
            AssertEndpoints(points, start, end, label);
        }

        private static void AssertArc(IReadOnlyList<Point2> points, Point2 start, Point2 end, string label)
        {
            if (points.Count <= 2) throw new InvalidOperationException(label + " must contain an interior vertex.");
            AssertEndpoints(points, start, end, label);
            for (var i = 0; i < points.Count; i++) AssertFinite(points[i], label + " vertex " + i);
        }

        private static void AssertEndpoints(IReadOnlyList<Point2> points, Point2 start, Point2 end, string label)
        {
            if (points.Count < 2) throw new InvalidOperationException(label + " must contain both chord endpoints.");
            AssertSamePoint(points[0], start, label + " start");
            AssertSamePoint(points[points.Count - 1], end, label + " end");
        }

        private static void AssertFinite(Point2 point, string label)
        {
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                throw new InvalidOperationException(label + " must be finite.");
        }

        private static void AssertDistinct(Point2 left, Point2 right, string label)
        {
            if (left.X == right.X && left.Y == right.Y) throw new InvalidOperationException(label + " must not collapse.");
        }

        private static void AssertSamePoint(Point2 actual, Point2 expected, string label)
        {
            if (actual.X != expected.X || actual.Y != expected.Y)
                throw new InvalidOperationException(label + " mismatch. Expected (" + expected.X + ", " + expected.Y + ") but got (" + actual.X + ", " + actual.Y + ").");
        }

        private static void Expect<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException(label + " must fail with " + typeof(TException).Name + ".");
        }
    }
}
