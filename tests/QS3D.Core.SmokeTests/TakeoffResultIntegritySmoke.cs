using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Model;
using QS3D.Core.Takeoff;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class TakeoffResultIntegritySmoke
    {
        public static void Run()
        {
            InvalidPublicResultStateFailsClosed();
            ZeroValueRemainsValid();
            QuantityEngineResultRemainsValid();
        }

        private static void InvalidPublicResultStateFailsClosed()
        {
            Throws<ArgumentException>(() => new TakeoffResult(" ", TakeoffKind.Count, 1d, "ea"));
            Throws<ArgumentOutOfRangeException>(() => new TakeoffResult("ABCD", (TakeoffKind)999, 1d, "ea"));
            Throws<ArgumentOutOfRangeException>(() => new TakeoffResult("ABCD", TakeoffKind.Length, -1d, "m"));
            Throws<ArgumentOutOfRangeException>(() => new TakeoffResult("ABCD", TakeoffKind.Length, double.NaN, "m"));
            Throws<ArgumentOutOfRangeException>(() => new TakeoffResult("ABCD", TakeoffKind.Length, double.PositiveInfinity, "m"));
            Throws<ArgumentOutOfRangeException>(() => new TakeoffResult("ABCD", TakeoffKind.Length, double.NegativeInfinity, "m"));
            Throws<ArgumentException>(() => new TakeoffResult("ABCD", TakeoffKind.Count, 1d, " "));
        }

        private static void ZeroValueRemainsValid()
        {
            var result = new TakeoffResult("ZERO", TakeoffKind.Length, 0d, "m");
            if (result.Value != 0d || result.Kind != TakeoffKind.Length || !string.Equals(result.Unit, "m", StringComparison.Ordinal))
                throw new InvalidOperationException("Valid zero takeoff result changed unexpectedly.");
        }

        private static void QuantityEngineResultRemainsValid()
        {
            var snapshot = new EntitySnapshot("ABCD", "Line", "TAKEOFF");
            var result = QuantityEngine.Calculate(snapshot, TakeoffKind.Count, DrawingUnit.Meter);
            if (result.Value != 1d || result.Kind != TakeoffKind.Count ||
                !string.Equals(result.Handle, "ABCD", StringComparison.Ordinal) ||
                !string.Equals(result.Unit, "ea", StringComparison.Ordinal))
                throw new InvalidOperationException("QuantityEngine count takeoff contract changed unexpectedly.");
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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class TakeoffResultIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TakeoffResultIntegritySmoke.Run();
        }
    }
}
