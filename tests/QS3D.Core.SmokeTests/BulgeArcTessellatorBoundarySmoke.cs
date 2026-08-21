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
            InvalidFiniteContractsFailClosed();
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

            var positive = BulgeArcTessellator.Tessellate(start, end, 1.0001e-12d);
            var negative = BulgeArcTessellator.Tessellate(start, end, -1.0001e-12d);
            AssertArc(positive, start, end, "positive just-above-tolerance bulge");
            AssertArc(negative, start, end, "negative just-above-tolerance bulge");
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
            if (positive.Count != positiveAgain.Count)
                throw new InvalidOperationException("Repeated bulge tessellation must retain deterministic vertex count.");
            for (var index = 0; index < positive.Count; index++)
            {
                AssertSamePoint(positive[index], positiveAgain[index], "Repeated bulge tessellation vertex " + index);
                AssertFinite(positive[index], "Positive semicircle vertex " + index);
                AssertFinite(negative[index], "Negative semicircle vertex " + index);
                if (index > 0)
                {
                    AssertDistinct(positive[index - 1], positive[index], "Positive semicircle adjacent vertices");
                    AssertDistinct(negative[index - 1], negative[index], "Negative semicircle adjacent vertices");
                }
            }

            var positiveInteriorY = positive[positive.Count / 2].Y;
            var negativeInteriorY = negative[negative.Count / 2].Y;
            if (!(positiveInteriorY < 0d && negativeInteriorY > 0d))
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

        private static void InvalidFiniteContractsFailClosed()
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

            var targetSegments = 4000d;
            var acceptedSagitta = Math.Pow(Math.Sin(Math.PI / (4d * targetSegments)), 2d);
            var accepted = BulgeArcTessellator.Tessellate(start, end, 1d, acceptedSagitta);
            if (accepted.Count < 3900 || accepted.Count > 4097)
                throw new InvalidOperationException("Near-ceiling bulge tessellation produced an unexpected vertex count: " + accepted.Count + ".");
            AssertArc(accepted, start, end, "near-ceiling tessellation");

            Expect<InvalidOperationException>(
                () => BulgeArcTessellator.Tessellate(start, end, 1d, 1e-12d),
                "segment-limit overflow");
        }

        private static void AssertStraight(IReadOnlyList<Point2> points, Point2 start, Point2 end, string label)
        {
            if (points.Count != 2)
                throw new InvalidOperationException(label + " must return exactly the chord endpoints.");
            AssertSamePoint(points[0], start, label + " start");
            AssertSamePoint(points[1], end, label + " end");
        }

        private static void AssertArc(IReadOnlyList<Point2> points, Point2 start, Point2 end, string label)
        {
            if (points.Count <= 2)
                throw new InvalidOperationException(label + " must contain an interior tessellation vertex.");
            AssertSamePoint(points[0], start, label + " start");
            AssertSamePoint(points[points.Count - 1], end, label + " end");
            for (var index = 0; index < points.Count; index++)
                AssertFinite(points[index], label + " vertex " + index);
        }

        private static void AssertFinite(Point2 point, string label)
        {
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                throw new InvalidOperationException(label + " must be finite.");
        }

        private static void AssertDistinct(Point2 left, Point2 right, string label)
        {
            if (left.X == right.X && left.Y == right.Y)
                throw new InvalidOperationException(label + " must not collapse to the same point.");
        }

        private static void AssertSamePoint(Point2 actual, Point2 expected, string label)
        {
            if (actual.X != expected.X || actual.Y != expected.Y)
                throw new InvalidOperationException(label + " mismatch. Expected (" + expected.X + ", " + expected.Y + ") but got (" + actual.X + ", " + actual.Y + ").");
        }

        private static void Expect<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException(label + " must fail with " + typeof(TException).Name + ".");
        }
    }
}
