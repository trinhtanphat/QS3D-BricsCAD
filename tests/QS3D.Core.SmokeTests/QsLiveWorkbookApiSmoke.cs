using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsLiveWorkbookApiSmoke
    {
        internal static void Run()
        {
            DeterministicWorkbookCascade();
            ConflictAndCycleHandling();
            NonFiniteCascadeIsContained();
            CollisionSafeWorkbookIdentity();
            VersionedApiAuthorizationAndCaching();
        }

        private static void DeterministicWorkbookCascade()
        {
            var sources = new[]
            {
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "E1", "R2", 12d, "ifc://model/E1"),
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.DrawingHandle, "H1", "R2", 3d, "drawing://A101/H1"),
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "E2", "R1", 5d, "ifc://model/E2")
            };
            var bindings = new[]
            {
                new LiveWorkbookBinding("B1", "WB", "BOQ", "D10", "BOQ-10", LiveWorkbookSourceKind.BimElement, "E1", "R1", new string[0], 1d, 0d, 10d),
                new LiveWorkbookBinding("B2", "WB", "BOQ", "D11", "BOQ-11", LiveWorkbookSourceKind.DrawingHandle, "H1", "R2", new[] { "B1" }, 1d, 0d, 13d),
                new LiveWorkbookBinding("B3", "WB", "BOQ", "D12", "BOQ-12", LiveWorkbookSourceKind.BimElement, "E2", "R1", new string[0], 1d, 0d, 5d),
                new LiveWorkbookBinding("B4", "WB", "BOQ", "D13", "BOQ-13", LiveWorkbookSourceKind.BimElement, "MISSING", "R2", new string[0], 1d, 0d, 7d)
            };

            var reversedBindings = bindings.OrderByDescending(x => x.BindingId, StringComparer.Ordinal).ToArray();
            var reversedSources = sources.OrderByDescending(x => x.SourceId, StringComparer.Ordinal).ToArray();
            var batch = new LiveWorkbookRefreshEngine2().Refresh(reversedBindings, reversedSources, "R2");
            var b1 = batch.Results.Single(x => x.Binding.BindingId == "B1");
            var b2 = batch.Results.Single(x => x.Binding.BindingId == "B2");
            var b3 = batch.Results.Single(x => x.Binding.BindingId == "B3");
            var b4 = batch.Results.Single(x => x.Binding.BindingId == "B4");

            Equal(LiveWorkbookFreshness.Refreshed, b1.Freshness, "BIM link refresh");
            Near(12d, b1.Value, "BIM value");
            Equal(LiveWorkbookFreshness.Refreshed, b2.Freshness, "drawing cascade refresh");
            Near(15d, b2.Value, "cascade value");
            True(b2.Trace.Any(x => x.StartsWith("source:DrawingHandle:H1@R2", StringComparison.Ordinal)), "drawing trace");
            True(b2.Trace.Any(x => x.StartsWith("binding:B1=12", StringComparison.Ordinal)), "dependency trace");
            Equal(LiveWorkbookFreshness.Stale, b3.Freshness, "stale source revision");
            Equal(LiveWorkbookFreshness.MissingSource, b4.Freshness, "missing source");
            True(batch.HasBlockingFailure, "missing source blocks publish");
            True(batch.HasStaleData, "stale indicator");

            var next = b1.ToNextBinding();
            var fresh = new LiveWorkbookRefreshEngine2().Refresh(new[] { next }, sources, "R2").Results.Single();
            Equal(LiveWorkbookFreshness.Fresh, fresh.Freshness, "accepted refresh becomes fresh");
        }

        private static void ConflictAndCycleHandling()
        {
            var conflictSources = new[]
            {
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "E1", "R2", 1d, "ifc://a"),
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "E1", "R2", 2d, "ifc://b")
            };
            var binding = new LiveWorkbookBinding("B1", "WB", "BOQ", "D10", "L1", LiveWorkbookSourceKind.BimElement, "E1", "R2", new string[0], 1d, 0d, 1d);
            var conflict = new LiveWorkbookRefreshEngine2().Refresh(new[] { binding }, conflictSources, "R2").Results.Single();
            Equal(LiveWorkbookFreshness.Conflict, conflict.Freshness, "source conflict");

            var cycle = new[]
            {
                new LiveWorkbookBinding("C1", "WB", "BOQ", "E10", "L2", LiveWorkbookSourceKind.BimElement, string.Empty, string.Empty, new[] { "C2" }, 1d, 0d, 0d),
                new LiveWorkbookBinding("C2", "WB", "BOQ", "E11", "L3", LiveWorkbookSourceKind.BimElement, string.Empty, string.Empty, new[] { "C1" }, 1d, 0d, 0d)
            };
            var cycleBatch = new LiveWorkbookRefreshEngine2().Refresh(cycle, new LiveWorkbookSourceSnapshot[0], "R2");
            True(cycleBatch.Results.All(x => x.Freshness == LiveWorkbookFreshness.Error), "cycle errors");
        }

        private static void NonFiniteCascadeIsContained()
        {
            var sources = new[]
            {
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "BIG", "R2", double.MaxValue, "ifc://model/BIG"),
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "OK", "R2", 4d, "ifc://model/OK")
            };
            var bindings = new[]
            {
                new LiveWorkbookBinding("N1", "WB", "BOQ", "F10", "L10", LiveWorkbookSourceKind.BimElement, "BIG", "R2", new string[0], 2d, 0d, 11d),
                new LiveWorkbookBinding("N2", "WB", "BOQ", "F11", "L11", LiveWorkbookSourceKind.BimElement, string.Empty, string.Empty, new[] { "N1" }, 1d, 0d, 12d),
                new LiveWorkbookBinding("N3", "WB", "BOQ", "F12", "L12", LiveWorkbookSourceKind.BimElement, "OK", "R2", new string[0], 1d, 0d, 3d)
            };

            var batch = new LiveWorkbookRefreshEngine2().Refresh(bindings, sources, "R2");
            var overflow = batch.Results.Single(x => x.Binding.BindingId == "N1");
            var downstream = batch.Results.Single(x => x.Binding.BindingId == "N2");
            var independent = batch.Results.Single(x => x.Binding.BindingId == "N3");

            Equal(LiveWorkbookFreshness.Error, overflow.Freshness, "non-finite arithmetic is a binding error");
            Near(11d, overflow.Value, "non-finite binding preserves accepted value");
            True(overflow.Message.IndexOf("non-finite", StringComparison.OrdinalIgnoreCase) >= 0, "non-finite diagnostic");
            Equal(LiveWorkbookFreshness.Error, downstream.Freshness, "downstream fails closed after arithmetic error");
            True(downstream.Message.IndexOf("N1", StringComparison.OrdinalIgnoreCase) >= 0, "downstream identifies unusable upstream");
            Equal(LiveWorkbookFreshness.Refreshed, independent.Freshness, "unrelated binding still refreshes");
            Near(4d, independent.Value, "unrelated binding value");
            True(batch.HasBlockingFailure, "non-finite arithmetic blocks publication");
        }

        private static void CollisionSafeWorkbookIdentity()
        {
            var sources = new[]
            {
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "E-A", "R|1", 2d, "E"),
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "E-A", "R", 1d, "2|E"),
                new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "E-B", "R2", 5d, "ifc://B")
            };
            var bindings = new[]
            {
                new LiveWorkbookBinding("K1", "A|B", "C", "D", "L1", LiveWorkbookSourceKind.BimElement, "E-A", "R2", new string[0], 1d, 0d, 0d),
                new LiveWorkbookBinding("K2", "A", "B|C", "D", "L2", LiveWorkbookSourceKind.BimElement, "E-B", "R2", new string[0], 1d, 0d, 0d)
            };

            var batch = new LiveWorkbookRefreshEngine2().Refresh(bindings, sources, "R2");
            var first = batch.Results.Single(x => x.Binding.BindingId == "K1");
            var second = batch.Results.Single(x => x.Binding.BindingId == "K2");

            Equal(LiveWorkbookFreshness.Conflict, first.Freshness, "hostile-token source snapshots remain conflicting");
            True(first.Message.IndexOf("source", StringComparison.OrdinalIgnoreCase) >= 0, "source conflict is not misclassified as cell conflict");
            Equal(LiveWorkbookFreshness.Refreshed, second.Freshness, "distinct delimiter-bearing workbook address remains independent");
            Near(5d, second.Value, "independent workbook address quantity");
        }

        private static void VersionedApiAuthorizationAndCaching()
        {
            var api = new QsIntegrationApiV1();
            True(api.Describe().Count >= 13, "API route coverage");
            True(api.Describe().Any(x => x.Resource == QsApiResourceKind.Procurement), "procurement route");
            True(api.Describe().Any(x => x.Resource == QsApiResourceKind.WorkbookRefresh && x.Method == "POST"), "workbook refresh route");

            var snapshot = CreateSnapshot("P1", "R2");

            var unauthenticated = api.Get(new QsApiRequest("GET", "P1", QsApiResourceKind.Quantity, null!, string.Empty), snapshot);
            Equal(401, unauthenticated.StatusCode, "API authentication");

            var wrongScope = new QsApiPrincipal("powerbi", new[] { "qs3d.project.read" });
            var forbidden = api.Get(new QsApiRequest("GET", "P1", QsApiResourceKind.Quantity, wrongScope, string.Empty), snapshot);
            Equal(403, forbidden.StatusCode, "API scope authorization");

            var reader = new QsApiPrincipal("powerbi", new[] { "qs3d.quantity.read" });
            var ok = api.Get(new QsApiRequest("GET", "P1", QsApiResourceKind.Quantity, reader, string.Empty), snapshot);
            Equal(200, ok.StatusCode, "API quantity GET");
            Equal("application/json", ok.ContentType, "API content type");
            True(ok.ETag.Length > 0, "API ETag");

            var cached = api.Get(new QsApiRequest("GET", "P1", QsApiResourceKind.Quantity, reader, ok.ETag), snapshot);
            Equal(304, cached.StatusCode, "API conditional GET");

            var collisionA = api.Get(new QsApiRequest("GET", "P1-R2", QsApiResourceKind.Quantity, reader, string.Empty), CreateSnapshot("P1-R2", "X"));
            var collisionB = api.Get(new QsApiRequest("GET", "P1", QsApiResourceKind.Quantity, reader, string.Empty), CreateSnapshot("P1", "R2-X"));
            True(!string.Equals(collisionA.ETag, collisionB.ETag, StringComparison.Ordinal), "delimiter-bearing project/revision identities produce distinct ETags");

            var refreshPrincipal = new QsApiPrincipal("erp", new[] { "qs3d.workbook.refresh" });
            var missingBinding = new LiveWorkbookBinding("B9", "WB", "BOQ", "D99", "L99", LiveWorkbookSourceKind.BimElement, "MISSING", "R2", new string[0], 1d, 0d, 0d);
            var blockedBatch = new LiveWorkbookRefreshEngine2().Refresh(new[] { missingBinding }, new LiveWorkbookSourceSnapshot[0], "R2");
            var refresh = api.RefreshWorkbook(refreshPrincipal, "P1", "WB", blockedBatch);
            Equal(409, refresh.StatusCode, "conflicted workbook refresh");
            Equal("WORKBOOK_REFRESH_CONFLICT", refresh.ErrorCode, "conflicted workbook refresh code");

            var otherWorkbookBinding = new LiveWorkbookBinding("B10", "WB-OTHER", "BOQ", "D100", "L100", LiveWorkbookSourceKind.BimElement, "E1", "R2", new string[0], 1d, 0d, 0d);
            var otherWorkbookBatch = new LiveWorkbookRefreshEngine2().Refresh(
                new[] { otherWorkbookBinding },
                new[] { new LiveWorkbookSourceSnapshot(LiveWorkbookSourceKind.BimElement, "E1", "R2", 1d, "ifc://model/E1") },
                "R2");
            var wrongWorkbook = api.RefreshWorkbook(refreshPrincipal, "P1", "WB", otherWorkbookBatch);
            Equal(409, wrongWorkbook.StatusCode, "workbook route identity mismatch");
            Equal("WORKBOOK_IDENTITY_MISMATCH", wrongWorkbook.ErrorCode, "workbook route identity mismatch code");
        }

        private static QsApiProjectSnapshot CreateSnapshot(string projectId, string revision)
        {
            return new QsApiProjectSnapshot(
                new QsApiProjectDto(projectId, "Demo", revision),
                new[] { new QsApiSourceDto("E1", "IFC", revision, "model.ifc") },
                new[] { new QsApiQuantityDto("ARC.WALL", "m2", 100d, 4) },
                new[] { new QsApiBoqLineDto("L1", "ARC.WALL", "Wall", "m2", 100d) },
                new[] { new QsApiEstimateLineDto("L1", 100d, 20d) },
                new[] { new QsApiNamedDto("ARC.WALL", "Wall", "active") },
                new QsQaFinding[0],
                new[] { new QsApiRevisionDto(revision, "S2", DateTime.SpecifyKind(new DateTime(2026, 9, 12), DateTimeKind.Utc)) },
                new[] { new QsApiNamedDto("S2", "Snapshot " + revision, "current") },
                new[] { new QsApiDiffDto("R1", revision, "ARC.WALL", 5d, 100d) },
                new[] { new QsApiNamedDto("T1", "Tender 1", "open") },
                new[] { new QsApiNamedDto("PO1", "Order 1", "delivering") });
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("QsLiveWorkbookApiSmoke failed: " + message + ".");
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12) throw new InvalidOperationException("QsLiveWorkbookApiSmoke failed: " + message + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("QsLiveWorkbookApiSmoke failed: " + message + ". Expected " + expected + ", actual " + actual + ".");
        }
    }
}
