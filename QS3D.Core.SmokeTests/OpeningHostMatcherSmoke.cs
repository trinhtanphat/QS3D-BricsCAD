using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class OpeningHostMatcherSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            NearestHostWins();
            ThicknessReducesEffectiveGap();
            SimilarHostsAreAmbiguous();
            SameHostSegmentsDoNotCreateAmbiguity();
            EndpointDistanceIsHandled();
            NoMatchOutsideRange();
            InvalidInputsAreRejected();
            OversizeSourcesAreBounded();
        }

        private static void NearestHostWins()
        {
            var result = new OpeningHostMatcher().Match(new Point2(2d, 0.18d), new[]
            {
                new OpeningHostSegment("W1", new Point2(0d, 0d), new Point2(5d, 0d), 0.2d),
                new OpeningHostSegment("W2", new Point2(0d, 1d), new Point2(5d, 1d), 0.2d)
            });
            Equal(OpeningHostMatchStatus.Matched, result.Status);
            Equal("W1", result.HostElementId);
            Near(0.08d, result.GapM);
            Near(0.18d, result.CenterlineDistanceM);
        }

        private static void ThicknessReducesEffectiveGap()
        {
            var result = new OpeningHostMatcher().Match(new Point2(2d, 0.20d), new[]
            {
                new OpeningHostSegment("THICK", new Point2(0d, 0d), new Point2(5d, 0d), 0.4d)
            }, maxGapM: 0d);
            Equal(OpeningHostMatchStatus.Matched, result.Status);
            Near(0d, result.GapM);
        }

        private static void SimilarHostsAreAmbiguous()
        {
            var result = new OpeningHostMatcher().Match(new Point2(2d, 0.5d), new[]
            {
                new OpeningHostSegment("A", new Point2(0d, 0.39d), new Point2(5d, 0.39d), 0.2d),
                new OpeningHostSegment("B", new Point2(0d, 0.61d), new Point2(5d, 0.61d), 0.2d)
            }, maxGapM: 0.25d, ambiguityToleranceM: 0.02d);
            Equal(OpeningHostMatchStatus.Ambiguous, result.Status);
            True(result.HostElementId.Length > 0);
            True(result.SecondaryHostElementId.Length > 0);
            True(!string.Equals(result.HostElementId, result.SecondaryHostElementId, StringComparison.OrdinalIgnoreCase));
        }

        private static void SameHostSegmentsDoNotCreateAmbiguity()
        {
            var result = new OpeningHostMatcher().Match(new Point2(1d, 0.1d), new[]
            {
                new OpeningHostSegment("P", new Point2(0d, 0d), new Point2(1d, 0d), 0.2d),
                new OpeningHostSegment("P", new Point2(1d, 0d), new Point2(2d, 0d), 0.2d)
            }, maxGapM: 0.1d, ambiguityToleranceM: 0.1d);
            Equal(OpeningHostMatchStatus.Matched, result.Status);
            Equal("P", result.HostElementId);
            Equal(1, result.CandidateHostCount);
        }

        private static void EndpointDistanceIsHandled()
        {
            var result = new OpeningHostMatcher().Match(new Point2(5.3d, 0d), new[]
            {
                new OpeningHostSegment("END", new Point2(0d, 0d), new Point2(5d, 0d), 0.2d)
            }, maxGapM: 0.21d, ambiguityToleranceM: 0d);
            Equal(OpeningHostMatchStatus.Matched, result.Status);
            Near(0.2d, result.GapM);
            Near(5d, result.ClosestPoint.X);
            Near(0d, result.ClosestPoint.Y);
        }

        private static void NoMatchOutsideRange()
        {
            var result = new OpeningHostMatcher().Match(new Point2(0d, 2d), new[]
            {
                new OpeningHostSegment("W", new Point2(-1d, 0d), new Point2(1d, 0d), 0.2d)
            }, maxGapM: 0.25d);
            Equal(OpeningHostMatchStatus.NoMatch, result.Status);
            Equal(0, result.CandidateHostCount);
        }

        private static void InvalidInputsAreRejected()
        {
            Throws<ArgumentOutOfRangeException>(() => new OpeningHostMatcher().Match(new Point2(double.NaN, 0d), Array.Empty<OpeningHostSegment>()));
            Throws<ArgumentOutOfRangeException>(() => new OpeningHostMatcher().Match(new Point2(0d, 0d), Array.Empty<OpeningHostSegment>(), -1d));
            Throws<ArgumentOutOfRangeException>(() => new OpeningHostSegment("W", new Point2(0d, 0d), new Point2(1d, 0d), 0d));
            Throws<ArgumentException>(() => new OpeningHostSegment("W", new Point2(0d, 0d), new Point2(0d, 0d), 0.2d));
        }

        private static void OversizeSourcesAreBounded()
        {
            var yielded = 0;

            IEnumerable<OpeningHostSegment> Source()
            {
                var segment = new OpeningHostSegment("BOUND", new Point2(0d, 0d), new Point2(1d, 0d), 0.2d);
                while (true)
                {
                    yielded++;
                    if (yielded > 20001) throw new Exception("OpeningHostMatcher enumerated beyond the declared segment cap probe.");
                    yield return segment;
                }
            }

            Throws<InvalidOperationException>(() => new OpeningHostMatcher().Match(new Point2(0.5d, 0d), Source()));
            Equal(20001, yielded);
        }

        private static void Near(double expected, double actual, double tolerance = 1e-9d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
