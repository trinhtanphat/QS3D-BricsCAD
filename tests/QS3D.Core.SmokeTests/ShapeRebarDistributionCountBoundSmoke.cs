using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ShapeRebarDistributionCountBoundSmoke
    {
        private const int MaxBars = 10000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ExactLimitIsAccepted();
            FirstCountBeyondLimitIsRejected();
            PathologicalCountIsRejectedBeforeAllocation();
        }

        private static void ExactLimitIsAccepted()
        {
            var result = ShapeRebarDistributionPlanner.Plan(Input(MaxBars));
            Equal(MaxBars, result.Offsets.Count);
            Near(0.001d, result.Offsets[0]);
            Near(9.999d, result.Offsets[MaxBars - 1]);
        }

        private static void FirstCountBeyondLimitIsRejected()
        {
            Throws<ArgumentOutOfRangeException>(() => ShapeRebarDistributionPlanner.Plan(Input(MaxBars + 1)));
        }

        private static void PathologicalCountIsRejectedBeforeAllocation()
        {
            Throws<ArgumentOutOfRangeException>(() => ShapeRebarDistributionPlanner.Plan(Input(int.MaxValue)));
        }

        private static ShapeRebarDistributionInput Input(int count)
        {
            return new ShapeRebarDistributionInput
            {
                Span = 10d,
                Cover = 0d,
                Radius = 0.001d,
                Count = count,
                Centered = false
            };
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
