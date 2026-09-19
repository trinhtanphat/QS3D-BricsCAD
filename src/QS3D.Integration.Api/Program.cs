using System.Security.Claims;
using QS3D.Integration.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IIntegrationReadStore, EmptyIntegrationReadStore>();
var app = builder.Build();

var v1 = app.MapGroup("/api/v1");
MapRead(v1, "/projects/{id}", "project:read", (IIntegrationReadStore s, string id) => s.GetProject(id));
MapRead(v1, "/models/{id}", "model:read", (IIntegrationReadStore s, string id) => s.GetModel(id));
MapRead(v1, "/sources/{id}", "source:read", (IIntegrationReadStore s, string id) => s.GetSource(id));
MapRead(v1, "/quantities/{id}", "quantity:read", (IIntegrationReadStore s, string id) => s.GetQuantity(id));
MapRead(v1, "/boq/{id}", "boq:read", (IIntegrationReadStore s, string id) => s.GetBoq(id));
MapRead(v1, "/estimates/{id}", "cost:read", (IIntegrationReadStore s, string id) => s.GetEstimate(id));
MapRead(v1, "/classifications/{id}", "classification:read", (IIntegrationReadStore s, string id) => s.GetClassification(id));
MapRead(v1, "/qa/{id}", "qa:read", (IIntegrationReadStore s, string id) => s.GetQa(id));
MapRead(v1, "/revisions/{id}", "revision:read", (IIntegrationReadStore s, string id) => s.GetRevision(id));
MapRead(v1, "/snapshots/{id}", "revision:read", (IIntegrationReadStore s, string id) => s.GetSnapshot(id));
MapRead(v1, "/diffs/{id}", "revision:read", (IIntegrationReadStore s, string id) => s.GetDiff(id));
MapRead(v1, "/tenders/{id}", "tender:read", (IIntegrationReadStore s, string id) => s.GetTender(id));
MapRead(v1, "/procurements/{id}", "procurement:read", (IIntegrationReadStore s, string id) => s.GetProcurement(id));

app.Run();

static void MapRead<T>(RouteGroupBuilder group, string pattern, string scope, Func<IIntegrationReadStore, string, T?> read)
{
    group.MapGet(pattern, (HttpContext http, IIntegrationReadStore store, string id) =>
    {
        var auth = ApiAuthorization.Authorize(http.User, scope);
        if (auth is not null) return auth;
        var access = store.GetAccess(id);
        var boundary = ApiAuthorization.AuthorizeAccess(http.User, access);
        if (boundary is not null) return boundary;
        var value = read(store, id);
        return value is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Resource not found")
            : Results.Json(ApiEnvelope<T>.V1(http.TraceIdentifier, DateTimeOffset.UtcNow, ApiAuthorization.Context(http.User), value));
    });
}

public static class ApiAuthorization
{
    public static IResult? Authorize(ClaimsPrincipal user, string requiredScope)
    {
        if (user.Identity?.IsAuthenticated != true)
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication required");
        var scopes = user.FindAll("scope").SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToHashSet(StringComparer.Ordinal);
        return scopes.Contains(requiredScope) ? null : Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Required scope missing");
    }

    public static IResult? AuthorizeAccess(ClaimsPrincipal user, ResourceAccess? access)
    {
        if (access is null)
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Resource not found");
        var tenant = user.FindFirstValue("tenant_id");
        if (!string.IsNullOrEmpty(access.TenantId) && !string.Equals(tenant, access.TenantId, StringComparison.Ordinal))
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Tenant boundary violation");
        var projects = user.FindAll("project_id").Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(access.ProjectId) && !projects.Contains(access.ProjectId))
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Project boundary violation");
        return null;
    }

    public static AuthorizationContext Context(ClaimsPrincipal user) => new(
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.Identity?.Name ?? "unknown",
        user.FindAll("scope").SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
        user.FindFirstValue("tenant_id"));
}

public sealed record ResourceAccess(string ProjectId, string? TenantId);

public interface IIntegrationReadStore
{
    ResourceAccess? GetAccess(string id);
    ProjectRef? GetProject(string id); ModelRef? GetModel(string id); SourceRef? GetSource(string id);
    QuantityItem? GetQuantity(string id); BoqItem? GetBoq(string id); EstimateRef? GetEstimate(string id); ClassificationRef? GetClassification(string id);
    QaFinding? GetQa(string id); RevisionRef? GetRevision(string id); SnapshotRef? GetSnapshot(string id);
    DiffEntry? GetDiff(string id); TenderRef? GetTender(string id); ProcurementRef? GetProcurement(string id);
}

public sealed class EmptyIntegrationReadStore : IIntegrationReadStore
{
    public ResourceAccess? GetAccess(string id) => null;
    public ProjectRef? GetProject(string id) => null; public ModelRef? GetModel(string id) => null; public SourceRef? GetSource(string id) => null;
    public QuantityItem? GetQuantity(string id) => null; public BoqItem? GetBoq(string id) => null; public EstimateRef? GetEstimate(string id) => null; public ClassificationRef? GetClassification(string id) => null;
    public QaFinding? GetQa(string id) => null; public RevisionRef? GetRevision(string id) => null; public SnapshotRef? GetSnapshot(string id) => null;
    public DiffEntry? GetDiff(string id) => null; public TenderRef? GetTender(string id) => null; public ProcurementRef? GetProcurement(string id) => null;
}

public partial class Program { }
