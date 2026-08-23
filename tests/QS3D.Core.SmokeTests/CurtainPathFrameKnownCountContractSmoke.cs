using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainPathFrameKnownCountContractSmoke
    {
        public static void Run()
        {
            NegativeFrameCountFailsBeforeIndexAccess();
            ValidatedFrameCountIsBoundToTraversal();
            EmptyFramesRemainValid();
            OrdinaryFrameMappingRemainsDeterministic();
            PositiveSegmentMustAdvanceCumulativeStation();
            InteriorProjectionStationMustRemainRepresentable();
        }

        private static void NegativeFrameCountFailsBeforeIndexAccess()
        {
            var frames = new NegativeCountList<CurtainWallRect>();

            try
            {
                CurtainPathFramePlanner.Plan(StraightPath(), frames);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("Count", StringComparison.OrdinalIgnoreCase) < 0 ||
                    ex.Message.IndexOf("negative", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Negative frame Count should report a deterministic Count-contract diagnostic.");
                if (frames.IndexReads != 0)
                    throw new Exception("Negative frame Count must fail before any frame index access.");
                return;
            }

            throw new Exception("Negative frame Count must fail closed instead of producing a plan.");
        }

        private static void ValidatedFrameCountIsBoundToTraversal()
        {
            var frames = new DriftingCountList<CurtainWallRect>(new CurtainWallRect(0d, 0d, 2d, 1d));
            var plan = CurtainPathFramePlanner.Plan(StraightPath(), frames);

            if (frames.CountReads != 1)
                throw new Exception("Curtain path frame planner must bind one validated Count snapshot to traversal.");
            if (frames.IndexReads != 1)
                throw new Exception("Validated single-frame traversal should read exactly one source frame.");
            if (plan.SourceFrameCount != 1 || plan.Pieces.Count != 1)
                throw new Exception("Count drift after validation must not change the validated frame traversal or published count.");
        }

        private static void EmptyFramesRemainValid()
        {
            var plan = CurtainPathFramePlanner.Plan(StraightPath(), Array.Empty<CurtainWallRect>());
            if (plan.SourceFrameCount != 0 || plan.Pieces.Count != 0)
                throw new Exception("An honest empty frame collection should remain a valid empty plan.");
            if (plan.PathSegmentCount != 1 || Math.Abs(plan.PathLengthM - 10d) > 1e-12d)
                throw new Exception("Empty-frame planning must preserve the validated host path metadata.");
        }

        private static void OrdinaryFrameMappingRemainsDeterministic()
        {
            var plan = CurtainPathFramePlanner.Plan(
                StraightPath(),
                new[] { new CurtainWallRect(2d, 0d, 3d, 1.5d) });

            if (plan.SourceFrameCount != 1 || plan.Pieces.Count != 1)
                throw new Exception("Ordinary single-frame mapping should produce one deterministic path piece.");

            var piece = plan.Pieces[0];
            if (piece.SourceFrameIndex != 0 || piece.PathSegmentIndex != 0)
                throw new Exception("Ordinary frame mapping changed source/path segment identity.");
            Near(2d, piece.StationStartM, "station start");
            Near(5d, piece.StationEndM, "station end");
            Near(3d, piece.WidthM, "piece width");
            Near(3.5d, piece.CenterX_M, "piece center X");
            Near(0d, piece.CenterY_M, "piece center Y");
            Near(0d, piece.Z_M, "piece elevation");
            Near(1.5d, piece.HeightM, "piece height");
        }

        private static void PositiveSegmentMustAdvanceCumulativeStation()
        {
            var path = new[]
            {
                new Point2(0d, 0d),
                new Point2(1e16d, 0d),
                new Point2(1e16d, 1d),
                new Point2(1e16d, 2d)
            };

            try
            {
                CurtainPathFramePlanner.Length(path);
            }
            catch (OverflowException ex)
            {
                if (ex.Message.IndexOf("cumulative length", StringComparison.OrdinalIgnoreCase) < 0 ||
                    ex.Message.IndexOf("precision", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Curtain path station precision collapse should report a deterministic cumulative-length diagnostic.");
                return;
            }

            throw new Exception("A positive path segment whose station cannot advance must fail closed instead of publishing a collapsed station interval.");
        }

        private static void InteriorProjectionStationMustRemainRepresentable()
        {
            var path = new[]
            {
                new Point2(0d, 0d),
                new Point2(1e16d, 0d),
                new Point2(1e16d, 2d)
            };

            try
            {
                CurtainPathFramePlanner.ProjectPoint(path, new Point2(1e16d, 0.5d));
            }
            catch (OverflowException ex)
            {
                if (ex.Message.IndexOf("projection station", StringComparison.OrdinalIgnoreCase) < 0 ||
                    ex.Message.IndexOf("precision", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Interior curtain projection station collapse should report a deterministic station-precision diagnostic.");
                return;
            }

            throw new Exception("An interior projection whose station rounds to a segment endpoint must fail closed instead of publishing the wrong station.");
        }

        private static IReadOnlyList<Point2> StraightPath() =>
            new[] { new Point2(0d, 0d), new Point2(10d, 0d) };

        private static void Near(double expected, double actual, string label)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(expected - actual) > 1e-12d)
                throw new Exception(label + " mismatch. Expected " + expected + ", actual " + actual + ".");
        }

        private sealed class NegativeCountList<T> : IReadOnlyList<T>
        {
            public int IndexReads { get; private set; }
            public int Count => -1;

            public T this[int index]
            {
                get
                {
                    IndexReads++;
                    throw new Exception("Negative Count source must not be indexed.");
                }
            }

            public IEnumerator<T> GetEnumerator() => throw new Exception("IReadOnlyList traversal must use indexed access only after Count validation.");
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class DriftingCountList<T> : IReadOnlyList<T>
        {
            private readonly T _item;

            public DriftingCountList(T item)
            {
                _item = item;
            }

            public int CountReads { get; private set; }
            public int IndexReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    return CountReads == 1 ? 1 : 0;
                }
            }

            public T this[int index]
            {
                get
                {
                    IndexReads++;
                    if (index != 0) throw new IndexOutOfRangeException();
                    return _item;
                }
            }

            public IEnumerator<T> GetEnumerator() => throw new Exception("Curtain path frame planner should use the validated indexed snapshot contract.");
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
