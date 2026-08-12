using System;
using QS3D.Core.Model;
using QS3D.Core.Takeoff;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityEngineDrawingUnitValidationSmoke
    {
        public static void Run()
        {
            SupportedCountRemainsStable();
            UndefinedDrawingUnitFailsClosedForCount();
            UndefinedTakeoffKindKeepsPrecedence();
            MetricConversionRemainsStable();
        }

        private static void SupportedCountRemainsStable()
        {
            var snapshot = new EntitySnapshot("COUNT", "BlockReference", "TAKEOFF");
            var result = QuantityEngine.Calculate(snapshot, TakeoffKind.Count, DrawingUnit.Meter);

            if (result.Value != 1d || result.Kind != TakeoffKind.Count ||
                !string.Equals(result.Unit, "ea", StringComparison.Ordinal))
                throw new InvalidOperationException("Supported Count takeoff changed unexpectedly.");
        }

        private static void UndefinedDrawingUnitFailsClosedForCount()
        {
            var snapshot = new EntitySnapshot("COUNT-BAD-UNIT", "BlockReference", "TAKEOFF");
            var ex = Throws<ArgumentOutOfRangeException>(() =>
                QuantityEngine.Calculate(snapshot, TakeoffKind.Count, (DrawingUnit)int.MaxValue));

            if (!string.Equals(ex.ParamName, "drawingUnit", StringComparison.Ordinal))
                throw new InvalidOperationException("Undefined Count drawing unit must fail on drawingUnit.");
        }

        private static void UndefinedTakeoffKindKeepsPrecedence()
        {
            var snapshot = new EntitySnapshot("BAD-KIND", "BlockReference", "TAKEOFF");
            var ex = Throws<ArgumentOutOfRangeException>(() =>
                QuantityEngine.Calculate(snapshot, (TakeoffKind)int.MaxValue, (DrawingUnit)int.MaxValue));

            if (!string.Equals(ex.ParamName, "kind", StringComparison.Ordinal))
                throw new InvalidOperationException("Undefined takeoff kind must retain validation precedence.");
        }

        private static void MetricConversionRemainsStable()
        {
            var snapshot = new EntitySnapshot("LENGTH", "Line", "TAKEOFF")
            {
                LengthDrawingUnits = 1000d
            };

            var result = QuantityEngine.Calculate(snapshot, TakeoffKind.Length, DrawingUnit.Millimeter);
            if (Math.Abs(result.Value - 1d) > 1e-12d ||
                !string.Equals(result.Unit, "m", StringComparison.Ordinal))
                throw new InvalidOperationException("Supported metric takeoff conversion changed unexpectedly.");
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".",
                    ex);
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
