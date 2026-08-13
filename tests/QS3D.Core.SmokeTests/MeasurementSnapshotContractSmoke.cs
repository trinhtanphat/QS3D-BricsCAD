using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementSnapshotContractSmoke
    {
        internal static void Run()
        {
            DeterministicDetachedSnapshot();
            PreservesCanonicalTraceProvenance();
            DuplicateIdentityFailsClosed();
            InvalidInputFailsClosed();
        }

        private static void DeterministicDetachedSnapshot()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                var traceA = CreateTrace("SEM-A", "SRC-A", "NetAreaM2", 12.5d, "m2", "wall-area", "2");
                var traceB = CreateTrace("SEM-B", "SRC-B", "Count", 3d, "ea", null, null);
                var source = new List<MeasurementTrace> { traceB, traceA };

                CultureInfo.CurrentCulture = new CultureInfo("vi-VN");
                var left = new MeasurementSnapshot(source);
                source.Clear();
                source.Add(CreateTrace("SEM-LATE", "SRC-LATE", "Count", 99d, "ea", null, null));

                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                var right = new MeasurementSnapshot(new[] { traceA, traceB });

                Equal(2, left.Traces.Count, "Snapshot must be detached from caller list mutation.");
                Equal("SEM-A", left.Traces[0].SemanticIdentity, "Snapshot traces must use deterministic identity ordering.");
                Equal("SEM-B", left.Traces[1].SemanticIdentity, "Snapshot traces must use deterministic identity ordering.");
                Equal(left.ToCanonicalString(), right.ToCanonicalString(), "Canonical snapshot text must not depend on caller order or current culture.");
                True(left.ToCanonicalString().StartsWith("3:MS11:2", StringComparison.Ordinal), "Canonical snapshot text must use the MS1 schema and explicit trace count.");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        private static void PreservesCanonicalTraceProvenance()
        {
            var trace = CreateTrace("SEM-WALL-1", "SRC-WALL-1", "NetAreaM2", 42.25d, "m2", "wall-net-area", "7");
            var snapshot = new MeasurementSnapshot(new[] { trace });
            var frozen = snapshot.Traces[0];

            Equal("SEM-WALL-1", frozen.SemanticIdentity, "Semantic identity must be preserved.");
            Equal("SRC-WALL-1", frozen.SourceIdentity, "Source identity must be preserved.");
            Equal("NetAreaM2", frozen.QuantityKey, "Quantity identity must be preserved.");
            Equal(42.25d, frozen.NetValue, "Snapshot must preserve the canonical calculated net value without recomputation.");
            Equal("m2", frozen.Unit, "Canonical unit must be preserved.");
            Equal("wall-net-area", frozen.RuleId, "Rule identity must be preserved.");
            Equal("7", frozen.RuleVersion, "Rule version must be preserved.");
            True(snapshot.ToCanonicalString().Contains(trace.ToCanonicalString()), "Snapshot canonical text must embed the canonical trace representation rather than a parallel quantity formula.");
        }

        private static void DuplicateIdentityFailsClosed()
        {
            var original = CreateTrace("SEM-WALL-1", "SRC-WALL-1", "NetAreaM2", 12d, "m2", "wall-net-area", "1");
            var changedRule = CreateTrace("SEM-WALL-1", "SRC-WALL-1", "NetAreaM2", 11d, "m2", "wall-net-area", "2");

            Throws<ArgumentException>(() => new MeasurementSnapshot(new[] { original, changedRule }));
        }

        private static void InvalidInputFailsClosed()
        {
            Throws<ArgumentNullException>(() => new MeasurementSnapshot(null!));
            var trace = CreateTrace("SEM-A", "SRC-A", "Count", 1d, "ea", null, null);
            Throws<ArgumentException>(() => new MeasurementSnapshot(new MeasurementTrace[] { trace, null! }));
        }

        private static MeasurementTrace CreateTrace(
            string semanticIdentity,
            string sourceIdentity,
            string quantityKey,
            double netValue,
            string unit,
            string? ruleId,
            string? ruleVersion)
        {
            return new MeasurementTrace(
                semanticIdentity,
                sourceIdentity,
                quantityKey,
                Array.Empty<MeasurementTraceFact>(),
                netValue,
                Array.Empty<MeasurementTraceAdjustment>(),
                netValue,
                unit,
                "none",
                ruleId: ruleId,
                ruleVersion: ruleVersion);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
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
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
