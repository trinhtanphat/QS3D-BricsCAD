using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Review;

namespace QS3D.Core.SmokeTests
{
    internal static class BqReviewLedgerSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ApprovalOverlayAndStaleness();
            ReviewContractValidation();
            ProjectMetadataRoundTrip();
            SnapshotRollback();
            ReservedMetadataFailsClosed();
        }

        private static void ApprovalOverlayAndStaleness()
        {
            var row = CreateRow();
            var entry = BqReviewService.CreateEntry(
                row,
                BqReviewStatus.Approved,
                "Reviewed against issued takeoff.",
                "QS-A",
                new DateTime(2026, 9, 11, 5, 0, 0, DateTimeKind.Utc),
                new[] { new BqManualAdjustment(BqReviewMetric.GrossConcreteM3, 2.5d, "Verified site instruction.") });

            Equal(true, BqReviewService.IsCurrent(entry, row), "fresh approval must be current");
            Equal(12.5d, BqReviewService.EffectiveApprovedValue(row, entry, BqReviewMetric.GrossConcreteM3), "approved overlay value");
            Equal(10d, row.GrossConcreteM3, "approved overlay must not mutate measured quantity");

            var reorderedHandles = CreateRow();
            reorderedHandles.SourceHandles.Clear();
            reorderedHandles.SourceHandles.Add("H-2");
            reorderedHandles.SourceHandles.Add("H-1");
            Equal(true, BqReviewService.IsCurrent(entry, reorderedHandles), "source-handle order must not affect signature");

            var changedQuantity = CreateRow();
            changedQuantity.GrossConcreteM3 = 11d;
            Equal(false, BqReviewService.IsCurrent(entry, changedQuantity), "quantity change must stale approval");
            Equal(11d, BqReviewService.EffectiveApprovedValue(changedQuantity, entry, BqReviewMetric.GrossConcreteM3), "stale approval must not apply overlay");

            var changedFingerprint = CreateRow();
            changedFingerprint.DrawingFingerprint = "FP-B";
            Equal(false, BqReviewService.IsCurrent(entry, changedFingerprint), "drawing fingerprint change must stale approval");

            var changedHandle = CreateRow();
            changedHandle.SourceHandles[0] = "H-X";
            Equal(false, BqReviewService.IsCurrent(entry, changedHandle), "source provenance change must stale approval");
        }

        private static void ReviewContractValidation()
        {
            var row = CreateRow();
            Throws<ArgumentException>(
                () => BqReviewService.CreateEntry(row, BqReviewStatus.Approved, string.Empty, string.Empty, DateTime.UtcNow, Array.Empty<BqManualAdjustment>()),
                "approved review requires reviewer");
            Throws<ArgumentException>(
                () => BqReviewService.CreateEntry(row, BqReviewStatus.Rejected, string.Empty, "QS-B", DateTime.UtcNow, Array.Empty<BqManualAdjustment>()),
                "rejected review requires note");
            Throws<ArgumentException>(
                () => BqReviewService.CreateEntry(
                    row,
                    BqReviewStatus.Reviewed,
                    string.Empty,
                    "QS-B",
                    DateTime.UtcNow,
                    new[] { new BqManualAdjustment(BqReviewMetric.GrossConcreteM3, -20d, "Invalid negative proposal.") }),
                "adjustment must not make quantity negative");

            var grouped = CreateRow();
            grouped.Count = 2;
            grouped.ElementIds.Add("E-2");
            Throws<InvalidOperationException>(() => BqReviewService.ElementId(grouped), "review requires one semantic detail row");
        }

        private static void ProjectMetadataRoundTrip()
        {
            var project = new ProjectState("BQ-REVIEW", "BQ review");
            var store = ProjectBqReviewLedger.Open(project);
            var entry = CreateApprovedEntry(CreateRow());
            store.Upsert(entry);
            Equal(1L, project.ChangeVersion, "ledger upsert change version");
            store.Upsert(entry);
            Equal(1L, project.ChangeVersion, "ledger deterministic no-op");

            var path = Path.Combine(Path.GetTempPath(), "qs3d-bq-review-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                new QsdbProjectStore().SaveNew(project, path);
                var loaded = new QsdbProjectStore().Load(path);
                var current = ProjectBqReviewLedger.Open(loaded).Current;
                Equal(1, current.Entries.Count, "roundtrip entry count");
                var restored = current.Find("E-1") ?? throw new Exception("BQ review entry missing after roundtrip.");
                Equal(BqReviewStatus.Approved, restored.Status, "roundtrip status");
                Equal("QS-A", restored.Reviewer, "roundtrip reviewer");
                Equal(1, restored.Adjustments.Count, "roundtrip adjustment count");
                Equal(2.5d, restored.Adjustments[0].Delta, "roundtrip adjustment delta");
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void SnapshotRollback()
        {
            var project = new ProjectState("BQ-ROLLBACK", "BQ rollback");
            var ledger = ProjectBqReviewLedger.Open(project);
            ledger.Upsert(CreateApprovedEntry(CreateRow()));
            var snapshot = ProjectStateSnapshot.Capture(project);
            var version = project.ChangeVersion;

            Equal(true, ledger.Remove("E-1"), "ledger removal before rollback");
            Equal(false, ledger.HasValue, "ledger should be empty after removal");
            snapshot.Restore(project);
            Equal(version, project.ChangeVersion, "snapshot restore change version");
            Equal(true, ProjectBqReviewLedger.Open(project).Current.Find("E-1") != null, "snapshot must restore ledger");
        }

        private static void ReservedMetadataFailsClosed()
        {
            var project = new ProjectState("BQ-INVALID", "BQ invalid");
            Throws<FormatException>(
                () => project.Metadata["QS3D.BQReview.v2.Ledger"] = "1:1",
                "unsupported BQ review reserved version");
            Equal(false, project.Metadata.ContainsKey("QS3D.BQReview.v2.Ledger"), "unsupported key must not persist");
            Equal(0L, project.ChangeVersion, "unsupported key must not touch project");

            Throws<FormatException>(
                () => project.Metadata["qs3d.bqreview.v1.ledger"] = "1:1",
                "non-canonical reserved key casing");
            Equal(0L, project.ChangeVersion, "non-canonical key must not touch project");
        }

        private static BqReviewEntry CreateApprovedEntry(QuantityReportRow row) =>
            BqReviewService.CreateEntry(
                row,
                BqReviewStatus.Approved,
                "Reviewed against issued takeoff.",
                "QS-A",
                new DateTime(2026, 9, 11, 5, 0, 0, DateTimeKind.Utc),
                new[] { new BqManualAdjustment(BqReviewMetric.GrossConcreteM3, 2.5d, "Verified site instruction.") });

        private static QuantityReportRow CreateRow()
        {
            var row = new QuantityReportRow
            {
                Floor = "L01",
                Zone = "Z-A",
                Category = "Column",
                FamilyId = "F-COL",
                FamilyName = "Column 300x300",
                ElementName = "C1",
                Material = "C30",
                Note = "Issued",
                DrawingFingerprint = "FP-A",
                Count = 1,
                GrossConcreteM3 = 10d,
                DeductionM3 = 1d,
                NetConcreteM3 = 9d,
                FormworkM2 = 20d,
                LengthM = 3d,
                WidthM = 0.3d,
                HeightM = 3d,
                OuterPerimeterM = 1.2d,
                InnerPerimeterM = 0d,
                DoorAreaM2 = 0d,
                SideAreaM2 = 0d,
                BottomAreaM2 = 0d,
                TopAreaM2 = 0d,
                OtherAreaM2 = 0d,
                HasGrossConcreteM3Evidence = true,
                HasDeductionM3Evidence = true,
                HasNetConcreteM3Evidence = true,
                HasFormworkM2Evidence = true,
                HasLengthMEvidence = true,
                HasWidthMEvidence = true,
                HasHeightMEvidence = true,
                HasOuterPerimeterMEvidence = true,
                HasInnerPerimeterMEvidence = false,
                HasDoorAreaM2Evidence = false,
                HasSideAreaM2Evidence = false,
                HasBottomAreaM2Evidence = false,
                HasTopAreaM2Evidence = false,
                HasOtherAreaM2Evidence = false,
                DensityKgM3 = 2400d,
                MassKg = 21600d
            };
            row.ElementIds.Add("E-1");
            row.SourceHandles.Add("H-1");
            row.SourceHandles.Add("H-2");
            return row;
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("BqReviewLedgerSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            catch (Exception ex)
            {
                throw new Exception("BqReviewLedgerSmoke " + label + " returned " + ex.GetType().Name + " instead of " + typeof(TException).Name + ".", ex);
            }
            throw new Exception("BqReviewLedgerSmoke " + label + " expected " + typeof(TException).Name + ".");
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
