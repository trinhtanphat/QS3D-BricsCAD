using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimatingRateBuildUpSmoke
    {
        [ModuleInitializer]
        internal static void RegisterAndRun()
        {
            Run();
        }

        internal static void Run()
        {
            CanonicalArithmeticAndProvenance();
            RevisionReviewApprovalLifecycle();
            InvalidProvenanceAndLifecycleFailClosed();
        }

        private static void CanonicalArithmeticAndProvenance()
        {
            var effective = Utc(2026, 2, 1);
            var sourceDate = Utc(2026, 1, 1);
            var lines = new[]
            {
                Line("MAT", EstimatingResourceCategory.Material, "Concrete material", "kg", 2m, 100m, 10m, "Q-MAT", "Supplier A", sourceDate),
                Line("LAB", EstimatingResourceCategory.Labour, "Labour", "hr", 3m, 50m, 0m, "Q-LAB", "Labour source", sourceDate),
                Line("PLANT", EstimatingResourceCategory.Plant, "Plant", "hr", 1m, 30m, 0m, "Q-PLANT", "Plant source", sourceDate),
                Line("SUB", EstimatingResourceCategory.Subcontract, "Subcontract", "ea", 1m, 100m, 0m, "Q-SUB", "Subcontractor", sourceDate)
            };

            var revision = EstimatingRateBuildUpRevision.CreateDraft(
                "EST-CONC",
                "R1",
                new CostCode("CONC"),
                "m3",
                "VND",
                effective,
                "Estimator",
                "Initial estimate",
                lines,
                10m,
                10m);

            Equal(EstimatingRevisionStatus.Draft, revision.Status, "Initial estimating revision must be Draft.");
            Equal(4, revision.Lines.Count, "Estimating resource count mismatch.");
            Equal("LAB", revision.Lines[0].ResourceCode, "Resource lines must be deterministically ordered by resource code.");
            Equal(2.2m, Find(revision.Lines, "MAT").EffectiveQuantityPerBillUnit, "Material wastage must be applied through Core decimal helpers.");
            Equal("Q-MAT", Find(revision.Lines, "MAT").Source.SourceId, "Quotation source identity was not retained.");
            Equal("Supplier A", Find(revision.Lines, "MAT").Source.Supplier, "Supplier provenance was not retained.");

            var result = revision.Evaluate();
            Equal(500m, result.DirectUnitCost, "Canonical direct unit cost mismatch.");
            Equal(50m, result.OverheadUnitCost, "Canonical overhead unit cost mismatch.");
            Equal(55m, result.ProfitUnitCost, "Canonical profit unit cost mismatch.");
            Equal(605m, result.UnitRate, "Canonical final unit rate mismatch.");
            Equal("R1", result.BuildUpId, "Revision identity must bind the canonical CostRateBuildUp result.");
        }

        private static void RevisionReviewApprovalLifecycle()
        {
            var sourceDate = Utc(2026, 1, 1);
            var line = Line("MAT", EstimatingResourceCategory.Material, "Material", "kg", 1m, 100m, 0m, "Q1", "Supplier", sourceDate);
            var draft = EstimatingRateBuildUpRevision.CreateDraft(
                "EST-1",
                "R1",
                new CostCode("ITEM"),
                "ea",
                "VND",
                Utc(2026, 2, 1),
                "Estimator",
                "Initial",
                new[] { line },
                5m,
                5m);

            Throws<InvalidOperationException>(() => draft.MarkApproved("Manager", "Approve", Utc(2026, 3, 1)));

            var reviewed = draft.MarkReviewed("Senior QS", "Checked quotation", Utc(2026, 3, 1));
            Equal(EstimatingRevisionStatus.Reviewed, reviewed.Status, "Reviewed lifecycle state mismatch.");
            Equal("Senior QS", reviewed.ReviewedBy, "Reviewer identity mismatch.");
            Throws<InvalidOperationException>(() => reviewed.MarkReviewed("Other", "Again", Utc(2026, 3, 2)));

            var approved = reviewed.MarkApproved("Commercial Manager", "Approved for tender", Utc(2026, 3, 2));
            Equal(EstimatingRevisionStatus.Approved, approved.Status, "Approved lifecycle state mismatch.");
            Equal("Commercial Manager", approved.ApprovedBy, "Approver identity mismatch.");
            Throws<InvalidOperationException>(() => approved.MarkApproved("Other", "Again", Utc(2026, 3, 3)));

            var revision2 = approved.CreateNextRevision(
                "R2",
                Utc(2026, 4, 1),
                "Estimator",
                "Supplier update",
                new[] { line },
                6m,
                5m);
            Equal(EstimatingRevisionStatus.Draft, revision2.Status, "Successor revision must restart as Draft.");
            Equal("R1", revision2.PreviousRevisionId, "Successor revision must retain deterministic previous-revision linkage.");
            True(revision2.ReviewedBy == null && revision2.ApprovedBy == null, "Successor revision must not inherit approval authority.");
        }

        private static void InvalidProvenanceAndLifecycleFailClosed()
        {
            var sourceDate = Utc(2026, 1, 1);
            var future = Line("FUT", EstimatingResourceCategory.Material, "Future", "kg", 1m, 1m, 0m, "QF", "Supplier", Utc(2026, 5, 1));
            Throws<ArgumentException>(() => EstimatingRateBuildUpRevision.CreateDraft(
                "EST-FUT",
                "R1",
                new CostCode("ITEM"),
                "ea",
                "VND",
                Utc(2026, 4, 1),
                "Estimator",
                "Invalid future source",
                new[] { future }));

            var usdSource = new EstimatingRateSource("Q-USD", "Supplier", "Quote", sourceDate, "USD");
            var usdLine = new EstimatingRateLine("USD", EstimatingResourceCategory.Material, "USD material", "kg", 1m, 1m, 0m, usdSource);
            Throws<ArgumentException>(() => EstimatingRateBuildUpRevision.CreateDraft(
                "EST-USD",
                "R1",
                new CostCode("ITEM"),
                "ea",
                "VND",
                Utc(2026, 2, 1),
                "Estimator",
                "Currency mismatch",
                new[] { usdLine }));

            Throws<ArgumentOutOfRangeException>(() => Line(
                "WASTE",
                EstimatingResourceCategory.Material,
                "Waste",
                "kg",
                1m,
                1m,
                101m,
                "Q-WASTE",
                "Supplier",
                sourceDate));

            var good = Line("MAT", EstimatingResourceCategory.Material, "Material", "kg", 1m, 1m, 0m, "Q1", "Supplier", sourceDate);
            Throws<ArgumentException>(() => EstimatingRateBuildUpRevision.CreateDraft(
                "EST-DUP",
                "R1",
                new CostCode("ITEM"),
                "ea",
                "VND",
                Utc(2026, 2, 1),
                "Estimator",
                "Duplicate",
                new[] { good, good }));
        }

        private static EstimatingRateLine Line(
            string code,
            EstimatingResourceCategory category,
            string description,
            string unit,
            decimal quantity,
            decimal rate,
            decimal wastage,
            string sourceId,
            string supplier,
            DateTime sourceDate)
        {
            return new EstimatingRateLine(
                code,
                category,
                description,
                unit,
                quantity,
                rate,
                wastage,
                new EstimatingRateSource(sourceId, supplier, "Quotation", sourceDate, "VND"));
        }

        private static EstimatingRateLine Find(IReadOnlyList<EstimatingRateLine> lines, string code)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (StringComparer.Ordinal.Equals(lines[i].ResourceCode, code))
                    return lines[i];
            }
            throw new InvalidOperationException("Missing estimating resource line: " + code + ".");
        }

        private static DateTime Utc(int year, int month, int day) =>
            new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

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
