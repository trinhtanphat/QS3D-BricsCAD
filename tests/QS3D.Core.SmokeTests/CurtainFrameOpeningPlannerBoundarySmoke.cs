using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainFrameOpeningPlannerBoundarySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            InputContractsFailClosed();
            NoOverlapAndFullCoverAreStable();
            CentralAndPartialCutsAreDeterministic();
            ClearanceExpandsTheInterruptedEnvelope();
            OpeningOrderPreservesCanonicalGeometry();
            InputCeilingsStopLazyTraversalAtBoundaryPlusOne();
            FragmentCeilingFailsClosed();
            ReturnedSnapshotIsIsolatedFromCallerMutation();
        }

        private static void InputContractsFailClosed()
        {
            var frame = R(0d, 0d, 10d, 10d);
            var opening = O(4d, 4d, 2d, 2d);
            Expect<ArgumentNullException>(() => CurtainFrameOpeningPlanner.Interrupt(null!, new[] { opening }), "null frames");
            Expect<ArgumentNullException>(() => CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, null!), "null openings");
            Expect<ArgumentException>(() => CurtainFrameOpeningPlanner.Interrupt(new CurtainWallRect[] { frame, null! }, Array.Empty<CurtainOpeningRect>()), "null frame entry");
            Expect<ArgumentException>(() => CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new CurtainOpeningRect[] { opening, null! }), "null opening entry");

            Expect<ArgumentOutOfRangeException>(() => R(0d, 0d, 0d, 1d), "zero frame width");
            Expect<ArgumentOutOfRangeException>(() => R(0d, 0d, 1d, -1d), "negative frame height");
            Expect<ArgumentOutOfRangeException>(() => R(double.NaN, 0d, 1d, 1d), "NaN frame coordinate");
            Expect<ArgumentOutOfRangeException>(() => R(0d, 0d, double.PositiveInfinity, 1d), "infinite frame width");
            Expect<OverflowException>(() => R(double.MaxValue, 0d, double.MaxValue, 1d), "overflowing frame bound");
            Expect<OverflowException>(() => R(1e308d, 0d, 1d, 1d), "unrepresentable frame width increment");

            Expect<ArgumentOutOfRangeException>(() => O(double.NaN, 0d, 1d, 1d), "NaN opening coordinate");
            Expect<ArgumentOutOfRangeException>(() => O(0d, 0d, 0d, 1d), "zero opening width");
            Expect<ArgumentOutOfRangeException>(() => O(0d, 0d, 1d, 1d, -1d), "negative opening clearance");
            Expect<OverflowException>(() => O(double.MaxValue, 0d, double.MaxValue, 1d), "overflowing opening bound");
            Expect<OverflowException>(() => O(1e308d, 0d, 1d, 1d), "unrepresentable opening width increment");
            Expect<OverflowException>(() => O(0d, 0d, 1e308d, 1d, double.MaxValue), "overflowing opening clearance");
        }

        private static void NoOverlapAndFullCoverAreStable()
        {
            var frame = R(0d, 0d, 10d, 10d);
            var edge = CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[] { O(10d, 3d, 2d, 2d) });
            AssertRects(edge, new[] { frame }, "edge-touch passthrough");
            var corner = CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[] { O(10d, 10d, 2d, 2d) });
            AssertRects(corner, new[] { frame }, "corner-touch passthrough");
            var removed = CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[] { O(-1d, -1d, 12d, 12d) });
            if (removed.Count != 0) throw new InvalidOperationException("A fully covering opening must eliminate the frame rectangle.");
        }

        private static void CentralAndPartialCutsAreDeterministic()
        {
            var frame = R(0d, 0d, 10d, 10d);
            var central = CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[] { O(4d, 4d, 2d, 2d) });
            AssertRects(central, new[]
            {
                R(0d, 0d, 4d, 10d), R(6d, 0d, 4d, 10d),
                R(4d, 0d, 2d, 4d), R(4d, 6d, 2d, 4d)
            }, "central cut");
            AssertArea(central, 96d, "central cut area");

            var partial = CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[] { O(-1d, 3d, 4d, 2d) });
            AssertRects(partial, new[]
            {
                R(3d, 0d, 7d, 10d), R(0d, 0d, 3d, 3d), R(0d, 5d, 3d, 5d)
            }, "left-edge partial cut");
            AssertArea(partial, 94d, "partial cut area");

            var repeated = CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[] { O(4d, 4d, 2d, 2d) });
            AssertRects(repeated, central, "repeat determinism");
        }

        private static void ClearanceExpandsTheInterruptedEnvelope()
        {
            var frame = R(0d, 0d, 10d, 10d);
            var cleared = CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[] { O(4d, 4d, 2d, 2d, 1d) });
            AssertRects(cleared, new[]
            {
                R(0d, 0d, 3d, 10d), R(7d, 0d, 3d, 10d),
                R(3d, 0d, 4d, 3d), R(3d, 7d, 4d, 3d)
            }, "clearance-expanded cut");
            AssertArea(cleared, 84d, "clearance-expanded cut area");
        }

        private static void OpeningOrderPreservesCanonicalGeometry()
        {
            var frame = R(0d, 0d, 12d, 10d);
            var first = O(2d, 2d, 2d, 3d);
            var second = O(7d, 4d, 3d, 2d);
            var forward = CanonicalKeys(CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[] { first, second }));
            var reverse = CanonicalKeys(CurtainFrameOpeningPlanner.Interrupt(new[] { frame }, new[] { second, first }));
            if (forward.Length != reverse.Length) throw new InvalidOperationException("Opening order changed fragment count.");
            for (var i = 0; i < forward.Length; i++)
                if (!string.Equals(forward[i], reverse[i], StringComparison.Ordinal))
                    throw new InvalidOperationException("Opening order changed canonical interrupted geometry at fragment " + i + ".");
        }

        private static void InputCeilingsStopLazyTraversalAtBoundaryPlusOne()
        {
            var exactFrames = new RepeatingFrames(20000);
            var acceptedFrames = CurtainFrameOpeningPlanner.Interrupt(exactFrames, Array.Empty<CurtainOpeningRect>());
            if (acceptedFrames.Count != 20000 || exactFrames.Yields != 20000)
                throw new InvalidOperationException("Exact frame boundary must accept exactly 20000 frames.");

            var tooManyFrames = new RepeatingFrames(20002);
            Expect<InvalidOperationException>(() => CurtainFrameOpeningPlanner.Interrupt(tooManyFrames, Array.Empty<CurtainOpeningRect>()), "frame boundary+1");
            if (tooManyFrames.Yields != 20001)
                throw new InvalidOperationException("Frame boundary+1 refusal must not over-read.");

            var exactOpenings = new RepeatingOpenings(4096);
            var noFrames = CurtainFrameOpeningPlanner.Interrupt(Array.Empty<CurtainWallRect>(), exactOpenings);
            if (noFrames.Count != 0 || exactOpenings.Yields != 4096)
                throw new InvalidOperationException("Exact opening boundary must accept exactly 4096 openings.");

            var tooManyOpenings = new RepeatingOpenings(4098);
            Expect<InvalidOperationException>(() => CurtainFrameOpeningPlanner.Interrupt(Array.Empty<CurtainWallRect>(), tooManyOpenings), "opening boundary+1");
            if (tooManyOpenings.Yields != 4097)
                throw new InvalidOperationException("Opening boundary+1 refusal must not over-read.");
        }

        private static void FragmentCeilingFailsClosed()
        {
            var frames = new CurtainWallRect[5001];
            for (var i = 0; i < frames.Length; i++) frames[i] = R(0d, 0d, 10d, 10d);
            Expect<InvalidOperationException>(() => CurtainFrameOpeningPlanner.Interrupt(frames, new[] { O(4d, 4d, 2d, 2d) }), "fragment safety boundary+1");
        }

        private static void ReturnedSnapshotIsIsolatedFromCallerMutation()
        {
            var original = R(0d, 0d, 10d, 10d);
            var frames = new List<CurtainWallRect> { original };
            var openings = new List<CurtainOpeningRect>();
            var result = CurtainFrameOpeningPlanner.Interrupt(frames, openings);
            frames[0] = R(100d, 100d, 1d, 1d);
            openings.Add(O(0d, 0d, 20d, 20d));
            AssertRects(result, new[] { original }, "returned snapshot isolation");
        }

        private static CurtainWallRect R(double x, double z, double width, double height) => new CurtainWallRect(x, z, width, height);
        private static CurtainOpeningRect O(double x, double z, double width, double height, double clearance = 0d) => new CurtainOpeningRect(x, z, width, height, clearance);

        private static void AssertArea(IReadOnlyList<CurtainWallRect> rects, double expected, string label)
        {
            var area = 0d;
            for (var i = 0; i < rects.Count; i++) area += rects[i].AreaM2;
            if (area != expected) throw new InvalidOperationException(label + " mismatch: " + area + " != " + expected + ".");
        }

        private static void AssertRects(IReadOnlyList<CurtainWallRect> actual, IReadOnlyList<CurtainWallRect> expected, string label)
        {
            if (actual.Count != expected.Count) throw new InvalidOperationException(label + " count mismatch.");
            for (var i = 0; i < actual.Count; i++)
            {
                var a = actual[i];
                var e = expected[i];
                if (a.X_M != e.X_M || a.Z_M != e.Z_M || a.WidthM != e.WidthM || a.HeightM != e.HeightM)
                    throw new InvalidOperationException(label + " rectangle " + i + " mismatch.");
            }
        }

        private static string[] CanonicalKeys(IReadOnlyList<CurtainWallRect> rects)
        {
            var keys = new string[rects.Count];
            for (var i = 0; i < rects.Count; i++)
            {
                var r = rects[i];
                keys[i] = r.X_M.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" +
                          r.Z_M.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" +
                          r.WidthM.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" +
                          r.HeightM.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            }
            Array.Sort(keys, StringComparer.Ordinal);
            return keys;
        }

        private static void Expect<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException(label + " must fail with " + typeof(TException).Name + ".");
        }

        private sealed class RepeatingFrames : IEnumerable<CurtainWallRect>
        {
            private readonly int _count;
            internal RepeatingFrames(int count) { _count = count; }
            internal int Yields { get; private set; }
            public IEnumerator<CurtainWallRect> GetEnumerator()
            {
                for (var i = 0; i < _count; i++) { Yields++; yield return R(0d, 0d, 1d, 1d); }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class RepeatingOpenings : IEnumerable<CurtainOpeningRect>
        {
            private readonly int _count;
            internal RepeatingOpenings(int count) { _count = count; }
            internal int Yields { get; private set; }
            public IEnumerator<CurtainOpeningRect> GetEnumerator()
            {
                for (var i = 0; i < _count; i++) { Yields++; yield return O(100d, 100d, 1d, 1d); }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}