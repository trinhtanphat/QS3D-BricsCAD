using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Cost;
using QS3D.Core.Intelligence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsIntelligenceSmoke
    {
        internal static void Run()
        {
            AutoClassification();
            QualityChecks();
            RevisionDelta();
            MissingScope();
            BoqSuggestion();
            CostImpact();
            UnifiedPipeline();
        }

        private static void AutoClassification()
        {
            var classifier = new RuleBasedQsClassifier();
            var beam = classifier.Classify(Record("B-1", "Beam", "Dầm bê tông B1", "Volume", 2d, "m3"));
            var duct = classifier.Classify(Record("D-1", "Mechanical", "Ống gió cấp SA", "Length", 5d, "m"));
            var excavation = classifier.Classify(Record("E-1", "Earthwork", "Đào đất móng", "Volume", 20d, "m3"));

            Equal("STR.BEAM", beam!.ClassificationCode, "beam classification");
            Equal("MEP.DUCT", duct!.ClassificationCode, "duct classification");
            Equal("CIVIL.EXCAVATION", excavation!.ClassificationCode, "excavation classification");
            True(beam.Confidence >= 0.68d, "beam confidence");
        }

        private static void QualityChecks()
        {
            var records = new[]
            {
                Record("A", "Wall", "Wall A", "Area", 10d, "m2", geometryFingerprint: "G-1"),
                Record("A", "Wall", "Wall duplicate id", "Area", 9d, "m2", geometryFingerprint: "G-2"),
                Record("C", "Wall", "Wall duplicate geometry", "Area", 10d, "m2", geometryFingerprint: "G-1")
            };
            var findings = new QsQualityAnalyzer().Analyze(records);

            True(findings.Any(x => x.RuleId == "QS.DUPLICATE_ELEMENT_ID"), "duplicate id finding");
            Equal(2, findings.Count(x => x.RuleId == "QS.DUPLICATE_GEOMETRY"), "duplicate geometry findings");
            Equal(3, findings.Count(x => x.RuleId == "QS.MISSING_CLASSIFICATION"), "missing classification findings");
            Equal(3, findings.Count(x => x.RuleId == "QS.MISSING_WBS"), "missing WBS findings");
            Equal(3, findings.Count(x => x.RuleId == "QS.MISSING_COST_CODE"), "missing cost code findings");
        }

        private static void RevisionDelta()
        {
            var before = new[]
            {
                Record("A", "Beam", "Beam A", "Volume", 10d, "m3", "Structure", "STR.BEAM", "WBS-1", "CONC-BEAM", "GA"),
                Record("R", "Wall", "Removed wall", "Area", 3d, "m2", "Architecture", "ARC.WALL", "WBS-2", "WALL", "GR")
            };
            var after = new[]
            {
                Record("A", "Beam", "Beam A", "Volume", 12d, "m3", "Structure", "STR.BEAM", "WBS-1", "CONC-BEAM", "GA2"),
                Record("N", "Duct", "New duct", "Length", 5d, "m", "MEP", "MEP.DUCT", "WBS-3", "DUCT", "GN")
            };

            var deltas = new QsRevisionQuantityEngine().Compare(before, after);
            Equal(3, deltas.Count, "revision delta count");
            var modified = deltas.Single(x => x.ElementId == "A");
            var removed = deltas.Single(x => x.ElementId == "R");
            var added = deltas.Single(x => x.ElementId == "N");
            Equal(QsRevisionChangeKind.Modified, modified.ChangeKind, "modified status");
            Near(2d, modified.QuantityDelta, 1e-12, "modified quantity delta");
            Equal(QsRevisionChangeKind.Removed, removed.ChangeKind, "removed status");
            Near(-3d, removed.QuantityDelta, 1e-12, "removed quantity delta");
            Equal(QsRevisionChangeKind.Added, added.ChangeKind, "added status");
            Near(5d, added.QuantityDelta, 1e-12, "added quantity delta");
        }

        private static void MissingScope()
        {
            var records = new[]
            {
                Record("A", "Beam", "Beam A", "Volume", 10d, "m3", "Structure", "STR.BEAM")
            };
            var missing = new QsMissingScopeDetector().Detect(records, new[] { "STR.BEAM", "MEP.DUCT", "CIVIL.EXCAVATION" });
            Equal(2, missing.Count, "missing scope count");
            True(missing.Any(x => x.ClassificationCode == "MEP.DUCT"), "missing MEP duct");
            True(missing.Any(x => x.ClassificationCode == "CIVIL.EXCAVATION"), "missing excavation");
        }

        private static void BoqSuggestion()
        {
            var records = new[]
            {
                Record("A", "Beam", "Beam A", "Volume", 10d, "m3", "Structure", "STR.BEAM", "WBS-1", "CONC-BEAM"),
                Record("B", "Beam", "Beam B", "Volume", 5d, "m3", "Structure", "STR.BEAM", "WBS-1", "CONC-BEAM")
            };
            var boq = new QsBoqSuggestionEngine().Build(records);
            Equal(1, boq.Count, "BOQ group count");
            Equal(2, boq[0].ElementCount, "BOQ element count");
            Near(15d, boq[0].Quantity, 1e-12, "BOQ quantity");
            Equal("STR.BEAM", boq[0].ClassificationCode, "BOQ classification");
        }

        private static void CostImpact()
        {
            var valuation = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc);
            var rateBook = BuildRateBook();
            var before = new[]
            {
                Record("A", "Beam", "Beam A", "Volume", 10d, "m3", "Structure", "STR.BEAM", "WBS-1", "CONC-BEAM")
            };
            var after = new[]
            {
                Record("A", "Beam", "Beam A", "Volume", 12d, "m3", "Structure", "STR.BEAM", "WBS-1", "CONC-BEAM"),
                Record("D", "Duct", "Supply duct", "Length", 5d, "m", "MEP", "MEP.DUCT", "WBS-2", "DUCT")
            };
            var deltas = new QsRevisionQuantityEngine().Compare(before, after);
            var impact = new QsCostImpactEngine().Evaluate(deltas, rateBook, "VND", valuation);

            Equal(2, impact.Lines.Count, "cost impact line count");
            Equal(0, impact.UnresolvedLineCount, "cost unresolved count");
            Equal(450m, impact.KnownDeltaCost, "cost delta total");
        }

        private static void UnifiedPipeline()
        {
            var valuation = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc);
            var before = new[]
            {
                Record("A", "Beam", "Dầm bê tông A", "Volume", 10d, "m3", wbsCode: "WBS-1", costCode: "CONC-BEAM", geometryFingerprint: "BEAM-A")
            };
            var after = new[]
            {
                Record("A", "Beam", "Dầm bê tông A", "Volume", 12d, "m3", wbsCode: "WBS-1", costCode: "CONC-BEAM", geometryFingerprint: "BEAM-A-R2"),
                Record("D", "Mechanical", "Ống gió cấp SA", "Length", 5d, "m", wbsCode: "WBS-2", costCode: "DUCT", geometryFingerprint: "DUCT-D")
            };

            var report = new QsIntelligencePipeline().Run(
                before,
                after,
                new[] { "STR.BEAM", "MEP.DUCT", "CIVIL.EXCAVATION" },
                BuildRateBook(),
                "VND",
                valuation);

            Equal(2, report.ClassifiedCurrentRecords.Count, "pipeline classified count");
            Equal("STR.BEAM", report.ClassifiedCurrentRecords.Single(x => x.ElementId == "A").ClassificationCode, "pipeline beam class");
            Equal("MEP.DUCT", report.ClassifiedCurrentRecords.Single(x => x.ElementId == "D").ClassificationCode, "pipeline duct class");
            Equal(1, report.MissingScopes.Count, "pipeline missing scope count");
            Equal("CIVIL.EXCAVATION", report.MissingScopes[0].ClassificationCode, "pipeline missing scope code");
            Equal(2, report.RevisionDeltas.Count, "pipeline revision count");
            Equal(2, report.BoqSuggestions.Count, "pipeline BOQ count");
            True(report.QualityFindings.All(x => x.Severity != QsQualitySeverity.Error), "pipeline no QA errors");
            Equal(450m, report.CostImpact!.KnownDeltaCost, "pipeline cost impact");
        }

        private static RateBook BuildRateBook()
        {
            var effective = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return new RateBook("QS-INTELLIGENCE", new[]
            {
                new RateItem("RATE-CONC-BEAM", new CostCode("CONC-BEAM"), "m3", "VND", 100m, effective, "2026.1"),
                new RateItem("RATE-DUCT", new CostCode("DUCT"), "m", "VND", 50m, effective, "2026.1")
            });
        }

        private static QsQuantityRecord Record(
            string id,
            string category,
            string name,
            string quantityType,
            double quantity,
            string unit,
            string discipline = "",
            string classificationCode = "",
            string wbsCode = "",
            string costCode = "",
            string geometryFingerprint = "")
        {
            return new QsQuantityRecord(
                id,
                "BricsCAD",
                category,
                name,
                quantityType,
                quantity,
                unit,
                discipline,
                classificationCode,
                wbsCode,
                costCode,
                geometryFingerprint,
                new Dictionary<string, string>());
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }
    }
}
