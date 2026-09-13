using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs2DRevisionPackageReviewSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() { Run(); }

        internal static void Run()
        {
            ProjectsDeterministicRevisionReviewRows();
            ProjectsUnitSafePackageSummary();
            ProjectsClassificationZoneLayerGroupSummary();
            GroupsPackageIdentityCaseInsensitively();
            RejectsAmbiguousRetainedGroupingMetadata();
        }

        private static void ProjectsDeterministicRevisionReviewRows()
        {
            var package = BuildSamplePackage();
            var rows = new RevisionTakeoffPackageReview2D().Build(package);
            Equal(4, rows.Count, "review row count");
            Equal("M-ADDED", rows[0].MarkupId, "stable order 1");
            Equal("M-CHANGED", rows[1].MarkupId, "stable order 2");
            Equal("M-REMOVED", rows[2].MarkupId, "stable order 3");
            Equal("M-UNCHANGED", rows[3].MarkupId, "stable order 4");

            var removed = rows.Single(x => x.Kind == RevisionMarkupChangeKind.Removed);
            if (removed.EstimateEligible) throw new InvalidOperationException("Removed revision evidence must not be estimate eligible.");
            Equal("A101-R1.pdf", removed.PreviousSourceReference, "removed previous provenance");
            Equal(string.Empty, removed.CurrentSourceReference, "removed current provenance");

            var changed = rows.Single(x => x.Kind == RevisionMarkupChangeKind.Changed);
            if (!changed.EstimateEligible) throw new InvalidOperationException("Changed current evidence must be estimate eligible.");
            Near(5d, changed.PreviousQuantity, "changed previous quantity");
            Near(7d, changed.CurrentQuantity, "changed current quantity");
            Near(2d, changed.QuantityDelta, "changed signed delta");
            Equal("L02", changed.Zone, "changed zone");
            Equal("A-WALL", changed.Layer, "changed layer");
        }

        private static void ProjectsUnitSafePackageSummary()
        {
            var result = new RevisionTakeoffPackageReview2D().BuildResult(BuildSamplePackage());
            Equal("R1", result.PreviousRevision, "previous revision");
            Equal("R2", result.CurrentRevision, "current revision");
            Equal(1, result.AddedCount, "added count");
            Equal(1, result.RemovedCount, "removed count");
            Equal(1, result.ChangedCount, "changed count");
            Equal(1, result.UnchangedCount, "unchanged count");
            Equal(4, result.Rows.Count, "summary row count");
            Equal(3, result.QuantitySummaries.Count, "unit summary count");

            var each = result.QuantitySummaries.Single(x => x.Unit == "ea");
            Near(1d, each.PreviousQuantity, "ea previous");
            Near(1d, each.CurrentQuantity, "ea current");
            Near(0d, each.QuantityDelta, "ea delta");
            Near(1d, each.EstimateEligibleCurrentQuantity, "ea estimate eligible");

            var length = result.QuantitySummaries.Single(x => x.Unit == "m");
            Near(9d, length.PreviousQuantity, "m previous");
            Near(7d, length.CurrentQuantity, "m current");
            Near(-2d, length.QuantityDelta, "m delta");
            Near(7d, length.EstimateEligibleCurrentQuantity, "m estimate eligible excludes removed");

            var area = result.QuantitySummaries.Single(x => x.Unit == "m2");
            Near(0d, area.PreviousQuantity, "m2 previous");
            Near(3d, area.CurrentQuantity, "m2 current");
            Near(3d, area.QuantityDelta, "m2 delta");
            Near(3d, area.EstimateEligibleCurrentQuantity, "m2 estimate eligible");
        }

        private static void ProjectsClassificationZoneLayerGroupSummary()
        {
            var result = new RevisionTakeoffPackageReview2D().BuildResult(BuildSamplePackage());
            Equal(4, result.GroupSummaries.Count, "group summary count");

            var changed = result.GroupSummaries.Single(x => x.Classification == "ARC.WALL" && x.Zone == "L02");
            Equal("A-WALL", changed.Layer, "changed group layer");
            Equal("m", changed.Unit, "changed group unit");
            Near(5d, changed.PreviousQuantity, "changed group previous");
            Near(7d, changed.CurrentQuantity, "changed group current");
            Near(2d, changed.QuantityDelta, "changed group delta");
            Near(7d, changed.EstimateEligibleCurrentQuantity, "changed group estimate eligible");

            var removed = result.GroupSummaries.Single(x => x.Classification == "ARC.WALL" && x.Zone == "L01");
            Near(4d, removed.PreviousQuantity, "removed group previous");
            Near(0d, removed.CurrentQuantity, "removed group current");
            Near(-4d, removed.QuantityDelta, "removed group delta");
            Near(0d, removed.EstimateEligibleCurrentQuantity, "removed group excluded from estimate eligible");

            Equal("ARC.DOOR", result.GroupSummaries[0].Classification, "group stable classification order");
            Equal("ARC.FLOOR", result.GroupSummaries[1].Classification, "group stable classification order 2");
        }

        private static void GroupsPackageIdentityCaseInsensitively()
        {
            var calibration = new DrawingCalibration(1d, 1d, "m");
            var previous = Extract("R1", "A101-R1.pdf", calibration, new TakeoffMarkup2D[0]);
            var current = Extract("R2", "A101-R2.pdf", calibration, new[]
            {
                new TakeoffMarkup2D("M-1", "A101", TakeoffMeasurementKind.Length, 1d, "ARC.WALL", "L01", "A-WALL", "H-1"),
                new TakeoffMarkup2D("M-2", "A101", TakeoffMeasurementKind.Length, 2d, "arc.wall", "l01", "a-wall", "H-2")
            });

            var result = new RevisionTakeoffPackageReview2D().BuildResult(new RevisionTakeoffPackage2D(previous, current));
            Equal(1, result.GroupSummaries.Count, "case-insensitive group identity");
            Near(3d, result.GroupSummaries[0].CurrentQuantity, "case-insensitive grouped current quantity");
            Near(3d, result.GroupSummaries[0].EstimateEligibleCurrentQuantity, "case-insensitive grouped estimate quantity");
        }

        private static RevisionTakeoffPackage2D BuildSamplePackage()
        {
            var calibration = new DrawingCalibration(1d, 1d, "m");
            var previous = Extract("R1", "A101-R1.pdf", calibration, new[]
            {
                new TakeoffMarkup2D("M-REMOVED", "A101", TakeoffMeasurementKind.Length, 4d, "ARC.WALL", "L01", "A-WALL", "H-R"),
                new TakeoffMarkup2D("M-UNCHANGED", "A101", TakeoffMeasurementKind.Count, 1d, "ARC.DOOR", "L01", "A-DOOR", "H-U"),
                new TakeoffMarkup2D("M-CHANGED", "A101", TakeoffMeasurementKind.Length, 5d, "ARC.WALL", "L02", "A-WALL", "H-C")
            });
            var current = Extract("R2", "A101-R2.pdf", calibration, new[]
            {
                new TakeoffMarkup2D("M-ADDED", "A101", TakeoffMeasurementKind.Area, 3d, "ARC.FLOOR", "L01", "A-FLOR", "H-A"),
                new TakeoffMarkup2D("M-UNCHANGED", "A101", TakeoffMeasurementKind.Count, 1d, "ARC.DOOR", "L01", "A-DOOR", "H-U"),
                new TakeoffMarkup2D("M-CHANGED", "A101", TakeoffMeasurementKind.Length, 7d, "ARC.WALL", "L02", "A-WALL", "H-C")
            });
            return new RevisionTakeoffPackage2D(previous, current);
        }

        private static void RejectsAmbiguousRetainedGroupingMetadata()
        {
            var calibration = new DrawingCalibration(1d, 1d, "m");
            var previous = Extract("R1", "A101-R1.pdf", calibration, new[]
            {
                new TakeoffMarkup2D("M-1", "A101", TakeoffMeasurementKind.Length, 1d, "ARC.WALL", "L01", "A-WALL", "H-1")
            });
            var current = Extract("R2", "A101-R2.pdf", calibration, new[]
            {
                new TakeoffMarkup2D("M-1", "A101", TakeoffMeasurementKind.Length, 1d, "ARC.COLUMN", "L01", "A-WALL", "H-1")
            });

            try
            {
                new RevisionTakeoffPackageReview2D().Build(new RevisionTakeoffPackage2D(previous, current));
                throw new InvalidOperationException("Expected ambiguous retained grouping metadata to fail closed.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("grouping metadata", StringComparison.OrdinalIgnoreCase) < 0) throw;
            }
        }

        private static TakeoffSheetResult2D Extract(string revision, string source, DrawingCalibration calibration, TakeoffMarkup2D[] markups)
        {
            var sheet = new DrawingSheet2D("A101", "Plan", DrawingSheetSourceKind.Pdf, source, revision, calibration);
            return new CalibratedTakeoffEngine2D().Extract(sheet, markups);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-12) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
