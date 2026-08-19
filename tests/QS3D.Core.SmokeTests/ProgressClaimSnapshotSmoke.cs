using System;
using QS3D.Core.Cost;
using QS3D.Core.Measurement;
using QS3D.Core.Progress;

namespace QS3D.Core.SmokeTests
{
    internal static class ProgressClaimSnapshotSmoke
    {
        public static void Run()
        {
            ProjectDateRejectsTechnicalTimestamps();
            ProgressMeasurementFreezesQuantityProvenance();
            ProgressSnapshotIsDeterministic();
            ProgressDeltaExplainsChanges();
            ClaimSnapshotFreezesExistingEvaluation();
            DuplicateProgressIdentityFailsClosed();
            InvalidClaimPeriodFailsClosed();
        }

        private static void ProjectDateRejectsTechnicalTimestamps()
        {
            Equal("2026-08-19", new ProjectDate(2026, 8, 19).ToString());
            Throws<ArgumentException>(() => ProjectDate.FromDateTime(
                new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc)));
            Throws<ArgumentException>(() => ProjectDate.FromDateTime(
                new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Unspecified)));
        }

        private static void ProgressMeasurementFreezesQuantityProvenance()
        {
            var trace = Trace("wall-1", 10d, "rule-a", "1");
            var measurement = new ProgressMeasurement(
                "pm-1",
                new ProjectDate(2026, 8, 18),
                trace,
                7m,
                6m,
                evidenceReference: "site-record-17");

            Equal(10m, measurement.MeasuredQuantity);
            Equal(7m, measurement.InstalledQuantity);
            Equal(6m, measurement.AcceptedQuantity);
            Equal("m3", measurement.Unit);
            True(measurement.MeasurementFingerprint.Length == 64);
            Equal(trace.SemanticIdentity, measurement.SemanticIdentity);

            Throws<ArgumentOutOfRangeException>(() => new ProgressMeasurement(
                "pm-over",
                new ProjectDate(2026, 8, 18),
                trace,
                11m,
                6m));
            Throws<ArgumentOutOfRangeException>(() => new ProgressMeasurement(
                "pm-accept",
                new ProjectDate(2026, 8, 18),
                trace,
                5m,
                6m));
        }

        private static void ProgressSnapshotIsDeterministic()
        {
            var date = new ProjectDate(2026, 8, 19);
            var a = new ProgressMeasurement("pm-a", date, Trace("beam-1", 8d, "rule-a", "1"), 5m, 4m);
            var b = new ProgressMeasurement("pm-b", date, Trace("wall-1", 10d, "rule-a", "1"), 7m, 6m);
            var created = new DateTime(2026, 8, 19, 3, 0, 0, DateTimeKind.Utc);

            var first = new ProgressSnapshot("ps-1", 1, date, created, new[] { b, a });
            var second = new ProgressSnapshot("ps-1", 1, date, created, new[] { a, b });

            Equal(first.ToCanonicalString(), second.ToCanonicalString());
            Equal(first.CanonicalDigest, second.CanonicalDigest);
            Equal("beam-1", first.Measurements[0].SemanticIdentity);
        }

        private static void ProgressDeltaExplainsChanges()
        {
            var date = new ProjectDate(2026, 8, 19);
            var created = new DateTime(2026, 8, 19, 4, 0, 0, DateTimeKind.Utc);
            var beforeMeasurement = new ProgressMeasurement(
                "pm-1", date, Trace("wall-1", 10d, "rule-a", "1"), 5m, 3m);
            var afterMeasurement = new ProgressMeasurement(
                "pm-2", date, Trace("wall-1", 10d, "rule-a", "2"), 7m, 6m);
            var before = new ProgressSnapshot("ps-1", 1, date, created, new[] { beforeMeasurement });
            var after = new ProgressSnapshot("ps-2", 2, date, created.AddMinutes(1), new[] { afterMeasurement }, "ps-1");

            var delta = ProgressSnapshotDelta.Compare(before, after);
            Equal(1, delta.Changes.Count);
            var kind = delta.Changes[0].Kind;
            True((kind & ProgressSnapshotDeltaKind.SourceChanged) != 0);
            True((kind & ProgressSnapshotDeltaKind.InstalledQuantityChanged) != 0);
            True((kind & ProgressSnapshotDeltaKind.AcceptedQuantityChanged) != 0);
            Equal(3m, delta.Changes[0].BeforeAcceptedQuantity);
            Equal(6m, delta.Changes[0].AfterAcceptedQuantity);
        }

        private static void ClaimSnapshotFreezesExistingEvaluation()
        {
            var progressDate = new ProjectDate(2026, 8, 19);
            var progress = new ProgressSnapshot(
                "progress-1",
                1,
                progressDate,
                new DateTime(2026, 8, 19, 5, 0, 0, DateTimeKind.Utc),
                new[]
                {
                    new ProgressMeasurement(
                        "pm-claim",
                        progressDate,
                        Trace("wall-claim", 10d, "rule-claim", "1"),
                        5m,
                        5m)
                });

            var contracts = new[] { new ProgressContractItem("WALL", "m3", 10m, 2m) };
            var evaluated = new ProgressClaimService().Evaluate(
                contracts,
                new[] { new ProgressClaimLine("WALL", 2m, 3m) },
                10m);

            var claim = new ClaimSnapshot(
                "claim-snapshot-1",
                "claim-series-a",
                1,
                ClaimSnapshotState.Issued,
                new ProjectDate(2026, 8, 1),
                new ProjectDate(2026, 8, 31),
                "USD",
                progress,
                "estimate-7",
                evaluated,
                contracts,
                new DateTime(2026, 8, 19, 6, 0, 0, DateTimeKind.Utc));

            Equal(1, claim.Lines.Count);
            Equal(2m, claim.Lines[0].PreviousCertifiedQuantity);
            Equal(3m, claim.Lines[0].ClaimedThisPeriodQuantity);
            Equal(3m, claim.Lines[0].CertifiedThisPeriodQuantity);
            Equal(5m, claim.Lines[0].RemainingQuantity);
            Equal(6m, claim.GrossCertifiedThisPeriod);
            Equal(0.6m, claim.RetentionThisPeriod);
            Equal(5.4m, claim.NetCertifiedThisPeriod);
            Equal(progress.CanonicalDigest, claim.SourceProgressDigest);
            True(claim.CanonicalDigest.Length == 64);

            contracts[0] = new ProgressContractItem("WALL", "m3", 99m, 99m);
            Equal(10m, claim.Lines[0].ContractQuantity);
            Equal(2m, claim.Lines[0].UnitRate);

            var successor = new ClaimSnapshot(
                "claim-snapshot-2",
                "claim-series-a",
                2,
                ClaimSnapshotState.Issued,
                new ProjectDate(2026, 8, 1),
                new ProjectDate(2026, 8, 31),
                "USD",
                progress,
                "estimate-7",
                evaluated,
                new[] { new ProgressContractItem("WALL", "m3", 10m, 2m) },
                new DateTime(2026, 8, 19, 7, 0, 0, DateTimeKind.Utc),
                "claim-snapshot-1");
            Equal("claim-snapshot-1", successor.SupersedesSnapshotId);
            Equal("claim-snapshot-1", claim.SnapshotId);
        }

        private static void DuplicateProgressIdentityFailsClosed()
        {
            var date = new ProjectDate(2026, 8, 19);
            var trace = Trace("wall-dup", 10d, "rule-a", "1");
            Throws<ArgumentException>(() => new ProgressSnapshot(
                "ps-dup",
                1,
                date,
                new DateTime(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc),
                new[]
                {
                    new ProgressMeasurement("pm-1", date, trace, 2m, 1m),
                    new ProgressMeasurement("pm-2", date, trace, 3m, 2m)
                }));
        }

        private static void InvalidClaimPeriodFailsClosed()
        {
            var progressDate = new ProjectDate(2026, 9, 1);
            var progress = new ProgressSnapshot(
                "progress-late",
                1,
                progressDate,
                new DateTime(2026, 9, 1, 1, 0, 0, DateTimeKind.Utc),
                Array.Empty<ProgressMeasurement>());
            var contracts = new[] { new ProgressContractItem("A", "m3", 1m, 1m) };
            var evaluated = new ProgressClaimService().Evaluate(contracts, Array.Empty<ProgressClaimLine>());

            Throws<ArgumentException>(() => new ClaimSnapshot(
                "claim-late",
                "claim-series-late",
                1,
                ClaimSnapshotState.Draft,
                new ProjectDate(2026, 8, 1),
                new ProjectDate(2026, 8, 31),
                "USD",
                progress,
                "estimate-late",
                evaluated,
                contracts,
                new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc)));
        }

        private static MeasurementTrace Trace(string semanticIdentity, double value, string ruleId, string ruleVersion)
        {
            return new MeasurementTrace(
                semanticIdentity,
                "source-1",
                "NetVolumeM3",
                Array.Empty<MeasurementTraceFact>(),
                value,
                Array.Empty<MeasurementTraceAdjustment>(),
                value,
                "m3",
                "none",
                ruleId: ruleId,
                ruleVersion: ruleVersion);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception("Expected exception: " + typeof(T).FullName + ".");
        }
    }
}
