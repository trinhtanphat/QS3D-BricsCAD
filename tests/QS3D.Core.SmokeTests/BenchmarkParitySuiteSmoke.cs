using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class BenchmarkParitySuiteSmoke
    {
        internal static void Run()
        {
            QaGateBlocksInvalidModel();
            CalibratedTwoDimensionalTakeoff();
            WorkbookLiveLinkRefresh();
            IntegrationRoutes();
            ConcreteAndFormwork();
            IfcWorkbench();
            ConstructionLifecycle();
        }

        private static void QaGateBlocksInvalidModel()
        {
            var invalid = new QsModelElementSnapshot("E1", "", "", "", "", 4d, 0d, 3d, new Dictionary<string, string>());
            var decision = new QsQaGate().Evaluate(new[] { invalid }, QsQaProfile.StrictIfcQuantity());
            Equal(QsQaGateStatus.Blocked, decision.Status, "QA gate status");
            True(!decision.CanTakeoff, "QA blocks takeoff");
            True(decision.Findings.Any(x => x.RuleId == "QA.MISSING_MATERIAL"), "material finding");
            True(decision.Findings.Any(x => x.RuleId == "QA.MISSING_PROPERTY"), "IFC property finding");

            var props = new Dictionary<string, string> { { "IfcGuid", "G1" }, { "IfcEntity", "IfcWall" }, { "QuantityUnit", "m3" } };
            var valid = new QsModelElementSnapshot("E2", "Wall", "Concrete", "STR.WALL", "L01", 4d, 0.2d, 3d, props);
            Equal(QsQaGateStatus.Pass, new QsQaGate().Evaluate(new[] { valid }, QsQaProfile.StrictIfcQuantity()).Status, "valid QA gate");
        }

        private static void CalibratedTwoDimensionalTakeoff()
        {
            var calibration = new DrawingCalibration(100d, 10d, "m");
            var package = new TakeoffPackage("PKG-1", "Architectural GA", "R2");
            package.Add(new TakeoffMeasurement2D("M1", "A101", TakeoffMeasurementKind.Length, 250d, calibration, "ARC.WALL"));
            package.Add(new TakeoffMeasurement2D("M2", "A101", TakeoffMeasurementKind.Area, 1000d, calibration, "ARC.FLOOR"));
            package.Add(new TakeoffMeasurement2D("M3", "A101", TakeoffMeasurementKind.Count, 8d, calibration, "ARC.DOOR"));
            var inventory = package.BuildInventory();
            Near(25d, inventory.Single(x => x.Classification == "ARC.WALL").Quantity, 1e-12, "scaled length");
            Near(10d, inventory.Single(x => x.Classification == "ARC.FLOOR").Quantity, 1e-12, "scaled area");
            Near(8d, inventory.Single(x => x.Classification == "ARC.DOOR").Quantity, 1e-12, "count");
        }

        private static void WorkbookLiveLinkRefresh()
        {
            var links = new[]
            {
                new WorkbookLiveLink("WB", "BOQ", "D10", "E1", "R1", 10d),
                new WorkbookLiveLink("WB", "BOQ", "D11", "E2", "R2", 5d),
                new WorkbookLiveLink("WB", "BOQ", "D12", "E3", "R1", 2d)
            };
            var current = new Dictionary<string, double> { { "E1", 12d }, { "E2", 5d } };
            var refreshed = new WorkbookLiveLinkEngine().Refresh(links, current, "R2");
            Equal(LiveLinkRefreshState.Updated, refreshed.Single(x => x.Link.SourceId == "E1").State, "updated source");
            Equal(LiveLinkRefreshState.Current, refreshed.Single(x => x.Link.SourceId == "E2").State, "current source");
            Equal(LiveLinkRefreshState.MissingSource, refreshed.Single(x => x.Link.SourceId == "E3").State, "missing source");
        }

        private static void IntegrationRoutes()
        {
            var routes = new QsIntegrationRouteCatalog().Routes;
            Equal(4, routes.Count, "REST route count");
            True(routes.Any(x => x.Contains("quantities")), "quantity route");
            True(routes.Any(x => x.Contains("revisions")), "revision route");
            var resource = new QsIntegrationResource("P1", "R2", new List<TakeoffInventoryLine>(), new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc));
            Equal("P1", resource.ProjectId, "integration project id");
        }

        private static void ConcreteAndFormwork()
        {
            var result = new ConcreteFormworkCalculator().RectangularMember(5d, 0.3d, 0.6d, true);
            Near(0.9d, result.ConcreteVolume, 1e-12, "concrete volume");
            Near(9.6d, result.FormworkArea, 1e-12, "formwork area");
        }

        private static void IfcWorkbench()
        {
            var items = new[]
            {
                new IfcQtoItem("G1", "IfcWall", "L01", "ARC.WALL", "NetSideArea", 10d, "m2"),
                new IfcQtoItem("G2", "IfcWall", "L02", "ARC.WALL", "NetSideArea", 12d, "m2"),
                new IfcQtoItem("G3", "IfcSlab", "L01", "STR.SLAB", "NetVolume", 4d, "m3")
            };
            var workbench = new IfcQtoWorkbench();
            Equal(1, workbench.Filter(items, "IfcWall", "L01").Count, "IFC filter");
            var inventory = workbench.Aggregate(items);
            Near(22d, inventory.Single(x => x.Classification == "ARC.WALL").Quantity, 1e-12, "IFC wall aggregate");
            Near(4d, inventory.Single(x => x.Classification == "STR.SLAB").Quantity, 1e-12, "IFC slab aggregate");
        }

        private static void ConstructionLifecycle()
        {
            var commitments = new[]
            {
                new ConstructionCommitment("PO1", "Supplier A", 100m, 60m, 40m, 0.5d),
                new ConstructionCommitment("PO2", "Supplier B", 300m, 100m, 80m, 0.25d)
            };
            var summary = new ConstructionLifecycleEngine().Summarize(commitments, 50m);
            Equal(400m, summary.Committed, "committed cost");
            Equal(160m, summary.Invoiced, "invoiced cost");
            Equal(120m, summary.Paid, "paid cost");
            Equal(450m, summary.ForecastAtCompletion, "forecast at completion");
            Near(0.3125d, summary.WeightedProgress, 1e-12, "weighted progress");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }
    }
}
