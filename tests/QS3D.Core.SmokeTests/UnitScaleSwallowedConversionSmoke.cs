using System;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class UnitScaleSwallowedConversionSmoke
    {
        internal static void Run()
        {
            Throws(() => UnitScale.ToMeters(double.Epsilon, DrawingUnit.Yard), "positive yard subnormal");
            Throws(() => UnitScale.ToMeters(-double.Epsilon, DrawingUnit.Yard), "negative yard subnormal");
            Throws(() => UnitScale.FromMeters(double.Epsilon, DrawingUnit.Yard), "positive meter-to-yard subnormal");
            Throws(() => UnitScale.FromMeters(-double.Epsilon, DrawingUnit.Yard), "negative meter-to-yard subnormal");
            Throws(() => UnitScale.ToSquareMeters(double.Epsilon, DrawingUnit.Yard), "square-yard subnormal");
            Throws(() => UnitScale.ToCubicMeters(double.Epsilon, DrawingUnit.Yard), "cubic-yard subnormal");

            Equal(UnitScale.ToMeters(double.Epsilon, DrawingUnit.Meter), double.Epsilon, "meter identity");
            Equal(UnitScale.FromMeters(-double.Epsilon, DrawingUnit.Meter), -double.Epsilon, "negative meter identity");
            Equal(UnitScale.ToMeters(0d, DrawingUnit.Yard), 0d, "positive zero");
            Equal(BitConverter.DoubleToInt64Bits(UnitScale.ToMeters(-0d, DrawingUnit.Yard)), 0L, "negative zero canonicalization");

            Near(UnitScale.ToMeters(2d, DrawingUnit.Yard), 1.8288d, "ordinary yards to meters");
            Near(UnitScale.ToMeters(-2d, DrawingUnit.Yard), -1.8288d, "ordinary negative yards to meters");
            Near(UnitScale.FromMeters(1.8288d, DrawingUnit.Yard), 2d, "ordinary meters to yards");
            Near(UnitScale.ToSquareMeters(2d, DrawingUnit.Yard), 2d * 0.9144d * 0.9144d, "ordinary square yards");
            Near(UnitScale.ToCubicMeters(2d, DrawingUnit.Yard), 2d * 0.9144d * 0.9144d * 0.9144d, "ordinary cubic yards");

            Throws(() => UnitScale.ToMeters(double.Epsilon, DrawingUnit.Foot), "existing underflow-to-zero guard");
            Throws(() => UnitScale.ToMeters(-double.Epsilon, DrawingUnit.Foot), "negative underflow-to-zero guard");
            Throws<ArgumentOutOfRangeException>(() => UnitScale.ToMeters(double.NaN, DrawingUnit.Yard), "NaN input guard");
            Throws<ArgumentOutOfRangeException>(() => UnitScale.ToMeters(double.PositiveInfinity, DrawingUnit.Yard), "infinite input guard");
            Throws(() => UnitScale.ToMeters(double.MaxValue, DrawingUnit.Parsec), "non-finite result guard");
        }

        private static void Throws(Action action, string name) => Throws<OverflowException>(action, name);

        private static void Throws<TException>(Action action, string name) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException(name + " should fail closed.");
        }

        private static void Near(double actual, double expected, string name)
        {
            if (Math.Abs(actual - expected) > 1e-12d)
                throw new InvalidOperationException(name + " changed unexpectedly.");
        }

        private static void Equal(double actual, double expected, string name)
        {
            if (actual != expected) throw new InvalidOperationException(name + " changed unexpectedly.");
        }

        private static void Equal(long actual, long expected, string name)
        {
            if (actual != expected) throw new InvalidOperationException(name + " changed unexpectedly.");
        }
    }
}
