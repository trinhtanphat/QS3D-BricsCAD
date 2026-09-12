using System;
using System.Collections.Generic;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsIntegrationApiAuthBoundarySmoke
    {
        internal static void Run()
        {
            var api = new QsIntegrationApiV1();
            var snapshot = CreateSnapshot("P1", "R2");

            var unauthenticatedExisting = api.Get(
                new QsApiRequest("GET", "P1", QsApiResourceKind.Quantity, null!, string.Empty), snapshot);
            var unauthenticatedMissing = api.Get(
                new QsApiRequest("GET", "P-MISSING", QsApiResourceKind.Quantity, null!, string.Empty), snapshot);
            Equal(401, unauthenticatedExisting.StatusCode, "unauthenticated existing project");
            Equal(401, unauthenticatedMissing.StatusCode, "unauthenticated missing project must not disclose existence");

            var wrongScope = new QsApiPrincipal("powerbi", new[] { "qs3d.project.read" });
            var forbiddenExisting = api.Get(
                new QsApiRequest("GET", "P1", QsApiResourceKind.Quantity, wrongScope, string.Empty), snapshot);
            var forbiddenMissing = api.Get(
                new QsApiRequest("GET", "P-MISSING", QsApiResourceKind.Quantity, wrongScope, string.Empty), snapshot);
            Equal(403, forbiddenExisting.StatusCode, "wrong-scope existing project");
            Equal(403, forbiddenMissing.StatusCode, "wrong-scope missing project must not disclose existence");

            var reader = new QsApiPrincipal("powerbi", new[] { "qs3d.quantity.read" });
            var authorizedMissing = api.Get(
                new QsApiRequest("GET", "P-MISSING", QsApiResourceKind.Quantity, reader, string.Empty), snapshot);
            Equal(404, authorizedMissing.StatusCode, "authorized caller retains project-not-found semantics");
            Equal("PROJECT_NOT_FOUND", authorizedMissing.ErrorCode, "authorized caller project-not-found code");
        }

        private static QsApiProjectSnapshot CreateSnapshot(string projectId, string revision)
        {
            return new QsApiProjectSnapshot(
                new QsApiProjectDto(projectId, "Demo", revision),
                new QsApiSourceDto[0],
                new QsApiQuantityDto[0],
                new QsApiBoqLineDto[0],
                new QsApiEstimateLineDto[0],
                new QsApiNamedDto[0],
                new QsQaFinding[0],
                new QsApiRevisionDto[0],
                new QsApiNamedDto[0],
                new QsApiDiffDto[0],
                new QsApiNamedDto[0],
                new QsApiNamedDto[0]);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("QsIntegrationApiAuthBoundarySmoke failed: " + message + ". Expected " + expected + ", actual " + actual + ".");
        }
    }
}
