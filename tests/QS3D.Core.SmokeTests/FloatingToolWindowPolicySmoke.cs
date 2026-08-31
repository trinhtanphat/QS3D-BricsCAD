using System;
using System.Collections;
using System.Collections.Generic;
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
            NonRepresentableWorkAreaEdgesFailClosed();
            ExactWorkAreaBoundaryIsAccepted();
            KnownOversizedWorkAreaCollectionFailsBeforeEnumeration();
            NonGenericKnownOversizedWorkAreaCollectionFailsBeforeEnumeration();
            KnownCountOverrunFailsAtFirstUnexpectedEntry();
            KnownCountUnderYieldFailsClosed();
            KnownCountDriftAfterTraversalFailsClosed();
            StreamingWorkAreaBoundaryFailsWithoutOverread();
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
            ExpectNoValidWorkArea(Array.Empty<FloatingToolBounds>(),
                "Floating tool normalization must fail closed without a valid visible work area.");
        }

        private static void NonRepresentableWorkAreaEdgesFailClosed()
        {
            ExpectNoValidWorkArea(
                new[] { new FloatingToolBounds(double.MaxValue, 0d, double.MaxValue, 600d) },
                "Work areas whose right edge overflows must fail closed.");
            ExpectNoValidWorkArea(
                new[] { new FloatingToolBounds(0d, double.MaxValue, 800d, double.MaxValue) },
                "Work areas whose bottom edge overflows must fail closed.");
            ExpectNoValidWorkArea(
                new[] { new FloatingToolBounds(9007199254740992d, 0d, 1d, 600d) },
                "Positive width that cannot advance the represented right edge must fail closed.");
            ExpectNoValidWorkArea(
                new[] { new FloatingToolBounds(0d, 9007199254740992d, 800d, 1d) },
                "Positive height that cannot advance the represented bottom edge must fail closed.");
        }

        private static void ExpectNoValidWorkArea(IEnumerable<FloatingToolBounds> areas, string message)
        {
            try
            {
                FloatingToolWindowPolicy.Normalize(
                    new FloatingToolBounds(0d, 0d, 400d, 300d),
                    areas);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception(message);
        }

        private static void ExactWorkAreaBoundaryIsAccepted()
        {
            var areas = new FloatingToolBounds[FloatingToolWindowPolicy.MaximumVisibleWorkAreas];
            for (var i = 0; i < areas.Length; i++)
                areas[i] = new FloatingToolBounds(i * 1000d, 0d, 800d, 600d);

            var requested = new FloatingToolBounds(50d, 50d, 400d, 300d);
            var result = FloatingToolWindowPolicy.Normalize(requested, areas);
            Equal(requested, result, "exact visible-work-area boundary must remain accepted");
        }

        private static void KnownOversizedWorkAreaCollectionFailsBeforeEnumeration()
        {
            var areas = new ThrowingOversizedCollection();
            try
            {
                FloatingToolWindowPolicy.Normalize(
                    new FloatingToolBounds(0d, 0d, 400d, 300d),
                    areas);
            }
            catch (InvalidOperationException)
            {
                if (areas.EnumerationAttempted)
                    throw new Exception("Known oversized work-area input must fail before enumeration.");
                return;
            }

            throw new Exception("Known oversized work-area input must fail closed.");
        }

        private static void NonGenericKnownOversizedWorkAreaCollectionFailsBeforeEnumeration()
        {
            var areas = new ThrowingNonGenericOversizedCollection();
            try
            {
                FloatingToolWindowPolicy.Normalize(
                    new FloatingToolBounds(0d, 0d, 400d, 300d),
                    areas);
            }
            catch (InvalidOperationException)
            {
                if (areas.EnumerationAttempted)
                    throw new Exception("Non-generic known oversized work-area input must fail before enumeration.");
                return;
            }

            throw new Exception("Non-generic known oversized work-area input must fail closed.");
        }

        private static void KnownCountOverrunFailsAtFirstUnexpectedEntry()
        {
            var areas = new KnownCountOverrunCollection();
            try
            {
                FloatingToolWindowPolicy.Normalize(
                    new FloatingToolBounds(0d, 0d, 400d, 300d),
                    areas);
            }
            catch (InvalidOperationException)
            {
                if (areas.MoveNextCalls != 2)
                    throw new Exception("Known Count overrun must fail on the first unexpected traversal entry without overread.");
                return;
            }

            throw new Exception("Known Count overrun must fail closed.");
        }

        private static void KnownCountUnderYieldFailsClosed()
        {
            ExpectNoValidWorkArea(
                new KnownCountUnderYieldCollection(),
                "Known Count under-yield must fail closed before publishing normalized bounds.");
        }

        private static void KnownCountDriftAfterTraversalFailsClosed()
        {
            var areas = new KnownCountDriftCollection();
            ExpectNoValidWorkArea(
                areas,
                "Changed deterministic Count metadata after traversal must fail closed.");
            if (!areas.EnumerationCompleted)
                throw new Exception("Count-drift regression must complete the bounded traversal before rebinding Count metadata.");
        }

        private static void StreamingWorkAreaBoundaryFailsWithoutOverread()
        {
            try
            {
                FloatingToolWindowPolicy.Normalize(
                    new FloatingToolBounds(0d, 0d, 400d, 300d),
                    StreamBoundaryPlusOne());
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception("Streaming work-area input must fail at boundary+1.");
        }

        private static IEnumerable<FloatingToolBounds> StreamBoundaryPlusOne()
        {
            for (var i = 0; i <= FloatingToolWindowPolicy.MaximumVisibleWorkAreas; i++)
                yield return new FloatingToolBounds(i * 1000d, 0d, 800d, 600d);

            throw new Exception("Floating tool policy requested an item after boundary+1.");
        }

        private sealed class ThrowingOversizedCollection : IReadOnlyCollection<FloatingToolBounds>
        {
            public int Count => FloatingToolWindowPolicy.MaximumVisibleWorkAreas + 1;
            public bool EnumerationAttempted { get; private set; }

            public IEnumerator<FloatingToolBounds> GetEnumerator()
            {
                EnumerationAttempted = true;
                throw new Exception("Known oversized collection must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ThrowingNonGenericOversizedCollection : IEnumerable<FloatingToolBounds>, ICollection
        {
            public int Count => FloatingToolWindowPolicy.MaximumVisibleWorkAreas + 1;
            public bool EnumerationAttempted { get; private set; }
            public object SyncRoot => this;
            public bool IsSynchronized => false;

            public void CopyTo(Array array, int index)
            {
                throw new NotSupportedException();
            }

            public IEnumerator<FloatingToolBounds> GetEnumerator()
            {
                EnumerationAttempted = true;
                throw new Exception("Non-generic known oversized collection must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class KnownCountOverrunCollection : IReadOnlyCollection<FloatingToolBounds>
        {
            public int Count => 1;
            public int MoveNextCalls { get; private set; }

            public IEnumerator<FloatingToolBounds> GetEnumerator()
            {
                return Enumerate();
            }

            private IEnumerator<FloatingToolBounds> Enumerate()
            {
                MoveNextCalls++;
                yield return new FloatingToolBounds(0d, 0d, 800d, 600d);
                MoveNextCalls++;
                yield return new FloatingToolBounds(1000d, 0d, 800d, 600d);
                MoveNextCalls++;
                throw new Exception("Known Count overrun policy read beyond the first unexpected entry.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class KnownCountUnderYieldCollection : IReadOnlyCollection<FloatingToolBounds>
        {
            public int Count => 2;

            public IEnumerator<FloatingToolBounds> GetEnumerator()
            {
                yield return new FloatingToolBounds(0d, 0d, 800d, 600d);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class KnownCountDriftCollection : IReadOnlyCollection<FloatingToolBounds>
        {
            public bool EnumerationCompleted { get; private set; }
            public int Count => EnumerationCompleted ? 2 : 1;

            public IEnumerator<FloatingToolBounds> GetEnumerator()
            {
                return Enumerate();
            }

            private IEnumerator<FloatingToolBounds> Enumerate()
            {
                yield return new FloatingToolBounds(0d, 0d, 800d, 600d);
                EnumerationCompleted = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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