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
            TakeoffResultUnitCanonicality();
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
            var paddedMessage = Capture<ArgumentException>(() =>
                new TakeoffResult("  H1  ", TakeoffKind.Count, 1d, "ea"));
            Contains(paddedMessage, "Takeoff handle must not contain surrounding whitespace.",
                "Padded TakeoffResult handles must fail closed instead of aliasing canonical identity.");

            var canonical = new TakeoffResult("H1", TakeoffKind.Count, 1d, "ea");
            Equal("H1", canonical.Handle, "Canonical TakeoffResult handle must be preserved exactly.");
            Equal("ea", canonical.Unit, "Canonical TakeoffResult unit must be preserved exactly.");
            Equal(TakeoffKind.Count, canonical.Kind, "TakeoffResult kind changed during handle hardening.");
            Equal(1d, canonical.Value, "TakeoffResult value changed during handle hardening.");

            var controls = new[]
            {
                '\0',
                '\u0001',
                '\t',
                '\n',
                '\u001F',
                '\u007F',
                '\u0085',
                '\u009F'
            };

            foreach (var control in controls)
            {
                var handle = "H" + control + "1";
                var message = Capture<ArgumentException>(() => new TakeoffResult(handle, TakeoffKind.Count, 1d, "ea"));
                Contains(message, "Takeoff handle must not contain control characters.",
                    "Embedded control character U+" + ((int)control).ToString("X4") + " must fail with the canonical handle diagnostic.");
            }

            var surroundingControlMessage = Capture<ArgumentException>(() =>
                new TakeoffResult("\t H2 \t", TakeoffKind.Count, 2d, "ea"));
            Contains(surroundingControlMessage, "Takeoff handle must not contain surrounding whitespace.",
                "Surrounding control whitespace must fail the canonical handle boundary before identity publication.");

            var entity = new EntitySnapshot("H3", "Point", "QTO");
            var calculated = QuantityEngine.Calculate(entity, TakeoffKind.Count, DrawingUnit.Meter);
            var traced = QuantityEngine.CalculateWithTrace(entity, TakeoffKind.Count, DrawingUnit.Meter);
            Equal("H3", calculated.Handle, "Canonical QuantityEngine handle changed unexpectedly.");
            Equal(calculated.Handle, traced.Result.Handle,
                "Trace projection must preserve canonical result handle after hardening.");
            Equal(calculated.Kind, traced.Result.Kind,
                "Trace projection kind parity changed during TakeoffResult hardening.");
            Equal(calculated.Value, traced.Result.Value,
                "Trace projection value parity changed during TakeoffResult hardening.");
            Equal(calculated.Unit, traced.Result.Unit,
                "Trace projection unit parity changed during TakeoffResult hardening.");
        }

        private static void TakeoffResultUnitCanonicality()
        {
            var canonicalUnits = new[] { "ea", "m", "m2", "m3" };
            foreach (var unit in canonicalUnits)
            {
                var result = new TakeoffResult("U-" + unit, TakeoffKind.Count, 1d, unit);
                Equal(unit, result.Unit, "Canonical TakeoffResult unit must be preserved exactly.");
            }

            var paddedUnits = new[] { " m", "m ", "\tm", "m\t", "\rm", "m\r", "\nm", "m\n" };
            foreach (var unit in paddedUnits)
            {
                var message = Capture<ArgumentException>(() => new TakeoffResult("U1", TakeoffKind.Length, 1d, unit));
                Contains(message, "Takeoff unit must not contain surrounding whitespace.",
                    "Padded TakeoffResult unit must fail closed instead of being trimmed: " + Escape(unit));
            }

            var embeddedUnits = new[] { "m 2", "m\t2", "m\r2", "m\n2", "m\u00012", "m\u007F2", "m\u00852" };
            foreach (var unit in embeddedUnits)
            {
                var message = Capture<ArgumentException>(() => new TakeoffResult("U2", TakeoffKind.Area, 1d, unit));
                Contains(message, "Takeoff unit must not contain whitespace or control characters.",
                    "Embedded whitespace/control TakeoffResult unit must fail closed: " + Escape(unit));
            }

            var nonCanonicalCaseUnits = new[] { "M", "EA", "M2", "m3X" };
            foreach (var unit in nonCanonicalCaseUnits)
            {
                var message = Capture<ArgumentException>(() => new TakeoffResult("U3", TakeoffKind.Volume, 1d, unit));
                Contains(message, "Takeoff unit must use canonical lower-case text.",
                    "Upper/mixed-case TakeoffResult unit must fail closed: " + unit);
            }

            var entity = new EntitySnapshot("U4", "Polyline", "QTO")
            {
                LengthDrawingUnits = 2d,
                AreaDrawingUnitsSquared = 3d,
                VolumeDrawingUnitsCubed = 4d
            };
            var kinds = new[] { TakeoffKind.Count, TakeoffKind.Length, TakeoffKind.Area, TakeoffKind.Volume };
            foreach (var kind in kinds)
            {
                var calculated = QuantityEngine.Calculate(entity, kind, DrawingUnit.Meter);
                var traced = QuantityEngine.CalculateWithTrace(entity, kind, DrawingUnit.Meter);
                Equal(calculated.Unit, traced.Result.Unit, kind + " result/trace unit parity changed during unit hardening.");
                Equal(calculated.Unit, traced.Trace.Unit, kind + " canonical trace unit changed during unit hardening.");
            }
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

        private static string Escape(string value)
        {
            return value.Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static void Contains(string actual, string expectedFragment, string message)
        {
            if (actual.IndexOf(expectedFragment, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
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
