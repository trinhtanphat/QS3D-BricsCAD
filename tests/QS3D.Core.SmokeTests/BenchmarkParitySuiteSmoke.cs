using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class BenchmarkParitySuiteSmoke
    {
        internal static void Run()
        {
            QaGateBlocksInvalidModel();
            QsQaGate2Smoke.Run();
            QaGateRejectsNonFiniteDimensions();
            CalibratedTwoDimensionalTakeoff();
            Qs2DTakeoffWorkflowSmoke.Run();
            QsTakeoffPackageUxSmoke.Run();
            WorkbookLiveLinkRefresh();
            IntegrationRoutes();
            IntegrationApiRejectsNonFiniteEstimateAmount();
            IntegrationApiNormalizesRevisionTimeDeterministically();
            QsLiveWorkbookApiSmoke.Run();
            ConcreteAndFormwork();
            IfcWorkbench();
            QsCubicostQuantBimSmoke.Run();
            ConstructionLifecycle();
            QsTrimbleConstructionLifecycleSmoke.Run();
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

        private static void QaGateRejectsNonFiniteDimensions()
        {
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "IfcGuid", "FINITE-DIM-GUID" },
                { "IfcPset.Pset_Qto", "present" },
                { "IfcPset.Pset_Identity", "present" },
                { "IfcRel.SpatialContainer", "L01" },
                { "IfcRel.TypeAssignment", "Wall" }
            };
            var values = new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity };

            for (var i = 0; i < values.Length; i++)
            {
                var element = new QsModelElementSnapshot("NF-" + i, "Wall", "Concrete", "A-WALL", "L01", 4d, 0.2d, 3d, properties);
                SetSnapshotDimensionForDefenseTest(element, "Length", values[i]);
                var decision = new QsQaGate2().Evaluate(
                    new[] { element },
                    QsQaRuleProfile.SolibriQuantityStrict(),
                    null!,
                    new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc));

                Equal(QsQaGateStatus.Blocked, decision.Status, "non-finite QA2 dimension blocks");
                True(decision.ActiveFindings.Any(x => x.RuleId == "QA2.INVALID_DIMENSIONS" && x.ElementId == element.Id), "non-finite QA2 dimension finding");
                True(!decision.CanTakeoff && !decision.CanBoq && !decision.CanEstimate, "non-finite QA2 dimension hard gate");
            }
        }

        private static void SetSnapshotDimensionForDefenseTest(QsModelElementSnapshot element, string propertyName, double value)
        {
            var property = typeof(QsModelElementSnapshot).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            True(property != null, "snapshot dimension property exists");
            var setter = property!.GetSetMethod(true);
            True(setter != null, "snapshot dimension private setter exists");
            setter!.Invoke(element, new object[] { value });
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

        private static void IntegrationApiRejectsNonFiniteEstimateAmount()
        {
            var finite = new QsApiEstimateLineDto("L1", 4d, 2.5d);
            Near(10d, finite.Amount, 0d, "finite API estimate amount");

            var rejected = false;
            try
            {
                _ = new QsApiEstimateLineDto("L2", double.MaxValue, 2d);
            }
            catch (ArgumentOutOfRangeException)
            {
                rejected = true;
            }
            True(rejected, "API estimate amount overflow is rejected before publication");
        }

        private static void IntegrationApiNormalizesRevisionTimeDeterministically()
        {
            var unspecified = new DateTime(2026, 9, 12, 6, 7, 8, DateTimeKind.Unspecified);
            var revision = new QsApiRevisionDto("R3", "S3", unspecified);
            Equal(DateTimeKind.Utc, revision.CreatedUtc.Kind, "unspecified revision timestamp kind");
            Equal(unspecified.Ticks, revision.CreatedUtc.Ticks, "unspecified revision timestamp ticks are host-independent");

            var utc = new DateTime(2026, 9, 12, 6, 7, 8, DateTimeKind.Utc);
            Equal(utc, new QsApiRevisionDto("R4", "S4", utc).CreatedUtc, "UTC revision timestamp preserved");
        }

        private static void ConcreteAndFormwork()
        {
            var result = new ConcreteFormworkCalculator().RectangularMember(5d, 0.3d, 0.6d, true);
            Near(0.9d, result.ConcreteVolume, 1e-12, "concrete volume");
            Near(9.36d, result.FormworkArea, 1e-12, "formwork area");
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
