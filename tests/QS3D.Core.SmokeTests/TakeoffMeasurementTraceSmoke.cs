using System;
using System.Collections.Generic;
using QS3D.Core.Model;
using QS3D.Core.Takeoff;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class TakeoffMeasurementTraceSmoke
    {
        internal static void Run()
        {
            CanonicalResultParityAndProvenance();
            SignedZeroResultParity();
            TakeoffResultHandleCanonicality();
            MissingMetricStillFailsThroughCanonicalPath();
            InvalidDrawingUnitStillFailsThroughCanonicalPath();
        }

        private static void CanonicalResultParityAndProvenance()
        {
            var entity = new EntitySnapshot("A1", "Polyline", "QTO")
            {
                LengthDrawingUnits = 2500d,
                AreaDrawingUnitsSquared = 2000000d,
                VolumeDrawingUnitsCubed = 3000000000d
            };

            AssertParity(entity, TakeoffKind.Count, DrawingUnit.Millimeter, null, 0d);
            AssertParity(entity, TakeoffKind.Length, DrawingUnit.Millimeter, "millimeter", 2500d);
            AssertParity(entity, TakeoffKind.Area, DrawingUnit.Millimeter, "millimeter2", 2000000d);
            AssertParity(entity, TakeoffKind.Volume, DrawingUnit.Millimeter, "millimeter3", 3000000000d);
        }

        private static void SignedZeroResultParity()
        {
            var direct = new TakeoffResult("Z0", TakeoffKind.Length, -0d, "m");
            PositiveZero(direct.Value, "Direct TakeoffResult retained a negative-zero value.");

            var entity = new EntitySnapshot("Z1", "Line", "QTO") { LengthDrawingUnits = -0d };
            var calculated = QuantityEngine.Calculate(entity, TakeoffKind.Length, DrawingUnit.Meter);
            PositiveZero(calculated.Value, "Canonical QuantityEngine result retained a negative-zero value.");

            var traced = QuantityEngine.CalculateWithTrace(entity, TakeoffKind.Length, DrawingUnit.Meter);
            PositiveZero(traced.Result.Value, "Traced TakeoffResult retained a negative-zero value.");
            PositiveZero(traced.Trace.GrossValue, "Trace gross zero must remain canonical.");
            PositiveZero(traced.Trace.NetValue, "Trace net zero must remain canonical.");
            Equal(traced.Trace.NetValue, traced.Result.Value, "Takeoff result and trace zero quantities must remain equal.");
        }

        private static void TakeoffResultHandleCanonicality()
        {
            var trimmed = new TakeoffResult("  H1  ", TakeoffKind.Count, 1d, "ea");
            Equal("H1", trimmed.Handle, "TakeoffResult must preserve existing surrounding-space canonicalization.");

            ExpectArgumentException(
                () => new TakeoffResult("H\n1", TakeoffKind.Count, 1d, "ea"),
                "Embedded newline in a TakeoffResult handle must fail closed.");
            ExpectArgumentException(
                () => new TakeoffResult("H\t1", TakeoffKind.Count, 1d, "ea"),
                "Embedded tab in a TakeoffResult handle must fail closed.");
            ExpectArgumentException(
                () => new TakeoffResult("H\0" + "1", TakeoffKind.Count, 1d, "ea"),
                "Embedded NUL in a TakeoffResult handle must fail closed.");

            var entity = new EntitySnapshot("H2", "Point", "QTO");
            var canonical = QuantityEngine.Calculate(entity, TakeoffKind.Count, DrawingUnit.Meter);
            var traced = QuantityEngine.CalculateWithTrace(entity, TakeoffKind.Count, DrawingUnit.Meter);
            Equal("H2", canonical.Handle, "Canonical QuantityEngine handle changed unexpectedly.");
            Equal(canonical.Handle, traced.Result.Handle, "Trace projection must preserve canonical result handle after hardening.");
        }

        private static void AssertParity(
            EntitySnapshot entity,
            TakeoffKind kind,
            DrawingUnit drawingUnit,
            string? expectedRawUnit,
            double expectedRawValue)
        {
            var canonical = QuantityEngine.Calculate(entity, kind, drawingUnit);
            var projected = QuantityEngine.CalculateWithTrace(entity, kind, drawingUnit);
            var result = projected.Result;
            var trace = projected.Trace;

            Equal(canonical.Handle, result.Handle, kind + " traced result handle must match canonical Calculate().");
            Equal(canonical.Kind, result.Kind, kind + " traced result kind must match canonical Calculate().");
            Equal(canonical.Value, result.Value, kind + " traced result value must match canonical Calculate().");
            Equal(canonical.Unit, result.Unit, kind + " traced result unit must match canonical Calculate().");

            Equal(result.Handle, trace.SemanticIdentity, kind + " raw takeoff semantic identity must remain the source Handle.");
            Equal(result.Handle, trace.SourceIdentity, kind + " raw takeoff source identity must remain the source Handle.");
            Equal(result.Kind.ToString(), trace.QuantityKey, kind + " trace quantity key mismatch.");
            Equal(result.Value, trace.GrossValue, kind + " trace gross must be the canonical result.");
            Equal(result.Value, trace.NetValue, kind + " trace net must be the canonical result.");
            Equal(result.Unit, trace.Unit, kind + " trace unit must be the canonical result unit.");
            Equal("none", trace.RoundingPolicy, kind + " raw takeoff must not invent rounding.");
            Equal(0, trace.Adjustments.Count, kind + " raw takeoff must not invent deductions/additions.");
            Equal(0, trace.Warnings.Count, kind + " raw takeoff should not invent warnings.");

            if (kind == TakeoffKind.Count)
            {
                Equal(0, trace.InputFacts.Count, "Count trace should not invent a raw metric fact.");
                Equal(0, trace.Assumptions.Count, "Count trace should not invent a conversion path.");
                return;
            }

            Equal(1, trace.InputFacts.Count, kind + " trace must expose exactly one raw drawing-unit metric fact.");
            var fact = trace.InputFacts[0];
            Equal(expectedRawValue, fact.Value, kind + " trace raw metric value mismatch.");
            Equal(expectedRawUnit, fact.Unit, kind + " trace raw metric unit mismatch.");
            Equal(entity.Handle, fact.SourceIdentity, kind + " trace raw fact source mismatch.");
            Equal(1, trace.Assumptions.Count, kind + " trace must expose exactly one conversion path assumption.");
            Equal("Conversion path: " + drawingUnit + " -> " + result.Unit, trace.Assumptions[0], kind + " conversion path mismatch.");
        }

        private static void MissingMetricStillFailsThroughCanonicalPath()
        {
            var entity = new EntitySnapshot("B2", "Line", "QTO");
            var canonicalMessage = Capture<InvalidOperationException>(() => QuantityEngine.Calculate(entity, TakeoffKind.Length, DrawingUnit.Meter));
            var tracedMessage = Capture<InvalidOperationException>(() => QuantityEngine.CalculateWithTrace(entity, TakeoffKind.Length, DrawingUnit.Meter));
            Equal(canonicalMessage, tracedMessage, "Trace projection must preserve the canonical missing-metric failure.");
        }

        private static void InvalidDrawingUnitStillFailsThroughCanonicalPath()
        {
            var entity = new EntitySnapshot("C3", "Point", "QTO");
            var invalid = (DrawingUnit)int.MaxValue;
            var canonicalMessage = Capture<ArgumentOutOfRangeException>(() => QuantityEngine.Calculate(entity, TakeoffKind.Count, invalid));
            var tracedMessage = Capture<ArgumentOutOfRangeException>(() => QuantityEngine.CalculateWithTrace(entity, TakeoffKind.Count, invalid));
            Equal(canonicalMessage, tracedMessage, "Trace projection must preserve canonical drawing-unit validation before Count.");
        }

        private static void ExpectArgumentException(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private static string Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception.Message;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void PositiveZero(double value, string message)
        {
            if (value != 0d || BitConverter.DoubleToInt64Bits(value) != 0L)
                throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
