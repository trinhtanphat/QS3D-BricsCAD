using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QS3D.Core.BenchmarkParity
{
    public enum QsApiResourceKind
    {
        Project,
        ModelSource,
        Quantity,
        Boq,
        Estimate,
        Classification,
        Qa,
        Revision,
        Snapshot,
        Diff,
        Tender,
        Procurement,
        WorkbookRefresh
    }

    public sealed class QsApiPrincipal
    {
        public QsApiPrincipal(string subject, IEnumerable<string> scopes)
        {
            Subject = QsModelElementSnapshot.Require(subject, "subject");
            Scopes = new ReadOnlyCollection<string>((scopes ?? Enumerable.Empty<string>())
                .Select(x => QsModelElementSnapshot.Require(x, "scopes"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList());
        }

        public string Subject { get; private set; }
        public IReadOnlyList<string> Scopes { get; private set; }

        public bool HasScope(string scope)
        {
            return Scopes.Contains(scope, StringComparer.OrdinalIgnoreCase) || Scopes.Contains("qs3d.admin", StringComparer.OrdinalIgnoreCase);
        }
    }

    public sealed class QsApiEndpointDescriptor
    {
        public QsApiEndpointDescriptor(string method, string routeTemplate, QsApiResourceKind resource, string requiredScope, bool cacheable)
        {
            Method = QsModelElementSnapshot.Require(method, "method").ToUpperInvariant();
            RouteTemplate = QsModelElementSnapshot.Require(routeTemplate, "routeTemplate");
            Resource = resource;
            RequiredScope = QsModelElementSnapshot.Require(requiredScope, "requiredScope");
            Cacheable = cacheable;
        }

        public string Method { get; private set; }
        public string RouteTemplate { get; private set; }
        public QsApiResourceKind Resource { get; private set; }
        public string RequiredScope { get; private set; }
        public bool Cacheable { get; private set; }
    }

    public sealed class QsIntegrationApiV1Catalog
    {
        private static readonly IReadOnlyList<QsApiEndpointDescriptor> AllRoutes = new ReadOnlyCollection<QsApiEndpointDescriptor>(new[]
        {
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}", QsApiResourceKind.Project, "qs3d.project.read", true),
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/sources", QsApiResourceKind.ModelSource, "qs3d.source.read", true),
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/quantities", QsApiResourceKind.Quantity, "qs3d.quantity.read", true),
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/boq", QsApiResourceKind.Boq, "qs3d.boq.read", true),
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/estimates", QsApiResourceKind.Estimate, "qs3d.cost.read", true),
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/classifications", QsApiResourceKind.Classification, "qs3d.classification.read", true),
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/qa", QsApiResourceKind.Qa, "qs3d.qa.read", false),
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/revisions", QsApiResourceKind.Revision, "qs3d.revision.read", true),
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/snapshots", QsApiResourceKind.Snapshot, "qs3d.revision.read", true),
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/diffs", QsApiResourceKind.Diff, "qs3d.revision.read", true),
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/tenders", QsApiResourceKind.Tender, "qs3d.tender.read", true),
            new QsApiEndpointDescriptor("GET", "/api/v1/projects/{projectId}/procurement", QsApiResourceKind.Procurement, "qs3d.procurement.read", true),
            new QsApiEndpointDescriptor("POST", "/api/v1/projects/{projectId}/workbooks/{workbookId}/refresh", QsApiResourceKind.WorkbookRefresh, "qs3d.workbook.refresh", false)
        });

        public IReadOnlyList<QsApiEndpointDescriptor> Routes { get { return AllRoutes; } }

        public QsApiEndpointDescriptor Find(string method, QsApiResourceKind resource)
        {
            method = QsModelElementSnapshot.Require(method, "method").ToUpperInvariant();
            return AllRoutes.SingleOrDefault(x => x.Method == method && x.Resource == resource);
        }
    }

    public sealed class QsApiProjectDto
    {
        public QsApiProjectDto(string projectId, string name, string currentRevision)
        {
            ProjectId = QsModelElementSnapshot.Require(projectId, "projectId");
            Name = QsModelElementSnapshot.Require(name, "name");
            CurrentRevision = QsModelElementSnapshot.Require(currentRevision, "currentRevision");
        }
        public string ProjectId { get; private set; }
        public string Name { get; private set; }
        public string CurrentRevision { get; private set; }
    }

    public sealed class QsApiSourceDto
    {
        public QsApiSourceDto(string sourceId, string sourceKind, string revision, string reference)
        {
            SourceId = QsModelElementSnapshot.Require(sourceId, "sourceId");
            SourceKind = QsModelElementSnapshot.Require(sourceKind, "sourceKind");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            Reference = QsModelElementSnapshot.Require(reference, "reference");
        }
        public string SourceId { get; private set; }
        public string SourceKind { get; private set; }
        public string Revision { get; private set; }
        public string Reference { get; private set; }
    }

    public sealed class QsApiQuantityDto
    {
        public QsApiQuantityDto(string classification, string unit, double quantity, int sourceCount)
        {
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            Unit = QsModelElementSnapshot.Require(unit, "unit");
            Quantity = QsModelElementSnapshot.Finite(quantity, "quantity");
            if (sourceCount < 0) throw new ArgumentOutOfRangeException("sourceCount");
            SourceCount = sourceCount;
        }
        public string Classification { get; private set; }
        public string Unit { get; private set; }
        public double Quantity { get; private set; }
        public int SourceCount { get; private set; }
    }

    public sealed class QsApiBoqLineDto
    {
        public QsApiBoqLineDto(string lineId, string classification, string description, string unit, double quantity)
        {
            LineId = QsModelElementSnapshot.Require(lineId, "lineId");
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            Description = QsModelElementSnapshot.Require(description, "description");
            Unit = QsModelElementSnapshot.Require(unit, "unit");
            Quantity = QsModelElementSnapshot.Finite(quantity, "quantity");
        }
        public string LineId { get; private set; }
        public string Classification { get; private set; }
        public string Description { get; private set; }
        public string Unit { get; private set; }
        public double Quantity { get; private set; }
    }

    public sealed class QsApiEstimateLineDto
    {
        public QsApiEstimateLineDto(string lineId, double quantity, double unitRate)
        {
            LineId = QsModelElementSnapshot.Require(lineId, "lineId");
            Quantity = QsModelElementSnapshot.Finite(quantity, "quantity");
            UnitRate = QsModelElementSnapshot.Finite(unitRate, "unitRate");
        }
        public string LineId { get; private set; }
        public double Quantity { get; private set; }
        public double UnitRate { get; private set; }
        public double Amount { get { return Quantity * UnitRate; } }
    }

    public sealed class QsApiNamedDto
    {
        public QsApiNamedDto(string id, string name, string status)
        {
            Id = QsModelElementSnapshot.Require(id, "id");
            Name = QsModelElementSnapshot.Require(name, "name");
            Status = QsModelElementSnapshot.Optional(status);
        }
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Status { get; private set; }
    }

    public sealed class QsApiRevisionDto
    {
        public QsApiRevisionDto(string revision, string snapshotId, DateTime createdUtc)
        {
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            SnapshotId = QsModelElementSnapshot.Require(snapshotId, "snapshotId");
            CreatedUtc = createdUtc.Kind == DateTimeKind.Utc ? createdUtc : createdUtc.ToUniversalTime();
        }
        public string Revision { get; private set; }
        public string SnapshotId { get; private set; }
        public DateTime CreatedUtc { get; private set; }
    }

    public sealed class QsApiDiffDto
    {
        public QsApiDiffDto(string fromRevision, string toRevision, string classification, double quantityDelta, double costDelta)
        {
            FromRevision = QsModelElementSnapshot.Require(fromRevision, "fromRevision");
            ToRevision = QsModelElementSnapshot.Require(toRevision, "toRevision");
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            QuantityDelta = QsModelElementSnapshot.Finite(quantityDelta, "quantityDelta");
            CostDelta = QsModelElementSnapshot.Finite(costDelta, "costDelta");
        }
        public string FromRevision { get; private set; }
        public string ToRevision { get; private set; }
        public string Classification { get; private set; }
        public double QuantityDelta { get; private set; }
        public double CostDelta { get; private set; }
    }

    public sealed class QsApiProjectSnapshot
    {
        public QsApiProjectSnapshot(
            QsApiProjectDto project,
            IEnumerable<QsApiSourceDto> sources,
            IEnumerable<QsApiQuantityDto> quantities,
            IEnumerable<QsApiBoqLineDto> boq,
            IEnumerable<QsApiEstimateLineDto> estimates,
            IEnumerable<QsApiNamedDto> classifications,
            IEnumerable<QsQaFinding> qa,
            IEnumerable<QsApiRevisionDto> revisions,
            IEnumerable<QsApiNamedDto> snapshots,
            IEnumerable<QsApiDiffDto> diffs,
            IEnumerable<QsApiNamedDto> tenders,
            IEnumerable<QsApiNamedDto> procurement)
        {
            Project = project ?? throw new ArgumentNullException("project");
            Sources = Freeze(sources);
            Quantities = Freeze(quantities);
            Boq = Freeze(boq);
            Estimates = Freeze(estimates);
            Classifications = Freeze(classifications);
            Qa = Freeze(qa);
            Revisions = Freeze(revisions);
            Snapshots = Freeze(snapshots);
            Diffs = Freeze(diffs);
            Tenders = Freeze(tenders);
            Procurement = Freeze(procurement);
        }

        public QsApiProjectDto Project { get; private set; }
        public IReadOnlyList<QsApiSourceDto> Sources { get; private set; }
        public IReadOnlyList<QsApiQuantityDto> Quantities { get; private set; }
        public IReadOnlyList<QsApiBoqLineDto> Boq { get; private set; }
        public IReadOnlyList<QsApiEstimateLineDto> Estimates { get; private set; }
        public IReadOnlyList<QsApiNamedDto> Classifications { get; private set; }
        public IReadOnlyList<QsQaFinding> Qa { get; private set; }
        public IReadOnlyList<QsApiRevisionDto> Revisions { get; private set; }
        public IReadOnlyList<QsApiNamedDto> Snapshots { get; private set; }
        public IReadOnlyList<QsApiDiffDto> Diffs { get; private set; }
        public IReadOnlyList<QsApiNamedDto> Tenders { get; private set; }
        public IReadOnlyList<QsApiNamedDto> Procurement { get; private set; }

        private static IReadOnlyList<T> Freeze<T>(IEnumerable<T> values)
        {
            return new ReadOnlyCollection<T>((values ?? Enumerable.Empty<T>()).ToList());
        }
    }

    public sealed class QsApiRequest
    {
        public QsApiRequest(string method, string projectId, QsApiResourceKind resource, QsApiPrincipal principal, string ifNoneMatch)
        {
            Method = QsModelElementSnapshot.Require(method, "method").ToUpperInvariant();
            ProjectId = QsModelElementSnapshot.Require(projectId, "projectId");
            Resource = resource;
            Principal = principal;
            IfNoneMatch = QsModelElementSnapshot.Optional(ifNoneMatch);
        }
        public string Method { get; private set; }
        public string ProjectId { get; private set; }
        public QsApiResourceKind Resource { get; private set; }
        public QsApiPrincipal Principal { get; private set; }
        public string IfNoneMatch { get; private set; }
    }

    public sealed class QsApiResponse
    {
        public QsApiResponse(int statusCode, string apiVersion, string etag, object? body, string errorCode)
        {
            StatusCode = statusCode;
            ApiVersion = QsModelElementSnapshot.Require(apiVersion, "apiVersion");
            ETag = QsModelElementSnapshot.Optional(etag);
            Body = body;
            ErrorCode = QsModelElementSnapshot.Optional(errorCode);
        }
        public int StatusCode { get; private set; }
        public string ApiVersion { get; private set; }
        public string ETag { get; private set; }
        public object? Body { get; private set; }
        public string ErrorCode { get; private set; }
        public string ContentType { get { return "application/json"; } }
    }

    public sealed class QsIntegrationApiV1
    {
        public const string Version = "1.0";
        private readonly QsIntegrationApiV1Catalog _catalog = new QsIntegrationApiV1Catalog();

        public IReadOnlyList<QsApiEndpointDescriptor> Describe()
        {
            return _catalog.Routes;
        }

        public QsApiResponse Get(QsApiRequest request, QsApiProjectSnapshot snapshot)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (!string.Equals(request.ProjectId, snapshot.Project.ProjectId, StringComparison.OrdinalIgnoreCase))
                return new QsApiResponse(404, Version, string.Empty, null, "PROJECT_NOT_FOUND");

            var endpoint = _catalog.Find(request.Method, request.Resource);
            if (endpoint == null || request.Method != "GET")
                return new QsApiResponse(405, Version, string.Empty, null, "METHOD_NOT_ALLOWED");
            if (request.Principal == null)
                return new QsApiResponse(401, Version, string.Empty, null, "UNAUTHENTICATED");
            if (!request.Principal.HasScope(endpoint.RequiredScope))
                return new QsApiResponse(403, Version, string.Empty, null, "FORBIDDEN");

            var etag = BuildEtag(snapshot.Project.ProjectId, snapshot.Project.CurrentRevision, request.Resource);
            if (endpoint.Cacheable && string.Equals(request.IfNoneMatch, etag, StringComparison.Ordinal))
                return new QsApiResponse(304, Version, etag, null, string.Empty);

            object body;
            switch (request.Resource)
            {
                case QsApiResourceKind.Project: body = snapshot.Project; break;
                case QsApiResourceKind.ModelSource: body = snapshot.Sources; break;
                case QsApiResourceKind.Quantity: body = snapshot.Quantities; break;
                case QsApiResourceKind.Boq: body = snapshot.Boq; break;
                case QsApiResourceKind.Estimate: body = snapshot.Estimates; break;
                case QsApiResourceKind.Classification: body = snapshot.Classifications; break;
                case QsApiResourceKind.Qa: body = snapshot.Qa; break;
                case QsApiResourceKind.Revision: body = snapshot.Revisions; break;
                case QsApiResourceKind.Snapshot: body = snapshot.Snapshots; break;
                case QsApiResourceKind.Diff: body = snapshot.Diffs; break;
                case QsApiResourceKind.Tender: body = snapshot.Tenders; break;
                case QsApiResourceKind.Procurement: body = snapshot.Procurement; break;
                default: return new QsApiResponse(405, Version, string.Empty, null, "METHOD_NOT_ALLOWED");
            }
            return new QsApiResponse(200, Version, etag, body, string.Empty);
        }

        public QsApiResponse RefreshWorkbook(QsApiPrincipal principal, string projectId, string workbookId, LiveWorkbookRefreshBatch batch)
        {
            projectId = QsModelElementSnapshot.Require(projectId, "projectId");
            workbookId = QsModelElementSnapshot.Require(workbookId, "workbookId");
            if (principal == null) return new QsApiResponse(401, Version, string.Empty, null, "UNAUTHENTICATED");
            if (!principal.HasScope("qs3d.workbook.refresh")) return new QsApiResponse(403, Version, string.Empty, null, "FORBIDDEN");
            if (batch == null) throw new ArgumentNullException("batch");
            if (batch.Results.Any(x => !string.Equals(x.Binding.WorkbookId, workbookId, StringComparison.OrdinalIgnoreCase)))
                return new QsApiResponse(409, Version, string.Empty, batch.Results, "WORKBOOK_IDENTITY_MISMATCH");
            if (batch.HasBlockingFailure) return new QsApiResponse(409, Version, string.Empty, batch.Results, "WORKBOOK_REFRESH_CONFLICT");
            return new QsApiResponse(200, Version, string.Empty, batch.Results, batch.HasStaleData ? "STALE_SOURCE_REVISION" : string.Empty);
        }

        private static string BuildEtag(string projectId, string revision, QsApiResourceKind resource)
        {
            return "W/\"qs3d-v1-" + FrameEtagSegment(projectId) + FrameEtagSegment(revision) + FrameEtagSegment(resource.ToString()) + "\"";
        }

        private static string FrameEtagSegment(string value)
        {
            value = QsModelElementSnapshot.Require(value, "etagSegment");
            var builder = new StringBuilder(value.Length * 4 + 16);
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            foreach (var character in value)
                builder.Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
            return builder.ToString();
        }
    }
}
