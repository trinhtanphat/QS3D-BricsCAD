using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class FloatingToolWindowPolicySmoke
    {
        public static void Run()
        {
            InvalidRequestUsesVisibleCenteredDefaults();
            OversizedRequestIsClampedToWorkArea();
            OffscreenRequestIsBroughtBackIntoView();
            BestIntersectingWorkAreaWins();
            MissingVisibleWorkAreaFailsClosed();
        }

        private static void InvalidRequestUsesVisibleCenteredDefaults()
        {
            var area = new FloatingToolBounds(100d, 50d, 1200d, 800d);
            var result = FloatingToolWindowPolicy.Normalize(
                new FloatingToolBounds(double.NaN, double.NaN, double.NaN, double.NaN),
                new[] { area });

            Equal(720d, result.Width, "default width");
            Equal(520d, result.Height, "default height");
            Equal(340d, result.Left, "centered left");
            Equal(190d, result.Top, "centered top");
        }

        private static void OversizedRequestIsClampedToWorkArea()
        {
            var area = new FloatingToolBounds(0d, 0d, 640d, 480d);
            var result = FloatingToolWindowPolicy.Normalize(
                new FloatingToolBounds(-200d, -100d, 2000d, 1600d),
                new[] { area });

            Equal(area, result, "oversized bounds must clamp to the visible work area");
        }

        private static void OffscreenRequestIsBroughtBackIntoView()
        {
            var area = new FloatingToolBounds(10d, 20d, 1000d, 700d);
            var result = FloatingToolWindowPolicy.Normalize(
                new FloatingToolBounds(5000d, 4000d, 400d, 300d),
                new[] { area });

            Equal(610d, result.Left, "right clamp");
            Equal(420d, result.Top, "bottom clamp");
            Equal(400d, result.Width, "preserved width");
            Equal(300d, result.Height, "preserved height");
        }

        private static void BestIntersectingWorkAreaWins()
        {
            var left = new FloatingToolBounds(0d, 0d, 1000d, 800d);
            var right = new FloatingToolBounds(1000d, 0d, 1000d, 800d);
            var result = FloatingToolWindowPolicy.Normalize(
                new FloatingToolBounds(1350d, 100d, 500d, 400d),
                new[] { left, right });

            if (result.Left < right.Left || result.Right > right.Right)
                throw new Exception("Floating tool should normalize into the work area with the greatest intersection.");
        }

        private static void MissingVisibleWorkAreaFailsClosed()
        {
            try
            {
                FloatingToolWindowPolicy.Normalize(
                    new FloatingToolBounds(0d, 0d, 400d, 300d),
                    Array.Empty<FloatingToolBounds>());
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception("Floating tool normalization must fail closed without a valid visible work area.");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (!expected.Equals(actual))
                throw new Exception(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Equal(FloatingToolBounds expected, FloatingToolBounds actual, string label)
        {
            if (!expected.Equals(actual))
                throw new Exception(label + ".");
        }
    }
}
